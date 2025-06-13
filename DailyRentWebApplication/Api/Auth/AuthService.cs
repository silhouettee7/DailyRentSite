using Domain.Abstractions.Auth;
using Domain.Abstractions.Repositories;
using Domain.Entities;
using Domain.Models;
using Domain.Models.Dtos;
using Domain.Models.Dtos.User;
using Domain.Models.Jwt;
using Domain.Models.Result;
using Microsoft.AspNetCore.Identity;

namespace Api.Auth;

public class AuthService(
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher<User> hasher, 
    IUserRepository userRepository,
    IRefreshSessionRepository refreshSessionRepository,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<(string accessToken, Guid refreshToken)>> AuthenticateAsync(UserLoginDto userLoginDto)
    {
        logger.LogInformation("Попытка аутентификации пользователя с email: {Email}", userLoginDto.Email);

        var user = await userRepository.GetByFilterAsync(u => u.Email == userLoginDto.Email);
        if (user == null)
        {
            logger.LogWarning("Аутентификация не удалась - пользователь с email {Email} не найден", userLoginDto.Email);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Пользователь не найден", ErrorType.NotFound));
        }

        var passwordVerification = hasher.VerifyHashedPassword(user, user.PasswordHash, userLoginDto.Password);
        if (passwordVerification != PasswordVerificationResult.Success)
        {
            logger.LogWarning("Аутентификация не удалась - неверный пароль для пользователя {UserId}", user.Id);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Неверный пароль", ErrorType.BadRequest));
        }

        logger.LogDebug("Генерация токенов для пользователя {UserId}", user.Id);

        try
        {
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

            logger.LogInformation("Успешная аутентификация пользователя {UserId}", user.Id);
            return Result<(string accessToken, Guid refreshToken)>.Success(
                SuccessType.Created, (accessToken, refreshSession.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при аутентификации пользователя {UserId}", user.Id);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Ошибка аутентификации", ErrorType.ServerError));
        }
    }

    public async Task<Result<(string accessToken, Guid refreshToken)>> UpdateRefreshTokenAsync(
        Guid refreshTokenId, string fingerprint, int userId)
    {
        logger.LogInformation("Попытка обновления refresh токена {RefreshTokenId} для пользователя {UserId}", 
            refreshTokenId, userId);

        var currentSession = await refreshSessionRepository.GetSessionByIdWithUser(refreshTokenId);
        if (currentSession == null)
        {
            logger.LogWarning("Сессия не найдена: {RefreshTokenId}", refreshTokenId);
            await refreshSessionRepository.DeleteAllUserSessions(userId);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Сессия не найдена", ErrorType.NotFound));
        }

        refreshSessionRepository.Delete(currentSession);

        if (!currentSession.Fingerprint.Equals(fingerprint))
        {
            logger.LogWarning("Неверный fingerprint для сессии {RefreshTokenId}", refreshTokenId);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Сессия истекла или неверный fingerprint", ErrorType.BadRequest));
        }

        if ((DateTime.UtcNow - currentSession.CreatedAt).Minutes >= currentSession.ExpiresInMinutes)
        {
            logger.LogWarning("Сессия истекла: {RefreshTokenId}", refreshTokenId);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Сессия истекла или неверный fingerprint", ErrorType.BadRequest));
        }

        var user = currentSession.User;
        if (user == null)
        {
            logger.LogError("Пользователь не найден для сессии {RefreshTokenId}", refreshTokenId);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Пользователь не найден для сессии", ErrorType.NotFound));
        }

        logger.LogDebug("Генерация новых токенов для пользователя {UserId}", user.Id);

        try
        {
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

            logger.LogInformation("Токены успешно обновлены для пользователя {UserId}", user.Id);
            return Result<(string accessToken, Guid refreshToken)>.Success(
                SuccessType.Ok, (accessToken, refreshSession.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении токена для пользователя {UserId}", user.Id);
            return Result<(string accessToken, Guid refreshToken)>.Failure(
                new Error("Ошибка обновления токена", ErrorType.ServerError));
        }
    }
}