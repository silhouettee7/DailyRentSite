namespace Domain.Entities;

public class Location
{
    public int Id { get; set; }
    public string Address { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string House { get; set; }
    public string Street { get; set; }
    public string District { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDeleted { get; set; }
    
    public List<Property> Properties { get; set; } = new();
}