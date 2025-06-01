using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models;
using Domain.Models.Dtos;
using Domain.Models.Result;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public class UserService(IUserRepository userRepository, IPasswordHasher<User> hasher, IMapper mapper): IUserService
{
    public async Task<Result> RegisterUserAsync(UserRegisterDto dto)
    {
        var user = mapper.Map<User>(dto);
        user.Role = "user";
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();
        return Result.Success();
    }
}