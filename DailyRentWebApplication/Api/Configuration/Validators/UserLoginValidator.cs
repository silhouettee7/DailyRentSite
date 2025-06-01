using Domain.Models.Dtos;
using FluentValidation;

namespace Api.Configuration.Validators;

public class UserLoginValidator: AbstractValidator<UserLoginDto>
{
    public UserLoginValidator()
    {
        RuleFor(dto => dto.Email)
            .MaximumLength(100).WithMessage("Email cannot be longer than 100 characters.")
            .NotEmpty().NotNull().WithMessage("Email cannot be empty or null")
            .EmailAddress().WithMessage("Invalid email address");
        
        RuleFor(x => x.Password)
            .NotNull().NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must contain at least 8 characters")
            .MaximumLength(32).WithMessage("Password must not exceed 32 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(dto => dto.Fingerprint)
            .NotNull().NotEmpty().WithMessage("Fingerprint cannot be empty or null")
            .MinimumLength(10).WithMessage("Fingerprint cannot be less than 5 characters");
    }
}