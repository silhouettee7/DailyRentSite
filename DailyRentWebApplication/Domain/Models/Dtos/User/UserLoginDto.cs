namespace Domain.Models.Dtos.User;

public class UserLoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Fingerprint { get; set; }
}