using Domain.Models.Payment;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IPaymentService
{
    Task<Result<PaymentAddedResult>> CreatePaymentAsync(PaymentCreate paymentCreate, int bookingId, int userId);
}