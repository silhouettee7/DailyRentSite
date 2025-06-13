using AutoMapper;
using Domain.Entities;
using Domain.Models.Enums;
using Domain.Models.Payment;
using Infrastructure.PaymentSystem.Api;
using Infrastructure.PaymentSystem.Extensions;

namespace Infrastructure.PaymentSystem.Mappers;

public class PaymentCreatedMapper: Profile
{
    public PaymentCreatedMapper()
    {
        CreateMap<PaymentCreatedResponse, PaymentCreated>()
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => decimal.Parse(string.Join(',',src.Amount.Value.Split('.', StringSplitOptions.RemoveEmptyEntries)))))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Amount.Currency))
            .ForMember(dest => dest.Paid, opt => opt.MapFrom(src => src.Paid))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToPaymentStatus()))
            .ForMember(dest => dest.ConfirmationUrl, opt => opt.MapFrom(src => src.Confirmation.ConfirmationUrl));

        CreateMap<PaymentCreated, Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}