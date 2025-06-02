namespace Domain.Models.Dtos.Image;

public class ImageFileResponse
{
    public string MimeType { get; set; }
    public string ContentBase64 {get; set;}
    public bool IsMain { get; set; }
}