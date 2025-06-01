namespace Domain.Entities;

public class Property
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public decimal PricePerDay { get; set; }
    public int MaxGuests { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    
    public int OwnerId { get; set; }
    public int? LocationId { get; set; }
    
    public User Owner { get; set; }
    public Location Location { get; set; }
    public List<Booking> Bookings { get; set; } = new List<Booking>();
    public List<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public List<Amenity> Amenities { get; set; } = new List<Amenity>();
    public List<Review> Reviews { get; set; } = new List<Review>();
    public List<CompensationRequest> CompensationRequests { get; set; } = new List<CompensationRequest>();
}