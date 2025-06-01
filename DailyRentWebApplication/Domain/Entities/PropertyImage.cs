namespace Domain.Entities;

public class PropertyImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; }
    public bool IsMain { get; set; }
    public bool IsDeleted { get; set; } = false;
    
    public int PropertyId { get; set; }
    
    public Property Property { get; set; }
}