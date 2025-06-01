using Domain.Abstractions.Auth;
using Domain.Abstractions.Repositories;
using Domain.Entities;
using Domain.Models;
using Domain.Models.Dtos;
using Domain.Models.Jwt;
using Microsoft.AspNetCore.Identity;

namespace Api.Auth;

public class AuthService(
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher<User> hasher, 
    IUserRepository userRepository,
    IRefreshSessionRepository refreshSessionRepository): IAuthService
{
    public async Task<Result<(string accessToken, Guid refreshToken)>> AuthenticateAsync(UserLoginDto userLoginDto)
    {
        var user = await userRepository
            .GetByFilterAsync(u => u.Email == userLoginDto.Email);
        if (user == null)
        {
            return Result<(string accessToken, Guid refreshToken)>.Failure("User not found");
        }

        if (hasher.VerifyHashedPassword(user, user.PasswordHash,userLoginDto.Password) !=
            PasswordVerificationResult.Success)
        {
            return Result<(string accessToken, Guid refreshToken)>.Failure("Password invalid");
        }

        var userJwtAccessModel = new UserJwtAccessModel
        {
            Role = user.Role,
            Id = user.Id,
        };
        var accessToken = jwtTokenGenerator.GenerateAccessJwtToken(userJwtAccessModel);

        var userJwtRefreshModel = new UserJwtRefreshModel
        {
            UserId = user.Id,
            Fingerprint = userLoginDto.Fingerprint,
        };
        var refreshSession = jwtTokenGenerator.GenerateRefreshSession(userJwtRefreshModel);
        await refreshSessionRepository.AddAsync(refreshSession);
        await refreshSessionRepository.SaveChangesAsync();
        return Result<(string accessToken, Guid refreshToken)>.Success((accessToken, refreshSession.Id));
    }

    public async Task<Result<(string accessToken, Guid refreshToken)>> UpdateRefreshTokenAsync(Guid refreshTokenId, string fingerprint, int userId)
    {
        var currentSession = await refreshSessionRepository.GetSessionByIdWithUser(refreshTokenId);
        if (currentSession == null)
        {
            await refreshSessionRepository.DeleteAllUserSessions(userId);
            return Result<(string accessToken, Guid refreshToken)>.Failure("Session not found");
        }
        refreshSessionRepository.Delete(currentSession);
        if (!currentSession.Fingerprint.Equals(fingerprint) ||
            (DateTime.UtcNow - currentSession.CreatedAt).Minutes >= currentSession.ExpiresInMinutes)
        {
            return Result<(string accessToken, Guid refreshToken)>.Failure("Session expired or fingerprint is invalid");
        }
        var user = currentSession.User;
        if (user == null)
        {
            return Result<(string accessToken, Guid refreshToken)>.Failure("User not found for refresh session");
        }
        var userJwtAccessModel = new UserJwtAccessModel
        {
            Role = user.Role,
            Id = user.Id,
        };
        var accessToken = jwtTokenGenerator.GenerateAccessJwtToken(userJwtAccessModel);

        var userJwtRefreshModel = new UserJwtRefreshModel
        {
            UserId = user.Id,
            Fingerprint = fingerprint,
        };
        var refreshSession = jwtTokenGenerator.GenerateRefreshSession(userJwtRefreshModel);
        await refreshSessionRepository.AddAsync(refreshSession);
        await refreshSessionRepository.SaveChangesAsync();
        return Result<(string accessToken, Guid refreshToken)>.Success((accessToken, refreshSession.Id));
    }
}