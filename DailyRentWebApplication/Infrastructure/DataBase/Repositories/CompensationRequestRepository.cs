using Domain.Abstractions.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataBase.Repositories;

public class CompensationRequestRepository(AppDbContext context)
    : Repository<CompensationRequest>(context), ICompensationRequestRepository
{
    private readonly AppDbContext _context = context;

    public async Task<CompensationRequest?> GetWithBookingAndPropertyAsync(int requestId)
    {
        return await _context.CompensationRequests
            .Include(cr => cr.Booking)
            .ThenInclude(b => b.Property)
            .FirstOrDefaultAsync(c => c.Id == requestId);
    }

    public async Task<CompensationRequest?> GetWithBookingPropertyAndTenantAsync(int requestId)
    {
        return await _context.CompensationRequests
            .Include(cr => cr.Booking)
            .ThenInclude(b => b.Property)
            .Include(cr => cr.Tenant)
            .FirstOrDefaultAsync(c => c.Id == requestId);
    }

    public async Task<CompensationRequest?> GetByBookingIdAsync(int bookingId)
    {
        return await _context.CompensationRequests
            .FirstOrDefaultAsync(cr => cr.BookingId == bookingId);
    }

    public async Task<int> DeleteByIdAsync(int requestId)
    {
        return await _context.CompensationRequests
            .Where(cr => cr.Id == requestId)
            .ExecuteDeleteAsync();
    }
}