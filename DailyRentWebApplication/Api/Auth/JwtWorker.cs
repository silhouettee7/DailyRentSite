using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Configuration.Options;
using Domain.Abstractions.Auth;
using Domain.Entities;
using Domain.Models.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

public class JwtWorker(IOptionsMonitor<JwtOptions> options): IJwtTokenGenerator
{
    public string GenerateAccessJwtToken(UserJwtAccessModel userJwtAccessModel)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.Role, userJwtAccessModel.Role),
            new (ClaimTypes.NameIdentifier, userJwtAccessModel.Id.ToString())
        };
        var jwtOptions = options.CurrentValue;
        var jwtToken = new JwtSecurityToken(
            audience: jwtOptions.Audience,
            issuer: jwtOptions.Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.ExpiredAtMinutesAccess),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.IssuerSecretKey)),
                SecurityAlgorithms.HmacSha256)
        );
        var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return token;
    }

    public RefreshSession GenerateRefreshSession(UserJwtRefreshModel userJwtRefreshModel)
    {
        var jwtOptions = options.CurrentValue;
        return new RefreshSession
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresInMinutes = jwtOptions.ExpiredAtMinutesRefresh,
            Id = Guid.NewGuid(),
            Fingerprint = userJwtRefreshModel.Fingerprint,
            UserId = userJwtRefreshModel.UserId
        };
    }
}