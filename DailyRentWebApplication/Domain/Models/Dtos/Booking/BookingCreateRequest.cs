namespace Domain.Models.Dtos.Booking;

public class BookingCreateRequest
{
    public int PropertyId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int AdultsCount { get; set; }
    public int ChildrenCount { get; set; }
    public bool HasPets { get; set; }
}