namespace Domain.Models.Dtos;

public class PropertySearchResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string City { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal TotalPrice { get; set; }
    public double AverageRating { get; set; }
    public int MainImageId { get; set; }
    public bool IsFavorite { get; set; }
}