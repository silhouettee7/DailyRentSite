namespace Domain.Models.Dtos;

public class PropertySearchRequest
{
    public string City { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public bool HasPets { get; set; }
}