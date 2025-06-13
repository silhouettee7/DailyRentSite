using System.Security.Claims;
using Api.Filters;
using Api.Models;
using Api.Utils;
using Domain.Abstractions.Auth;
using Domain.Models.Dtos.User;
using Domain.Models.Result;

namespace Api.Endpoints;

public static class AuthEndpointsExt
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth")
            .WithTags("Auth")
            .WithOpenApi();

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
                return Results.Ok(new Token { AccessToken = tokens.Value.accessToken });
            })
            .AddEndpointFilter<ValidationFilter<UserLoginDto>>()
            .WithName("LoginUser")
            .WithSummary("Аутентификация пользователя")
            .WithDescription(
                "Авторизует пользователя по email и паролю, возвращает access token и устанавливает refresh token в cookie")
            .Produces<Token>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
            .ProducesProblem(StatusCodes.Status404NotFound, "text/plain")
            .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

        group.MapPost("/refresh", async (string fingerprint, IAuthService authService,
                HttpContext context, IConfiguration config, HttpResponseCreator responseCreator) =>
            {
                var refreshId = Guid.Parse(context.Request.Cookies["refreshToken"] ?? Guid.Empty.ToString());
                var userId =
                    int.Parse(context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ??
                              "-1");
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
                return Results.Ok(new Token { AccessToken = tokens.Value.accessToken });
            })
            .WithName("RefreshToken")
            .WithSummary("Обновление токенов доступа")
            .WithDescription("Обновляет access token по refresh token, который должен быть в cookies")
            .Produces<Token>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
            .ProducesProblem(StatusCodes.Status401Unauthorized, "text/plain")
            .ProducesProblem(StatusCodes.Status500InternalServerError, "text/plain");

        return endpoints;
    }
}