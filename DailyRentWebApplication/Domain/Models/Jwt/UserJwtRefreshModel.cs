namespace Domain.Models.Jwt;

public class UserJwtRefreshModel
{
    public int UserId { get; set; }
    public string Fingerprint { get; set; }
}