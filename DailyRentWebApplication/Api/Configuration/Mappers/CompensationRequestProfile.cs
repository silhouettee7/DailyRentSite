using Api.Models;
using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos.CompensationRequest;
using Domain.Models.Dtos.Image;

namespace Api.Configuration.Mappers;

public class CompensationRequestProfile: Profile
{
    public CompensationRequestProfile()
    {
        CreateMap<CompensationRequestCreate, CompensationRequestDto>()
            .ForMember(dest => dest.ProofPhotos, opt => opt.MapFrom(src => src.ProofPhotos.Select(i => new ImageFileRequest
            {
                FileName = i.FileName,
                Stream = i.OpenReadStream(),
                ContentType = i.ContentType
            })));
        CreateMap<CompensationRequest, CompensationRequestResponse>();
    }
}