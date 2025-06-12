using AutoMapper;
using Domain.Models.Payment;
using Infrastructure.PaymentSystem.Api;
using Infrastructure.PaymentSystem.Extensions;

namespace Infrastructure.PaymentSystem.Mappers;

public class PaymentInfoMapper: Profile
{
    public PaymentInfoMapper()
    {
        CreateMap<PaymentInfoResponse, PaymentInfo>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            .ForMember(dest => dest.Paid, opt => opt.MapFrom(src => src.Paid))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToPaymentStatus()));
    }
}