using Domain.Models.Result;

namespace Api.Utils;

public class HttpResponseCreator
{
    public IResult CreateResponse(Result result)
    {
        if (result.IsSuccess)
        {
            if (result.SuccessType is null)
            {
                return Results.Problem();
            }
            switch (result.SuccessType)
            {
                case SuccessType.Ok:
                    return Results.Ok();
                case SuccessType.Created:
                    return Results.Created();
                case SuccessType.NoContent:
                    return Results.NoContent();
            }
        }
        if (result.Error is null)
        {
            return Results.Problem();
        }
        switch (result.Error.ErrorType)
        {
            case ErrorType.AuthenticationError:
                return Results.Unauthorized();
            case ErrorType.AuthorizationError:
                return Results.Forbid();
            case ErrorType.BadRequest:
                return Results.BadRequest();
            case ErrorType.NotFound:
                return Results.NotFound();
            case ErrorType.ServerError:
                return Results.Problem();
            default:
                return Results.Empty;
        }
    }
}