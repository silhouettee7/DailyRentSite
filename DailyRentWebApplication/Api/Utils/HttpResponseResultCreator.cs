using Domain.Models.Result;

namespace Api.Utils;

public class HttpResponseResultCreator
{
    public IResult CreateResponse(Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
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