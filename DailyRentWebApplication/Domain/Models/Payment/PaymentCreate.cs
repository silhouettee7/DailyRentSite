using System.Text.Json.Serialization;

namespace Domain.Models.Payment;


using System.Text.Json.Serialization;

public class PaymentCreate
{
    [JsonPropertyName("amount")]
    public AmountCreate Amount { get; set; }

    [JsonPropertyName("capture")]
    public bool Capture { get; set; }

    [JsonPropertyName("confirmation")]
    public ConfirmationCreate Confirmation { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class AmountCreate
{
    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }
}

public class ConfirmationCreate
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("return_url")]
    public string ReturnUrl { get; set; }
}
