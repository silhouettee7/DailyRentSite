using AutoMapper;
using Domain.Entities;
using Domain.Models.Dtos;

namespace Api.Configuration.Mappers;

public class UserMapperProfile: Profile
{
    public UserMapperProfile()
    {
        CreateMap<UserRegisterDto, User>()
            .ForMember(user => user.Name,
                config => config.MapFrom(userRegisterDto => userRegisterDto.Name))
            .ForMember(user => user.Email,
                config => config.MapFrom(userRegisterDto => userRegisterDto.Email));
    }
}