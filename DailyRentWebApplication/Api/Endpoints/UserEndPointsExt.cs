using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos;

namespace Api.Endpoints;

public static class UserEndPointsExt
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var userGroup = endpoints.MapGroup("/users");
        
        userGroup.MapPost("/register", async (UserRegisterDto userRegisterDto, IUserService userService) => 
            await userService.RegisterUserAsync(userRegisterDto));

        return endpoints;
    }
}