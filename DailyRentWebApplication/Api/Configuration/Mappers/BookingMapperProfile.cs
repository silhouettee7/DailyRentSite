using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos.Booking;

namespace Api.Configuration.Mappers;

public class BookingMapperProfile: Profile
{
    public BookingMapperProfile()
    {
        CreateMap<BookingCreateRequest, Booking>();
        CreateMap<Booking, BookingResponse>()
            .ForMember(dest => dest.PropertyCity, opt => opt.MapFrom(src => src.Property.Location.City))
            .ForMember(dest => dest.PropertyTitle, opt => opt.MapFrom(src => src.Property.Title));
    }
}