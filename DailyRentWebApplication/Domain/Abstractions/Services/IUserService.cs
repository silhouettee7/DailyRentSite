using Domain.Models;
using Domain.Models.Dtos;
using Domain.Models.Dtos.User;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IUserService
{
    Task<Result> RegisterUserAsync(UserRegisterDto dto);
    Task<Result<UserProfileResponse>> GetUserProfileAsync(int userId);
    Task<Result> EditUserProfileAsync(UserProfileEdit userProfileEdit, int userId);
}