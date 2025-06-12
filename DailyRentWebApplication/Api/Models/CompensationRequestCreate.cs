namespace Api.Models;

public class CompensationRequestCreate
{
    public string Description { get; set; }
    public List<IFormFile> ProofPhotos { get; set; }
    public decimal RequestedAmount { get; set; }
    public int BookingId { get; set; }
}