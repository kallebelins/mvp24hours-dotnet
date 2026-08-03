using App.Domain.Entities;
using FluentValidation;

namespace App.Domain.Validations;

public class ItemValidator : AbstractValidator<Item>
{
    public ItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Item {PropertyName} is required.");
    }
}
