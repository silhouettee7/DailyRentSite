using AutoMapper;
using Domain.Abstractions.Clients;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Payment;
using Domain.Models.Result;
using Infrastructure.DataBase;

namespace Infrastructure.PaymentSystem;

public class PaymentService(IPaymentApiClient apiClient, AppDbContext context, IMapper mapper): IPaymentService
{
    public async Task<Result<PaymentAddedResult>> CreatePaymentAsync(PaymentCreate paymentCreate, int bookingId, int userId)
    {
        try
        {
            var createdPayment = await apiClient.CreatePaymentAsync(paymentCreate);
            var payment = mapper.Map<Payment>(createdPayment);
            payment.UserId = userId;
            payment.BookingId = bookingId;
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
            var paymentAddedResult = new PaymentAddedResult
            {
                Id = payment.Id,
                ExternalId = payment.ExternalId,
                ConfirmationUrl = createdPayment.ConfirmationUrl
            };
            return Result<PaymentAddedResult>.Success(SuccessType.Created, paymentAddedResult);
        }
        catch (Exception ex)
        {
            return Result<PaymentAddedResult>.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }
}