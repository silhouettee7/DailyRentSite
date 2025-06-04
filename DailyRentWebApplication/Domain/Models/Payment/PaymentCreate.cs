namespace Domain.Models.Payment;


public class PaymentCreate
{
    public AmountCreate AmountCreate { get; set; }
    public bool Capture { get; set; }
    public ConfirmationCreate ConfirmationCreate { get; set; }
    public string Description { get; set; }
}

public class AmountCreate
{
    public string Value { get; set; }
    public string Currency { get; set; }
}

public class ConfirmationCreate
{
    public string Type { get; set; }
    public string ReturnUrl { get; set; }
}