using Domain.Models.Dtos.Booking;
using Domain.Models.Payment;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IBookingService
{
    Task<Result<int>> CreateBookingAsync(BookingCreateRequest bookingCreateRequest, int userId);
    Task<Result<List<BookingResponse>>> GetOwnerPropertyBookingsAsync(int propertyId, int userId);
    Task<Result<List<BookingResponse>>> GetUserBookingsAsync(int userId);
    Task<Result> RejectBookingAsync(int bookingId, int userId);
    Task<Result> ApproveBookingAsync(int bookingId, int userId);
    Task<Result> CancelBookingAsync(int bookingId, int userId);
    Task<Result<PaymentAddedResult>> PayForBookingAsync(int bookingId, int userId);
    Task<Result> GetPaymentForBookingAsync(int bookingId, int userId);
}