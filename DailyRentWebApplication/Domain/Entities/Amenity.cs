namespace Domain.Entities;

public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? IconUrl { get; set; }
    public bool IsDeleted { get; set; }
    
    public List<Property> Properties { get; set; } = new();
}