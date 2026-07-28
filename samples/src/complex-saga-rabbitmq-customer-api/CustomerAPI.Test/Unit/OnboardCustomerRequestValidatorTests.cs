using CustomerAPI.WebAPI.Controllers;
using CustomerAPI.WebAPI.Validations;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class OnboardCustomerRequestValidatorTests
{
    private readonly OnboardCustomerRequestValidator _validator = new();

    [Fact]
    public void OnboardCustomerRequestValidator_WhenInvalid_HasErrors()
    {
        var request = new OnboardCustomerRequest(Name: "", Email: "not-an-email");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OnboardCustomerRequest.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OnboardCustomerRequest.Email));
    }

    [Fact]
    public void OnboardCustomerRequestValidator_WhenValid_Passes()
    {
        var request = new OnboardCustomerRequest(
            Name: "Katherine Johnson",
            Email: "katherine@example.com");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
