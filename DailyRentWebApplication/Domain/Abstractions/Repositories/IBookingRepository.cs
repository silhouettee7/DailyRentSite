using Domain.Entities;
using Domain.Models.Enums;

namespace Domain.Abstractions.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<bool> HasActiveBookingsAsync(int userId, int propertyId, DateTime checkInDate, DateTime checkOutDate);
    Task<decimal> GetPropertyPriceAsync(int propertyId);
    Task<bool> IsPropertyOwnedByUserAsync(int propertyId, int userId);
    Task<List<Booking>> GetOwnerPropertyBookingsAsync(int propertyId);
    Task<List<Booking>> GetUserBookingsWithDetailsAsync(int userId);
    Task<Booking?> GetBookingWithPropertyAsync(int bookingId);
    Task<Booking?> GetBookingWithPropertyAndOwnerAsync(int bookingId);
    Task<Booking?> GetBookingWithPropertyAndCompensationAsync(int bookingId);
    Task<bool> HasApprovedBookingForPropertyAsync(int propertyId);
    Task<Payment?> GetPaymentForBookingAsync(int bookingId);
}