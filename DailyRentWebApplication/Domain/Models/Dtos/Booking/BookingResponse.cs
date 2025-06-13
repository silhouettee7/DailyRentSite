using Domain.Models.Enums;

namespace Domain.Models.Dtos.Booking;

public class BookingResponse
{
    public int Id { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; }
    public DateTime CreatedDate { get; set; } 
    public int AdultsCount { get; set; }
    public int ChildrenCount { get; set; }
    public bool HasPets { get; set; }
    public int PropertyId { get; set; }
    public bool IsPaid { get; set; }
    public string PropertyTitle { get; set; }
    public string PropertyCity { get; set; }
}