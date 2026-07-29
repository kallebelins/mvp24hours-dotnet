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
        var command = new CreateCustomerCommand
        {
            Model = new CustomerCreate { Name = string.Empty, Note = null! }
        };

        ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer Name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameProvided_IsValid()
    {
        var command = new CreateCustomerCommand
        {
            Model = new CustomerCreate { Name = "Grace Hopper", Note = "Admiral" }
        };

        ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
