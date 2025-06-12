using System.Security.Claims;
using Api.Utils;
using Domain.Abstractions.Services;
using Domain.Models.Dtos.Review;

namespace Api.Endpoints;

public static class ReviewEndpointsExt
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/reviews")
            .WithTags("Reviews");

        group.MapPost("/create",
            async (ReviewCreateRequest reviewCreateRequest,
                IReviewService reviewService, HttpResponseCreator creator, HttpContext context) =>
            {
                var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                var success = int.TryParse(userIdClaim?.Value, out int userId);
                if (!success)
                {
                    return Results.Forbid();
                }
                
                var result = await reviewService.CreateReview(reviewCreateRequest, userId);
                return creator.CreateResponse(result);
            });
        return endpoints;
    }
}