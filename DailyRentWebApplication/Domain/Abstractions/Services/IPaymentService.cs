using Domain.Models.Payment;

namespace Domain.Abstractions.Services;

public interface IPaymentService
{
    Task<string> MakePaymentAsync(PaymentCreate paymentCreate);
    Task<bool> CheckForPaymentAsync(Guid paymentId);
}