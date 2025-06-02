using AutoMapper;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.Booking;
using Domain.Models.Enums;
using Domain.Models.Result;
using Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BookingService(AppDbContext context, IMapper mapper): IBookingService
{
    public async Task<Result<int>> CreateBookingAsync(BookingCreateRequest bookingCreateRequest, int userId)
    {
        var booking = mapper.Map<Booking>(bookingCreateRequest);
        var propertyPricePerDay = await context.Properties
            .Where(p => p.Id == booking.PropertyId)
            .Select(p => p.PricePerDay)
            .FirstOrDefaultAsync();
        if (propertyPricePerDay == default)
        {
            return Result<int>.Failure(new Error("Property is not found", ErrorType.NotFound));
        }
        booking.TotalPrice = propertyPricePerDay * (decimal)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();
        return Result<int>.Success(SuccessType.Created, booking.Id);
    }

    public async Task<Result<List<BookingResponse>>> GetOwnerPropertyBookingsAsync(int propertyId, int userId)
    {
        var isPropertyOwned = await context.Users
            .Include(u => u.OwnedProperties)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.OwnedProperties)
            .AnyAsync(p => p.Id == propertyId);
        if (!isPropertyOwned)
        {
            return Result<List<BookingResponse>>.Failure(new Error("Property is not owned", ErrorType.BadRequest));
        }

        var bookings = await context.Bookings
            .Where(b => b.PropertyId == propertyId && 
                        b.Status != BookingStatus.Cancelled)
            .ToListAsync();
        var bookingsResponse = mapper.Map<List<BookingResponse>>(bookings);
        if (bookingsResponse.Count == 0)
        {
            return Result<List<BookingResponse>>.Success(SuccessType.NoContent, bookingsResponse);
        }
        
        return Result<List<BookingResponse>>.Success(SuccessType.Ok, bookingsResponse);
    }

    public async Task<Result<List<BookingResponse>>> GetUserBookingsAsync(int userId)
    {
        var userBookings = await context.Users
            .Include(u => u.Bookings)
            .ThenInclude(b => b.Property)
            .ThenInclude(p => p.Location)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Bookings)
            .ToListAsync();
        var bookingsResponse = mapper.Map<List<BookingResponse>>(userBookings);
        if (userBookings.Count == 0)
        {
            return Result<List<BookingResponse>>.Success(SuccessType.NoContent, bookingsResponse );
        }
        return Result<List<BookingResponse>>.Success(SuccessType.Ok, bookingsResponse);
    }

    public async Task<Result> RejectBookingAsync(int bookingId, int userId)
    {
        var booking = await context.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null)
        {
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.Property.OwnerId != userId)
        {
            return Result.Failure(new Error("To reject booking user must be an owner", ErrorType.BadRequest));
        }
        booking.Status = BookingStatus.Rejected;
        await context.SaveChangesAsync();
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result> ApproveBookingAsync(int bookingId, int userId)
    {
        var booking = await context.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null)
        {
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.Property.OwnerId != userId)
        {
            return Result.Failure(new Error("To approve booking user must be an owner", ErrorType.BadRequest));
        }
        booking.Status = BookingStatus.Approved;
        await context.SaveChangesAsync();
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result> CancelBookingAsync(int bookingId, int userId)
    {
        var booking = await context.Bookings
            .FindAsync(bookingId);
        if (booking == null)
        {
            return Result.Failure(new Error("Booking not found", ErrorType.NotFound));
        }

        if (booking.TenantId != userId)
        {
            return Result.Failure(new Error("To cancel booking it must belong to user", ErrorType.BadRequest));
        }
        booking.Status = BookingStatus.Cancelled;
        await context.SaveChangesAsync();
        return Result.Success(SuccessType.NoContent);
    }
}