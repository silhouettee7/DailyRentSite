using Domain.Models;
using Domain.Models.Dtos;
using Domain.Models.Dtos.User;
using Domain.Models.Result;

namespace Domain.Abstractions.Auth;

public interface IAuthService
{
    Task<Result<(string accessToken, Guid refreshToken)>> AuthenticateAsync(UserLoginDto userLoginDto);
    Task<Result<(string accessToken, Guid refreshToken)>> UpdateRefreshTokenAsync(Guid refreshTokenId, string fingerprint, int userId);
}