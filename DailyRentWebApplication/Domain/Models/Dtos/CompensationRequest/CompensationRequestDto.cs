using Domain.Models.Dtos.Image;

namespace Domain.Models.Dtos.CompensationRequest;

public class CompensationRequestDto
{
    public string Description { get; set; }
    public List<ImageFileRequest> ProofPhotos { get; set; }
    public decimal RequestedAmount { get; set; }
    public int BookingId { get; set; }
}