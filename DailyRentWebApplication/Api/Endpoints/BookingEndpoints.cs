using System.Security.Claims;
using Api.Utils;
using Domain.Abstractions.Services;
using Domain.Models.Dtos.Booking;

namespace Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookings(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/bookings");

        group.MapPost("/create", async (BookingCreateRequest bookingCreateRequest, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context) =>
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
        })
        .RequireAuthorization();
        
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
        })
        .RequireAuthorization();

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
        })
        .RequireAuthorization();
        
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
        })
        .RequireAuthorization();
        
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
        })
        .RequireAuthorization();
        
        return endpoints;
    }
}