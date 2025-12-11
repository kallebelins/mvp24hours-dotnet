//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using FluentValidation;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Command for validation testing.
/// </summary>
public class CreateUserCommand : IMediatorCommand<int>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

/// <summary>
/// Validator for CreateUserCommand.
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Age)
            .GreaterThan(0).WithMessage("Age must be greater than 0")
            .LessThan(150).WithMessage("Age must be less than 150");
    }
}

/// <summary>
/// Handler for CreateUserCommand.
/// </summary>
public class CreateUserCommandHandler : IMediatorCommandHandler<CreateUserCommand, int>
{
    private static int _lastId = 0;

    public Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(++_lastId);
    }
}

