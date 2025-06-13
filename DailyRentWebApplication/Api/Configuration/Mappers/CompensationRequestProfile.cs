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
        CreateMap<CompensationRequest, CompensationRequestResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<CompensationRequestDto, CompensationRequest>()
            .ForMember(dest => dest.ProofPhotosFileNames, opt => opt.Ignore());
    }
}