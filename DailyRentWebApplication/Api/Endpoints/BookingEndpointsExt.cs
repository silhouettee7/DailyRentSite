using System.Security.Claims;
using Api.BackgroundServices;
using Api.Utils;
using Domain.Abstractions.Services;
using Domain.Models.Dtos.Booking;
using Domain.Models.Result;
using Hangfire;

namespace Api.Endpoints;

public static class BookingEndpointsExt
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/bookings")
            .WithTags("Bookings")
            .RequireAuthorization();

        group.MapPost("/create", async (BookingCreateRequest bookingCreateRequest,
            IBookingService bookingService, HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var bookingIdResult = await bookingService.CreateBookingAsync(bookingCreateRequest, userId);

            return creator.CreateResponse(bookingIdResult);
        });

        group.MapGet("/all/{propertyId}", async (int propertyId, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var bookingsResult = await bookingService.GetOwnerPropertyBookingsAsync(propertyId, userId);

            return creator.CreateResponse(bookingsResult);
        });

        group.MapGet("/all", async (IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var bookingsResult = await bookingService.GetUserBookingsAsync(userId);

            return creator.CreateResponse(bookingsResult);
        });

        group.MapPatch("/cancel/{bookingId}", async (int bookingId, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var result = await bookingService.CancelBookingAsync(bookingId, userId);

            return creator.CreateResponse(result);
        });

        group.MapPatch("/reject/{bookingId}", async (int bookingId, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var result = await bookingService.RejectBookingAsync(bookingId, userId);

            return creator.CreateResponse(result);
        });

        group.MapPatch("/approve/{bookingId}", async (int bookingId, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var result = await bookingService.ApproveBookingAsync(bookingId, userId);

            return creator.CreateResponse(result);
        });

        group.MapPost("/pay/{bookingId}", async (int bookingId, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var result = await bookingService.PayForBookingAsync(bookingId, userId);
            if (!result.IsSuccess)
            {
                return creator.CreateResponse(result);
            }

            var externalId = result.Value!.ExternalId;
            var id = result.Value.Id;
            BackgroundJob.Schedule((PaymentStatusCheckJob paymentStatusCheckJob) =>
                paymentStatusCheckJob.CheckStatusAsync(externalId, id, 0), TimeSpan.FromMinutes(5));
            return Results.Redirect(result.Value.ConfirmationUrl);
        });

        group.MapPost("/take/payment/{bookingId:int}", async (int bookingId,
            IBookingService bookingService, HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            return creator.CreateResponse(await bookingService.GetPaymentForBookingAsync(bookingId, userId));
        });
        
        return endpoints;
    }
}