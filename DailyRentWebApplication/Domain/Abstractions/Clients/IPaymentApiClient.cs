using Domain.Models.Payment;

namespace Domain.Abstractions.Clients;

public interface IPaymentApiClient
{
    Task<PaymentCreated> CreatePaymentAsync(PaymentCreate paymentCreate);
    Task<PaymentInfo> CheckForPaymentAsync(Guid paymentId);
}