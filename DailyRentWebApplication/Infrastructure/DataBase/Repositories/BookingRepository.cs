using Domain.Abstractions.Repositories;
using Domain.Entities;
using Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataBase.Repositories;

public class BookingRepository(AppDbContext context) : Repository<Booking>(context), IBookingRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> HasActiveBookingsAsync(int userId, int propertyId, DateTime checkInDate, DateTime checkOutDate)
    {
        return await _context.Bookings
            .AnyAsync(b => b.TenantId == userId &&
                          b.PropertyId == propertyId &&
                          b.Status != BookingStatus.Cancelled &&
                          b.Status != BookingStatus.Rejected &&
                          checkInDate <= b.CheckOutDate &&
                          checkOutDate >= b.CheckInDate);
    }

    public async Task<decimal> GetPropertyPriceAsync(int propertyId)
    {
        return await _context.Properties
            .Where(p => p.Id == propertyId)
            .Select(p => p.PricePerDay)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsPropertyOwnedByUserAsync(int propertyId, int userId)
    {
        return await _context.Users
            .Include(u => u.OwnedProperties)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.OwnedProperties)
            .AnyAsync(p => p.Id == propertyId);
    }

    public async Task<List<Booking>> GetOwnerPropertyBookingsAsync(int propertyId)
    {
        return await _context.Bookings
            .Where(b => b.PropertyId == propertyId && 
                       b.Status != BookingStatus.Cancelled)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetUserBookingsWithDetailsAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Bookings)
            .ThenInclude(b => b.Property)
            .ThenInclude(p => p.Location)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Bookings)
            .ToListAsync();
    }

    public async Task<Booking?> GetBookingWithPropertyAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<Booking?> GetBookingWithPropertyAndOwnerAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Property)
            .ThenInclude(p => p.Bookings)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<Booking?> GetBookingWithPropertyAndCompensationAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Property)
            .Include(b => b.CompensationRequest)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<bool> HasApprovedBookingForPropertyAsync(int propertyId)
    {
        return await _context.Bookings
            .AnyAsync(b => b.PropertyId == propertyId && 
                          b.Status == BookingStatus.Approved);
    }

    public async Task<Payment?> GetPaymentForBookingAsync(int bookingId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
    }
}