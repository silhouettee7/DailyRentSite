using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos.Review;

namespace Api.Configuration.Mappers;

public class ReviewMapperProfile: Profile
{
    public ReviewMapperProfile()
    {
        CreateMap<Review, ReviewDetailsResponse>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Comment))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.Name));
    }
}