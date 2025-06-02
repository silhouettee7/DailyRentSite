using Domain.Entities;
using Domain.Models.Dtos.Amenity;
using Domain.Models.Dtos.Image;
using Domain.Models.Dtos.Location;
using Domain.Models.Dtos.Review;

namespace Domain.Models.Dtos.Property;

public class PropertyDetailsResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal PricePerDay { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public bool PetsAllowed { get; set; } 
    public string OwnerName { get; set; }
    public string OwnerPhone { get; set; }
    public LocationDetailsResponse Location { get; set; }
    public List<int> ImagesId { get; set; }
    public List<AmenityDetailsResponse> Amenities { get; set; }
    public List<ReviewDetailsResponse> Reviews { get; set; }
}