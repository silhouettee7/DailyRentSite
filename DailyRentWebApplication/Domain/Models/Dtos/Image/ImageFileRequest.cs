namespace Domain.Models.Dtos.Image;

public class ImageFileRequest
{
    public string FileName { get; set; }
    public Stream Stream { get; set; }
    public string ContentType { get; set; }
}