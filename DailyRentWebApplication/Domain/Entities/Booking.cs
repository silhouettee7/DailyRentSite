using Domain.Models.Enums;

namespace Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int AdultsCount { get; set; }
    public int ChildrenCount { get; set; }
    public bool HasPets { get; set; }
    public bool IsDeleted { get; set; } 
    
    public int PropertyId { get; set; }
    public int TenantId { get; set; }
    
    public Property Property { get; set; }
    public User Tenant { get; set; }
    public CompensationRequest CompensationRequest { get; set; }
}