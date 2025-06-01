namespace Domain.Models.Result;

public enum ErrorType
{
    AuthenticationError = 1,
    AuthorizationError = 2,
    NotFound = 3,
    BadRequest = 4,
    ServerError = 5,
}