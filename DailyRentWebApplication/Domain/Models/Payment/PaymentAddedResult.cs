namespace Domain.Models.Payment;

public class PaymentAddedResult
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; }
    public string ConfirmationUrl { get; set; }
}