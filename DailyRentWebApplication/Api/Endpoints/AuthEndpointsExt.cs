using System.Security.Claims;
using Api.Filters;
using Api.Models;
using Api.Utils;
using Domain.Abstractions.Auth;
using Domain.Models.Dtos;
using Domain.Models.Dtos.User;
using Domain.Models.Result;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class AuthEndpointsExt
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (UserLoginDto userLoginDto, IAuthService authService, 
            HttpContext context, IConfiguration config, HttpResponseCreator responseCreator) =>
        {
            var tokens = await authService.AuthenticateAsync(userLoginDto);
            if (!tokens.IsSuccess)
            {
                return responseCreator.CreateResponse(tokens);
            }

            var cookieOptions = new CookieOptions
            {
                SameSite = SameSiteMode.Strict,
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(
                    int.Parse(config["AuthConfig:ExpiredAtMinutesRefresh"] ?? "1")),
                Secure = true,
                Path = "/auth"
            };
            context.Response.Cookies.Append("refreshToken", tokens.Value.refreshToken.ToString(), cookieOptions);
            return Results.Ok(new Token{AccessToken = tokens.Value.accessToken});
        })
        .AddEndpointFilter<ValidationFilter<UserLoginDto>>();

        group.MapPost("/refresh", async (string fingerprint, IAuthService authService,
            HttpContext context, IConfiguration config, HttpResponseCreator responseCreator) =>
        {
            var refreshId = Guid.Parse(context.Request.Cookies["refreshToken"] ?? Guid.Empty.ToString());
            var userId =
                int.Parse(context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "-1");
            var tokens = await authService.UpdateRefreshTokenAsync(refreshId, fingerprint, userId);
            if (!tokens.IsSuccess)
            {
                return responseCreator.CreateResponse(Result<string>.Failure(tokens.Error!));
            }
            var cookieOptions = new CookieOptions
            {
                SameSite = SameSiteMode.Strict,
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(
                    int.Parse(config["AuthConfig:ExpiredAtMinutesRefresh"] ?? "1")),
                Secure = true,
                Path = "/auth"
            };
            context.Response.Cookies.Append("refreshToken", tokens.Value.refreshToken.ToString(), cookieOptions);
            return Results.Ok(new Token{AccessToken = tokens.Value.accessToken});
        });
        
        return endpoints;
    }
}