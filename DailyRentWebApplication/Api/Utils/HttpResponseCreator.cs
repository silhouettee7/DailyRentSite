using Domain.Models.Result;

namespace Api.Utils;

public class HttpResponseCreator
{
    public IResult CreateResponse<T>(Result<T> result)
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
                    return Results.Ok(result.Value);
                case SuccessType.Created:
                    return Results.Created();
                case SuccessType.NoContent:
                    return Results.NoContent();
            }
        }

        return HandleError(result);
    }
    
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
                    return Results.Ok(result);
                case SuccessType.Created:
                    return Results.Created();
                case SuccessType.NoContent:
                    return Results.NoContent();
            }
        }
        return HandleError(result);
    }

    private IResult HandleError(Result result)
    {
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
                return Results.BadRequest(result.Error.ErrorMessage);
            case ErrorType.NotFound:
                return Results.NotFound(result.Error.ErrorMessage);
            case ErrorType.ServerError:
                return Results.Problem(result.Error.ErrorMessage);
            default:
                return Results.Empty;
        }
    }
}