namespace Infrastructure.PaymentSystem.Api;


public class PaymentCreatedResponse
{
    public string Id { get; set; }
    public string Status { get; set; }
    public bool Paid { get; set; }
    public Amount Amount { get; set; }
    public Confirmation Confirmation { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public Recipient Recipient { get; set; }
    public bool Refundable { get; set; }
    public bool Test { get; set; }
}

public class Amount
{
    public string Value { get; set; }
    public string Currency { get; set; }
}

public class Confirmation
{
    public string Type { get; set; }
    public string ConfirmationUrl { get; set; }
}

public class Recipient
{
    public string AccountId { get; set; }
    public string GatewayId { get; set; }
}