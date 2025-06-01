namespace Domain.Entities;

public class RefreshSession
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Fingerprint { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ExpiresInMinutes { get; set; }
}