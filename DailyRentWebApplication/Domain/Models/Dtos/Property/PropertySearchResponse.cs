using Domain.Models.Dtos.Image;

namespace Domain.Models.Dtos.Property;

public class PropertySearchResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string City { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal TotalPrice { get; set; }
    public double AverageRating { get; set; }
    public ImageFileResponse MainImage { get; set; }
    public bool IsFavorite { get; set; }
}