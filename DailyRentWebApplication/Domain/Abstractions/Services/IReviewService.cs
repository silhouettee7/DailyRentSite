using Domain.Entities;
using Domain.Models.Dtos.Review;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IReviewService
{
    Task<Result<int>> CreateReview(ReviewCreateRequest reviewCreateRequest, int userId);
}