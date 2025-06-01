using Domain.Models.Enums;

namespace Domain.Entities;

public class CompensationRequest
{
    public int Id { get; set; }
    public string Description { get; set; }
    public List<string> ProofPhotos { get; set; } = new List<string>();
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public CompensationStatus Status { get; set; } = CompensationStatus.Pending;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolutionDate { get; set; }
    public bool IsDeleted { get; set; } = false;
    
    public int BookingId { get; set; }
    public int TenantId { get; set; }
    public int PropertyId { get; set; }
    
    public Booking Booking { get; set; }
    public User Tenant { get; set; }
    public Property Property { get; set; }
}