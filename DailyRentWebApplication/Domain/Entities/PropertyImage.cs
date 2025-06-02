namespace Domain.Entities;

public class PropertyImage
{
    public int Id { get; set; }
    public string? ImageUrl { get; set; }
    public string FileName { get; set; }
    public bool IsMain { get; set; }
    public bool IsDeleted { get; set; }
    
    public int PropertyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Property Property { get; set; }
}