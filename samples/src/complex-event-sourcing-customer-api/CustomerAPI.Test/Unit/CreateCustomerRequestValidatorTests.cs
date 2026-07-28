using CustomerAPI.WebAPI.Controllers;
using CustomerAPI.WebAPI.Validations;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CreateCustomerRequestValidatorTests
{
    private readonly CreateCustomerRequestValidator _validator = new();

    [Fact]
    public void CreateCustomerRequestValidator_WhenInvalid_HasErrors()
    {
        var request = new CreateCustomerRequest(Name: "", Email: "bad");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCustomerRequest.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCustomerRequest.Email));
    }

    [Fact]
    public void CreateCustomerRequestValidator_WhenValid_Passes()
    {
        var request = new CreateCustomerRequest("Ada Lovelace", "ada@example.com");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
