using Domain.Models.Dtos.Booking;

namespace Api.Configuration.Validators;

using FluentValidation;
using System;

public class BookingCreateRequestValidator : AbstractValidator<BookingCreateRequest>
{
    public BookingCreateRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("ID свойства обязательно для заполнения")
            .GreaterThan(0).WithMessage("ID свойства должен быть положительным числом");

        RuleFor(x => x.CheckInDate)
            .NotEmpty().WithMessage("Дата заезда обязательна для заполнения")
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Дата заезда не может быть в прошлом")
            .LessThan(x => x.CheckOutDate)
            .WithMessage("Дата заезда должна быть раньше даты выезда");

        RuleFor(x => x.CheckOutDate)
            .NotEmpty().WithMessage("Дата выезда обязательна для заполнения")
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Дата выезда должна быть позже даты заезда")
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("Минимальная продолжительность бронирования - 1 день");

        RuleFor(x => x.AdultsCount)
            .NotEmpty().WithMessage("Количество взрослых обязательно для заполнения")
            .GreaterThan(0).WithMessage("Должен быть хотя бы один взрослый")
            .LessThanOrEqualTo(10).WithMessage("Максимальное количество взрослых - 10");

        RuleFor(x => x)
            .Must(x => x.AdultsCount + x.ChildrenCount > 0)
            .WithMessage("Должен быть указан хотя бы один гость (взрослый или ребенок)")
            .Must(x => x.AdultsCount + x.ChildrenCount <= 15)
            .WithMessage("Общее количество гостей не может превышать 15");

        // Дополнительная проверка на минимальный срок бронирования
        RuleFor(x => x)
            .Must(x => (x.CheckOutDate - x.CheckInDate).TotalDays >= 1)
            .WithMessage("Минимальный срок бронирования - 1 день");
    }
}