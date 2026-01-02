//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Mvp24Hours.Application.Integration.Test.Entities;

namespace Mvp24Hours.Application.Integration.Test.Validators;

/// <summary>
/// FluentValidation validator for Product entity.
/// </summary>
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Product description is required")
            .MaximumLength(500)
            .WithMessage("Product description must not exceed 500 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("A valid category must be selected");
    }
}

/// <summary>
/// FluentValidation validator for Category entity.
/// </summary>
public class CategoryValidator : AbstractValidator<Category>
{
    public CategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required")
            .MaximumLength(100)
            .WithMessage("Category name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Category description must not exceed 300 characters");
    }
}

