using Domain.Models.Dtos.CompensationRequest;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface ICompensationRequestService
{
    Task<Result<int>> CreateCompensationRequest(CompensationRequestDto compensationRequest, int userId);
    Task<Result> DeleteCompensationRequest(int compensationRequestId);
    Task<Result<CompensationRequestResponse>> GetCompensationRequestByBookingId(int bookingId);
    Task<Result> RejectCompensationRequest(int compensationRequestId, int userId);
    Task<Result> ApproveCompensationRequest(int userId, int compensationRequestId, decimal amount);
    Task<Result<CompensationRequestResponse>> GetCompensationRequestById(int requestId);
}