using Domain.Models.Dtos.Amenity;
using Domain.Models.Dtos.Image;

namespace Domain.Models.Dtos.Property;

public class PropertyCreateRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal PricePerDay { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public bool PetsAllowed { get; set; }
    public List<AmenityCreateRequest> Amenities { get; set; }
    public List<ImageFileRequest> PropertyImages { get; set; }
    public ImageFileRequest MainImage { get; set; }
    public LocationCreateRequest Location { get; set; }
}