using Api.Models;
using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos;
using Domain.Models.Dtos.Image;
using Domain.Models.Dtos.Property;

namespace Api.Configuration.Mappers;

public class PropertyMapperProfile: Profile
{
    public PropertyMapperProfile()
    {
        CreateMap<PropertyCreateRequest, Property>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Bookings, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenities)) 
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location));

        CreateMap<Property, PropertySearchResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Location.City))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.PricePerDay, opt => opt.MapFrom(src => src.PricePerDay))
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.Reviews.Count == 0 ? 0:src.Reviews.Average(r => r.Rating)));

        CreateMap<Property, PropertyDetailsResponse>()
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<PropertyCreate, PropertyCreateRequest>();

        CreateMap<Property, OwnerProperty>();
    }
}