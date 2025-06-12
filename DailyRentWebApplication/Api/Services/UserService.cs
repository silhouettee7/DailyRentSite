using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.User;
using Domain.Models.Result;
using Infrastructure.DataBase;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public class UserService(
    IUserRepository userRepository,
    IPasswordHasher<User> hasher,
    IMapper mapper,
    AppDbContext context,
    ILogger<UserService> logger)
    : IUserService
{
    public async Task<Result> RegisterUserAsync(UserRegisterDto dto)
    {
        logger.LogDebug("Начата регистрация нового пользователя. Email: {Email}", dto.Email);

        try
        {
            var user = mapper.Map<User>(dto);
            user.Role = "user";
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
            
            await userRepository.AddAsync(user);
            await userRepository.SaveChangesAsync();

            logger.LogInformation("Пользователь успешно зарегистрирован. UserId: {UserId}", user.Id);
            return Result.Success(SuccessType.Created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при регистрации пользователя. Email: {Email}", dto.Email);
            return Result.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }

    public async Task<Result<UserProfileResponse>> GetUserProfileAsync(int userId)
    {
        logger.LogDebug("Запрос профиля пользователя. UserId: {UserId}", userId);

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Пользователь не найден. UserId: {UserId}", userId);
            return Result<UserProfileResponse>.Failure(new Error("User not found", ErrorType.NotFound));
        }

        var userProfileResponse = mapper.Map<UserProfileResponse>(user);
        
        logger.LogDebug("Профиль пользователя получен. UserId: {UserId}", userId);
        return Result<UserProfileResponse>.Success(SuccessType.Ok, userProfileResponse);
    }

    public async Task<Result> EditUserProfileAsync(UserProfileEdit userProfileEdit, int userId)
    {
        logger.LogDebug("Начато редактирование профиля пользователя. UserId: {UserId}", userId);

        var user = new User
        {
            Id = userId,
        };
        context.Attach(user);
        mapper.Map(userProfileEdit, user);
        
        await context.SaveChangesAsync();

        logger.LogInformation("Профиль пользователя обновлен. UserId: {UserId}", userId);
        return Result.Success(SuccessType.NoContent);
    }
}