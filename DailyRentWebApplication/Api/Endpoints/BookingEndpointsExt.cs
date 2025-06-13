using System.Security.Claims;
using Api.BackgroundServices;
using Api.Filters;
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
        })
        .AddEndpointFilter<ValidationFilter<BookingCreateRequest>>()
        .WithName("CreateBooking")
        .WithSummary("Создание нового бронирования")
        .WithDescription("Позволяет пользователю создать новое бронирование объекта недвижимости")
        .Produces<int>(StatusCodes.Status201Created, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");;

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
        .WithName("GetPropertyBookings")
        .WithSummary("Получение бронирований для объекта")
        .WithDescription("Возвращает список всех бронирований для указанного объекта недвижимости (для владельца)")
        .Produces<List<BookingResponse>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

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
        .WithName("GetUserBookings")
        .WithSummary("Получение бронирований текущего пользователя")
        .WithDescription("Возвращает список всех бронирований текущего пользователя")
        .Produces<List<BookingResponse>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

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
        .WithName("CancelBooking")
        .WithSummary("Отмена бронирования")
        .WithDescription("Позволяет пользователю отменить свое бронирование")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

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
        .WithName("RejectBooking")
        .WithSummary("Отклонение бронирования")
        .WithDescription("Позволяет владельцу объекта отклонить бронирование")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

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
        .WithName("ApproveBooking")
        .WithSummary("Подтверждение бронирования")
        .WithDescription("Позволяет владельцу объекта подтвердить бронирование")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

        group.MapPost("/pay/{bookingId}", async (int bookingId, IBookingService bookingService,
            HttpResponseCreator creator, HttpContext context, IBackgroundJobClient backgroundJobClient) =>
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
            backgroundJobClient.Schedule((PaymentStatusCheckJob paymentStatusCheckJob) =>
                paymentStatusCheckJob.CheckStatusAsync(externalId, id, 0), TimeSpan.FromMinutes(1));
            return Results.Ok(new {
                Url = result.Value.ConfirmationUrl
            });
        })
        .WithName("PayForBooking")
        .WithSummary("Оплата бронирования")
        .WithDescription("Инициирует процесс оплаты бронирования и возвращает URL для подтверждения оплаты")
        .Produces<object>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

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
        })
        .WithName("GetPaymentForBooking")
        .WithSummary("Получение платежа за бронирование")
        .WithDescription("Позволяет владельцу получить платеж за завершенное бронирование")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
        .ProducesProblem(StatusCodes.Status403Forbidden, "text/plain")
        .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
        .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");
        
        return endpoints;
    }
}