namespace Api.Configuration.Options;

public class JwtOptions
{
    public int ExpiredAtMinutesAccess { get; set; }
    public int ExpiredAtMinutesRefresh { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string IssuerSecretKey { get; set; }
}