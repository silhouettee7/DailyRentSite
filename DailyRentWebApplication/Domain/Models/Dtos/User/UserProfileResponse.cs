namespace Domain.Models.Dtos.User;

public class UserProfileResponse
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public decimal Balance { get; set; }
}