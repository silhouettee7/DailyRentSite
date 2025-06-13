using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos.Booking;

namespace Api.Configuration.Mappers;

public class BookingMapperProfile: Profile
{
    public BookingMapperProfile()
    {
        CreateMap<BookingCreateRequest, Booking>()
            .ForMember(dest => dest.CheckInDate, opt => opt.MapFrom(src => src.CheckInDate.ToUniversalTime()))
            .ForMember(dest => dest.CheckOutDate, opt => opt.MapFrom(src => src.CheckOutDate.ToUniversalTime()))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<Booking, BookingResponse>()
            .ForMember(dest => dest.PropertyCity, opt => opt.MapFrom(src => src.Property.Location.City))
            .ForMember(dest => dest.PropertyTitle, opt => opt.MapFrom(src => src.Property.Title))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}