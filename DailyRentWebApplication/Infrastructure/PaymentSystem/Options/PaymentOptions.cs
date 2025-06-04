namespace Infrastructure.PaymentSystem.Options;

public class PaymentOptions
{
    public string SecretKey { get; set; }
    public int ShopId { get; set; }
    public Guid IdempotenceKey { get; set; }
}