using System.Security.Claims;
using Api.Filters;
using Api.Utils;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos;
using Domain.Models.Dtos.User;
using Microsoft.OpenApi.Any;

namespace Api.Endpoints;

public static class UserEndpointsExt
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var userGroup = endpoints.MapGroup("/users")
            .WithTags("User Actions");
        
        userGroup.MapPost("/register", 
                async (UserRegisterDto userRegisterDto, IUserService userService) => 
                    await userService.RegisterUserAsync(userRegisterDto))
            .AddEndpointFilter<ValidationFilter<UserRegisterDto>>()
            .WithName("Registration")
            .WithSummary("Register user with email and password")
            .Produces<int>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        userGroup.MapPatch("/profile/edit", 
                async (UserProfileEdit userProfileEditDto, IUserService userService, 
                    HttpResponseCreator creator, HttpContext context) => 
                {
                    var userIdClaim = context.User.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    var success = int.TryParse(userIdClaim?.Value, out int userId);
                    if (!success)
                    {
                        return Results.Forbid();
                    }
                    
                    return creator.CreateResponse(await userService.EditUserProfileAsync(userProfileEditDto, userId));
                })
            .AddEndpointFilter<ValidationFilter<UserProfileEdit>>()
            .RequireAuthorization()
            .WithName("Edit Profile")
            .WithSummary("Edit user profile")
            .Produces<int>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        userGroup.MapGet("/profile", 
                async (IUserService userService, HttpResponseCreator creator, HttpContext context) =>
                {
                    var userIdClaim = context.User.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    var success = int.TryParse(userIdClaim?.Value, out int userId);
                    if (!success)
                    {
                        return Results.Forbid();
                    }
                    return creator.CreateResponse(await userService.GetUserProfileAsync(userId));
                })
            .RequireAuthorization()
            .WithName("Get Profile")
            .WithSummary("Get user profile")
            .Produces<UserProfileResponse>()
            .WithOpenApi(builder =>
            {
                builder.Responses["200"].Content["application/json"].Example = new OpenApiObject
                {
                    ["Name"] = new OpenApiString("Kamil"),
                    ["Email"] = new OpenApiString("kamil@gmail.com"),
                    ["Phone"] = new OpenApiString("123456789"),
                    ["Balance"] = new OpenApiDouble(200)
                };
                return builder;
            })
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        return endpoints;
    }
}