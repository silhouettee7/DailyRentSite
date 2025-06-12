namespace Infrastructure.PaymentSystem.Api;

public class PaymentInfoResponse
{
    public string Id { get; set; }
    public string Status { get; set; }
    public bool Paid { get; set; }
    public Amount Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Recipient Recipient { get; set; }
    public bool Refundable { get; set; }
    public bool Test { get; set; }
}

public class AmountInfo
{
    public string Value { get; set; }
    public string Currency { get; set; }
}

public class PaymentMethod
{
    public string Type { get; set; }
    public string Id { get; set; }
    public bool Saved { get; set; }
    public Card Card { get; set; }
    public string Title { get; set; }
}

public class Card
{
    public string First6 { get; set; }
    public string Last4 { get; set; }
    public string ExpiryMonth { get; set; }
    public string ExpiryYear { get; set; }
    public string CardType { get; set; }
    public CardProduct CardProduct { get; set; }
    public string IssuerCountry { get; set; }
    public string IssuerName { get; set; }
}

public class CardProduct
{
    public string Code { get; set; }
    public string Name { get; set; }
}

public class RecipientInfo
{
    public string AccountId { get; set; }
    public string GatewayId { get; set; }
}