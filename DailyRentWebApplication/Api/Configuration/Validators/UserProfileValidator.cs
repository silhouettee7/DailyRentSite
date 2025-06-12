using System.Data;
using Domain.Models.Dtos.User;
using FluentValidation;

namespace Api.Configuration.Validators;

public class UserProfileValidator: AbstractValidator<UserProfileEdit>
{
    public UserProfileValidator()
    {
        RuleFor(o => o.Email)
            .NotNull().NotEmpty().WithMessage("email is required")
            .EmailAddress().WithMessage("email is invalid");
        
        RuleFor(o => o.Phone)
            .NotNull().NotEmpty().WithMessage("phone is required")
            .Matches("^\\+?\\d{1,3}[\\s-]?\\(?\\d{3}\\)?[\\s-]?\\d{3}[\\s-]?\\d{2}[\\s-]?\\d{2}$").WithMessage("phone is not valid");
        
        RuleFor(dto => dto.Name)
            .MaximumLength(100).WithMessage("Name cannot be longer than 100 characters.")
            .NotEmpty().NotNull().WithMessage("Name cannot be empty or null");
    }
}