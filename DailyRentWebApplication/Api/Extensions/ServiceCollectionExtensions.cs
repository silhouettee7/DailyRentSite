using System.Text;
using Api.Auth;
using Api.Configuration.Options;
using Api.Services;
using Api.Utils;
using Domain.Abstractions.Auth;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Models.Dtos;
using Infrastructure.DataBase.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
        services.AddScoped<IJwtTokenGenerator, JwtWorker>();
        services.AddScoped<IAuthService, AuthService>();
        services.Configure<JwtOptions>(config.GetSection("AuthConfig"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = config["AuthConfig:Audience"];
                options.ClaimsIssuer = config["AuthConfig:Issuer"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    RequireExpirationTime = true,
                    RequireAudience = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["AuthConfig:IssuerSecretKey"] ?? ""))
                };
            });
        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddUtils(this IServiceCollection services)
    {
        services.AddSingleton<HttpResponseResultCreator>();
        return services;
    }
}