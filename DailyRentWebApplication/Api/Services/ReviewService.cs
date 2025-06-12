using AutoMapper;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.Review;
using Domain.Models.Result;
using Infrastructure.DataBase;

namespace Api.Services;

public class ReviewService(
    AppDbContext context, 
    IMapper mapper, 
    ILogger<IReviewService> logger): IReviewService
{
    public async Task<Result<int>> CreateReview(ReviewCreateRequest reviewCreateRequest, int userId)
    {
        try
        {
            logger.LogDebug("Создание отзыва от {UserId}", userId);
            var review = mapper.Map<Review>(reviewCreateRequest);
            review.AuthorId = userId;
            review.CreatedDate = DateTime.UtcNow;
            await context.Reviews.AddAsync(review);
            await context.SaveChangesAsync();
            logger.LogInformation("Отзыв создан от {UserId}", userId);
            return Result<int>.Success(SuccessType.Created, review.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось создать отзыв от {UserId}", userId);
            return Result<int>.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }
}