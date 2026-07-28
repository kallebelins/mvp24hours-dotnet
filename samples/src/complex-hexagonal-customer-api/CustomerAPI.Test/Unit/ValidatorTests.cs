using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Application.Validations.Contacts;
using CustomerAPI.Application.Validations.Customers;
using CustomerAPI.Core.Enums;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerCreateValidatorTests
{
    private readonly CustomerCreateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameEmpty_IsInvalid()
    {
        var model = new CustomerCreate { Name = string.Empty };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameTooLong_IsInvalid()
    {
        var model = new CustomerCreate { Name = new string('X', 51) };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("50 characters"));
    }

    [Fact]
    public async Task Validate_WhenNameValid_IsValid()
    {
        var model = new CustomerCreate { Name = "Katherine Johnson", Note = "NASA" };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }
}

[Trait("Category", "Unit")]
public class ContactCreateValidatorTests
{
    private readonly ContactCreateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenDescriptionEmpty_IsInvalid()
    {
        var model = new ContactCreate { Type = ContactType.Other, Description = string.Empty };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Contact description is required.");
    }

    [Fact]
    public async Task Validate_WhenEmailInvalid_IsInvalid()
    {
        var model = new ContactCreate { Type = ContactType.Email, Description = "not-an-email" };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Incorrect email.");
    }

    [Fact]
    public async Task Validate_WhenEmailValid_IsValid()
    {
        var model = new ContactCreate { Type = ContactType.Email, Description = "ada@example.com" };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }
}
