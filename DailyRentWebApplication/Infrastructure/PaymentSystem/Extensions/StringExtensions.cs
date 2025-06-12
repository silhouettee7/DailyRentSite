using Domain.Models.Enums;

namespace Infrastructure.PaymentSystem.Extensions;

public static class StringExtensions
{
    public static PaymentStatus ToPaymentStatus(this string status)
    {
        status = string.Join("", status.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.First().ToString().ToUpper() + string.Join("",x.Skip(1))));
        return Enum.Parse<PaymentStatus>(status);
    }
}