using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos.Location;
using Domain.Models.Dtos.Property;

namespace Api.Configuration.Mappers;

public class LocationMapperProfile: Profile
{
    public LocationMapperProfile()
    {
        CreateMap<LocationCreateRequest, Location>()
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Properties, opt => opt.Ignore());
        CreateMap<Location, LocationDetailsResponse>();
    }
}