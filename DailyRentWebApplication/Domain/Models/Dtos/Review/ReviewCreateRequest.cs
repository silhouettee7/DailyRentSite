namespace Domain.Models.Dtos.Review;

public class ReviewCreateRequest
{
    public required int Rating { get; set; }
    public required string Comment { get; set; }
    public required int PropertyId { get; set; }
}