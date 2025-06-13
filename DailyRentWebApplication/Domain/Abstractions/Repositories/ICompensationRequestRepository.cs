using Domain.Entities;

namespace Domain.Abstractions.Repositories;

public interface ICompensationRequestRepository : IRepository<CompensationRequest>
{
    Task<CompensationRequest?> GetWithBookingAndPropertyAsync(int requestId);
    Task<CompensationRequest?> GetWithBookingPropertyAndTenantAsync(int requestId);
    Task<CompensationRequest?> GetByBookingIdAsync(int bookingId);
    Task<int> DeleteByIdAsync(int requestId);
}