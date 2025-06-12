using Domain.Abstractions.Clients;
using Domain.Models.Enums;
using Hangfire;
using Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Api.BackgroundServices;

public class PaymentStatusCheckJob(IPaymentApiClient apiClient, AppDbContext context)
{
    private readonly int _maxAttempts = 10;
    public async Task CheckStatusAsync(Guid paymentExternalId, int paymentId, int attemptCount)
    {
        if (attemptCount == _maxAttempts)
        {
            return;
        }
        var paymentStatusResponse = await apiClient.CheckForPaymentAsync(paymentExternalId);
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null)
        {
            return;
        }
        payment.Status = paymentStatusResponse.Status;
        payment.LastCheckedAt = DateTime.UtcNow;
        payment.Paid = payment.Status == PaymentStatus.Succeeded;
        await context.SaveChangesAsync();
        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Canceled)
        {
            return;
        }
        BackgroundJob.Schedule(
            () => CheckStatusAsync(paymentExternalId, paymentId, attemptCount + 1), 
            TimeSpan.FromMinutes(6));
    }
}