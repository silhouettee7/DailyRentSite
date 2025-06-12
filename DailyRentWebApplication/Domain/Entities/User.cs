namespace Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public decimal Balance { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    
    public List<Property> OwnedProperties { get; set; } = new ();
    public List<Property> FavoriteProperties { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new ();
    public List<Review> Reviews { get; set; } = new ();
    public List<CompensationRequest> CompensationRequests { get; set; } = new();
    public List<RefreshSession> RefreshSessions { get; set; } = new();
}