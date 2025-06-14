using System.Net.Http.Headers;
using System.Text;
using Api.Auth;
using Api.BackgroundServices;
using Api.Configuration.Options;
using Api.Services;
using Api.Utils;
using Domain.Abstractions.Auth;
using Domain.Abstractions.Clients;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Models.Dtos;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.DataBase.Repositories;
using Infrastructure.FileStorage;
using Infrastructure.FileStorage.Options;
using Infrastructure.PaymentSystem;
using Infrastructure.PaymentSystem.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<ICompensationRequestService, CompensationRequestService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICompensationRequestRepository, CompensationRequestRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
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
                    ValidIssuer = config["AuthConfig:Issuer"],
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
        services.AddSingleton<HttpResponseCreator>();
        services.AddSingleton<FileWorker>();
        return services;
    }

    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MinioOptions>(config.GetSection("MinioConfig"));
        services.AddScoped<IFileStorageService, MinioService>();
        return services;
    }

    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration config)
    { 
        services.AddHangfire(configuration => 
            configuration.UsePostgreSqlStorage(options => 
                options.UseNpgsqlConnection(config.GetConnectionString("HangfireConnection"))));
        services.AddHangfireServer();
        services.AddScoped<PaymentStatusCheckJob>();
        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PaymentOptions>(config.GetSection("PaymentConfig"));
        services.AddHttpClient<IPaymentApiClient, PaymentApiClient>(client =>
        {
            client.BaseAddress = new Uri(config.GetSection("PaymentConfig:BaseAddress").Value!);
            var secretKey = config.GetSection("PaymentConfig:SecretKey").Value;
            var shopId = config.GetSection("PaymentConfig:ShopId").Value;
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            
        });

        return services;
    }
}