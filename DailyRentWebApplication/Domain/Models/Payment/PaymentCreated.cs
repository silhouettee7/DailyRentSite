using Domain.Models.Enums;

namespace Domain.Models.Payment;

public class PaymentCreated
{
    public Guid ExternalId { get; set; }
    public PaymentStatus Status { get; set; }
    public string ConfirmationUrl { get; set; }
    public bool Paid { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}