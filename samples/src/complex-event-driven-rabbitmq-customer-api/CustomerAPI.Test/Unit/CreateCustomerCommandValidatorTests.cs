using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.DTOs.Customers;
using FluentValidation.Results;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameEmpty_IsInvalid()
    {
        var command = new CreateCustomerCommand(new CustomerCreate { Name = string.Empty });

        ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WhenNameProvided_IsValid()
    {
        var command = new CreateCustomerCommand(new CustomerCreate { Name = "Ada Lovelace", Email = "ada@example.com" });

        ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
