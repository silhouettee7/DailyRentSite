using System.Globalization;
using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.Booking;
using Domain.Models.Enums;
using Domain.Models.Payment;
using Domain.Models.Result;
using Infrastructure.PaymentSystem.Options;
using Microsoft.Extensions.Options;

namespace Api.Services;

public class BookingService(
    IPaymentService paymentService,
    IBookingRepository bookingRepository,
    IMapper mapper,
    IOptionsMonitor<PaymentOptions> paymentOptions,
    ILogger<BookingService> logger)
    : IBookingService
{
    private readonly PaymentOptions _paymentOptions = paymentOptions.CurrentValue;

    public async Task<Result<int>> CreateBookingAsync(BookingCreateRequest bookingCreateRequest, int userId)
    {
        logger.LogDebug("Начато создание бронирования для propertyId: {PropertyId}", bookingCreateRequest.PropertyId);
        
        var booking = mapper.Map<Booking>(bookingCreateRequest);
        
        var hasActiveBookings = await bookingRepository.HasActiveBookingsAsync(
            userId,
            booking.PropertyId,
            booking.CheckInDate,
            booking.CheckOutDate);
        
        if (hasActiveBookings)
        {
            return Result<int>.Failure(new Error("Booking with given property already exists", ErrorType.BadRequest));
        }

        var propertyPricePerDay = await bookingRepository.GetPropertyPriceAsync(booking.PropertyId);
        if (propertyPricePerDay == default)
        {
            logger.LogWarning("Property не найден. PropertyId: {PropertyId}", booking.PropertyId);
            return Result<int>.Failure(new Error("Property is not found", ErrorType.NotFound));
        }
        
        booking.TotalPrice = propertyPricePerDay * (decimal)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
        logger.LogDebug("Рассчитана общая стоимость бронирования: {TotalPrice}", booking.TotalPrice);
        booking.TenantId = userId;
        
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        logger.LogInformation("Бронирование создано успешно. BookingId: {BookingId}", booking.Id);
        return Result<int>.Success(SuccessType.Created, booking.Id);
    }

    public async Task<Result<List<BookingResponse>>> GetOwnerPropertyBookingsAsync(int propertyId, int userId)
    {
        logger.LogDebug("Запрос бронирований владельца. PropertyId: {PropertyId}, UserId: {UserId}", propertyId, userId);

        var isPropertyOwned = await bookingRepository.IsPropertyOwnedByUserAsync(propertyId, userId);
        if (!isPropertyOwned)
        {
            logger.LogWarning("Пользователь не является владельцем property. UserId: {UserId}, PropertyId: {PropertyId}", 
                userId, propertyId);
            return Result<List<BookingResponse>>.Failure(new Error("Property is not owned", ErrorType.BadRequest));
        }

        var bookings = await bookingRepository.GetOwnerPropertyBookingsAsync(propertyId);
        var result = mapper.Map<List<BookingResponse>>(bookings);
        
        foreach (var booking in result)
        {
            var payment = await bookingRepository.GetPaymentForBookingAsync(booking.Id);
            booking.IsPaid = payment?.Paid ?? false;
            booking.IsPayProcess = payment is not null && payment.Status != PaymentStatus.Canceled;
        }

        if (bookings.Count == 0)
        {
            logger.LogInformation("Бронирования не найдены для propertyId: {PropertyId}", propertyId);
            return Result<List<BookingResponse>>.Success(SuccessType.NoContent, new List<BookingResponse>());
        }

        logger.LogDebug("Найдено {Count} бронирований для propertyId: {PropertyId}", bookings.Count, propertyId);
        return Result<List<BookingResponse>>.Success(SuccessType.Ok, result);
    }

    public async Task<Result<List<BookingResponse>>> GetUserBookingsAsync(int userId)
    {
        logger.LogDebug("Запрос бронирований пользователя. UserId: {UserId}", userId);

        var userBookings = await bookingRepository.GetUserBookingsWithDetailsAsync(userId);
        var result = mapper.Map<List<BookingResponse>>(userBookings);
        
        foreach (var booking in result)
        {
            var payment = await bookingRepository.GetPaymentForBookingAsync(booking.Id);
            booking.IsPaid = payment?.Paid ?? false;
            booking.IsPayProcess = payment is not null && payment.Status != PaymentStatus.Canceled;
        }
        
        if (result.Count == 0)
        {
            logger.LogInformation("У пользователя нет бронирований. UserId: {UserId}", userId);
            return Result<List<BookingResponse>>.Success(SuccessType.NoContent, new List<BookingResponse>());
        }

        logger.LogDebug("Найдено {Count} бронирований для userId: {UserId}", userBookings.Count, userId);
        return Result<List<BookingResponse>>.Success(SuccessType.Ok, result);
    }

    public async Task<Result> RejectBookingAsync(int bookingId, int userId)
    {
        logger.LogDebug("Попытка отклонить бронирование. BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);

        var booking = await bookingRepository.GetBookingWithPropertyAsync(bookingId);
        if (booking == null)
        {
            logger.LogWarning("Бронирование не найдено. BookingId: {BookingId}", bookingId);
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.Property.OwnerId != userId)
        {
            logger.LogWarning("Пользователь не является владельцем property. UserId: {UserId}, PropertyId: {PropertyId}", 
                userId, booking.Property.Id);
            return Result.Failure(new Error("To reject booking user must be an owner", ErrorType.BadRequest));
        }

        booking.Status = BookingStatus.Rejected;
        await bookingRepository.SaveChangesAsync();

        logger.LogInformation("Бронирование отклонено. BookingId: {BookingId}", bookingId);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result> ApproveBookingAsync(int bookingId, int userId)
    {
        logger.LogDebug("Попытка подтвердить бронирование. BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);

        var booking = await bookingRepository.GetBookingWithPropertyAndOwnerAsync(bookingId);
        if (booking == null)
        {
            logger.LogWarning("Бронирование не найдено. BookingId: {BookingId}", bookingId);
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }
        
        if (booking.Property.OwnerId != userId)
        {
            logger.LogWarning("Пользователь не является владельцем property. UserId: {UserId}, PropertyId: {PropertyId}", 
                userId, booking.Property.Id);
            return Result.Failure(new Error("To approve booking user must be an owner", ErrorType.BadRequest));
        }
        
        if (await bookingRepository.HasApprovedBookingForPropertyAsync(booking.Property.Id))
        {
            logger.LogWarning("У property уже есть подтвержденное бронирование. PropertyId: {PropertyId}", booking.Property.Id);
            return Result.Failure(new Error("Property has booking which already approved", ErrorType.BadRequest));
        }

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogWarning("Статус бронирования не Pending. Current status: {Status}", booking.Status);
            return Result.Failure(new Error("Booking status is not pending", ErrorType.BadRequest));
        }

        booking.Status = BookingStatus.Approved;
        await bookingRepository.SaveChangesAsync();

        logger.LogInformation("Бронирование подтверждено. BookingId: {BookingId}", bookingId);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result> CancelBookingAsync(int bookingId, int userId)
    {
        logger.LogDebug("Попытка отменить бронирование. BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);

        var booking = await bookingRepository.GetByFilterAsync(b => b.Id == bookingId);
        if (booking == null)
        {
            logger.LogWarning("Бронирование не найдено. BookingId: {BookingId}", bookingId);
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.TenantId != userId)
        {
            logger.LogWarning("Пользователь не является арендатором. UserId: {UserId}, TenantId: {TenantId}", 
                userId, booking.TenantId);
            return Result.Failure(new Error("To cancel booking it must belong to user", ErrorType.BadRequest));
        }

        booking.Status = BookingStatus.Cancelled;
        await bookingRepository.SaveChangesAsync();

        logger.LogInformation("Бронирование отменено. BookingId: {BookingId}", bookingId);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result<PaymentAddedResult>> PayForBookingAsync(int bookingId, int userId)
    {
        logger.LogDebug("Попытка оплаты бронирования. BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);

        var booking = await bookingRepository.GetByFilterAsync(b => b.Id == bookingId);
        if (booking == null)
        {
            logger.LogWarning("Бронирование не найдено. BookingId: {BookingId}", bookingId);
            return Result<PaymentAddedResult>.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.Status != BookingStatus.Approved)
        {
            logger.LogWarning("Статус бронирования не Approved. Current status: {Status}", booking.Status);
            return Result<PaymentAddedResult>.Failure(new Error("Booking status is not approved", ErrorType.BadRequest));
        }

        var paymentCreate = new PaymentCreate
        {
            Description = $"Booking id:{bookingId}\nUser id:{userId}",
            Capture = true,
            Amount = new AmountCreate
            {
                Currency = "RUB",
                Value = booking.TotalPrice.ToString(CultureInfo.InvariantCulture)
            },
            Confirmation = new ConfirmationCreate
            {
                Type = "redirect",
                ReturnUrl = _paymentOptions.RedirectUrl,
            }
        };

        logger.LogDebug("Создание платежа для бронирования. Сумма: {Amount}", booking.TotalPrice);
        var addedPaymentResult = await paymentService.CreatePaymentAsync(paymentCreate, bookingId, userId);

        if (!addedPaymentResult.IsSuccess)
        {
            logger.LogError("Ошибка при создании платежа. BookingId: {BookingId}, Error: {Error}", 
                bookingId, addedPaymentResult.Error?.ErrorMessage);
            return Result<PaymentAddedResult>.Failure(addedPaymentResult.Error ?? 
                new Error("Payment creation failed", ErrorType.ServerError));
        }

        logger.LogInformation("Платеж создан успешно. BookingId: {BookingId}, PaymentId: {PaymentId}", 
            bookingId, addedPaymentResult.Value?.Id);
        return Result<PaymentAddedResult>.Success(SuccessType.Created, addedPaymentResult.Value!);
    }

    public async Task<Result> GetPaymentForBookingAsync(int bookingId, int userId)
    {
        logger.LogDebug("Попытка получения платежа за бронирование. BookingId: {BookingId}, UserId: {UserId}", 
            bookingId, userId);

        var booking = await bookingRepository.GetBookingWithPropertyAndCompensationAsync(bookingId);
        if (booking == null)
        {
            logger.LogWarning("Бронирование не найдено. BookingId: {BookingId}", bookingId);
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.Property.OwnerId != userId)
        {
            logger.LogWarning("Пользователь не является владельцем property. UserId: {UserId}, PropertyId: {PropertyId}", 
                userId, booking.Property.Id);
            return Result.Failure(new Error("You are not property owner", ErrorType.BadRequest));
        }
        
        if (booking.Status != BookingStatus.Approved)
        {
            logger.LogWarning("Статус бронирования не Approved. Current status: {Status}", booking.Status);
            return Result.Failure(new Error("Booking status is not approved", ErrorType.BadRequest));
        }

        booking.Status = BookingStatus.Completed;
        var gettingSum = booking.TotalPrice - booking.CompensationRequest.ApprovedAmount;
        logger.LogInformation("Бронирование завершено. Сумма к выплате: {Amount}", gettingSum);

        return Result.Success(SuccessType.NoContent);
    }
}