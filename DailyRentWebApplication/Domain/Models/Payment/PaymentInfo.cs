using Domain.Models.Enums;

namespace Domain.Models.Payment;

public class PaymentInfo
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public bool Paid { get; set; }
}
