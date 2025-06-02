using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos.Amenity;

namespace Api.Configuration.Mappers;

public class AmenityMapperProfile: Profile
{
    public AmenityMapperProfile()
    {
        CreateMap<Amenity, AmenityDetailsResponse>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
        CreateMap<AmenityCreateRequest, Amenity>()
            .ForMember(dest => dest.Properties, opt => opt.Ignore());
    }
}