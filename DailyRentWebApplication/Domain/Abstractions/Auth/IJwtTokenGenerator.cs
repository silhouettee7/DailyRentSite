using Domain.Entities;
using Domain.Models.Jwt;

namespace Domain.Abstractions.Auth;

public interface IJwtTokenGenerator
{
    string GenerateAccessJwtToken(UserJwtAccessModel userJwtAccessModel);
    RefreshSession GenerateRefreshSession(UserJwtRefreshModel userJwtRefreshModel);
}