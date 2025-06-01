namespace Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    
    public int PropertyId { get; set; }
    public int AuthorId { get; set; }
    public int BookingId { get; set; }
    
    public Property Property { get; set; }
    public User Author { get; set; }
    public Booking Booking { get; set; }
}