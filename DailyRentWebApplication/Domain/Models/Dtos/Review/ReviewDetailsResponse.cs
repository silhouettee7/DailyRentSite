namespace Domain.Models.Dtos.Review;

public class ReviewDetailsResponse
{
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedDate { get; set; } 
    public int AuthorName { get; set; }
}