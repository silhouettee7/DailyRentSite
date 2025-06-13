using Domain.Models.Dtos.Image;
using Domain.Models.Enums;

namespace Domain.Models.Dtos.CompensationRequest;

public class CompensationRequestResponse
{
    public int Id { get; set; }
    public string Description { get; set; }
    public List<ImageFileResponse> ProofPhotos { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string Status { get; set; }
}