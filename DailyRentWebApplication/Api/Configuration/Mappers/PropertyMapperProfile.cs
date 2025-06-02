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
            .ForMember(dest => dest.CompensationRequests, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenities)) 
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location));

        CreateMap<Property, PropertySearchResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Location.City))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.PricePerDay, opt => opt.MapFrom(src => src.PricePerDay))
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.Reviews.Average(r => r.Rating)))
            .ForMember(dest => dest.MainImageId, opt => opt.MapFrom(src => src.Images.FirstOrDefault(i => i.IsMain).Id));

        CreateMap<Property, PropertyDetailsResponse>()
            .ForMember(dest => dest.ImagesId, opt => opt.MapFrom(src => src.Images.Select(i => i.Id)));

        CreateMap<PropertyCreate, PropertyCreateRequest>()
            .ForMember(dest => dest.PropertyImages,
                opt => opt.MapFrom(src => src.PropertyImages.Select(file => new ImageFileRequest
                {
                    Stream = file.OpenReadStream(),
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                })))
            .ForMember(dest => dest.MainImage, 
                opt => opt.MapFrom(src => new ImageFileRequest
                {
                    Stream = src.MainImage.OpenReadStream(),
                    FileName = src.MainImage.FileName,
                    ContentType = src.MainImage.ContentType,
                }));
    }
}