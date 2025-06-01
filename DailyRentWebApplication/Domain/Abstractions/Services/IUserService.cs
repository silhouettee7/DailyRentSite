using Domain.Models;
using Domain.Models.Dtos;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IUserService
{
    Task<Result> RegisterUserAsync(UserRegisterDto dto);
}