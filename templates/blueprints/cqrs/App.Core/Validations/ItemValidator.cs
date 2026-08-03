using App.Core.Entities;
using FluentValidation;

namespace App.Core.Validations;

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
