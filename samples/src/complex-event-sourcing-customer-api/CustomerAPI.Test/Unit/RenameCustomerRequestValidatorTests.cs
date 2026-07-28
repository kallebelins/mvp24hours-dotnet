using CustomerAPI.WebAPI.Controllers;
using CustomerAPI.WebAPI.Validations;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class RenameCustomerRequestValidatorTests
{
    private readonly RenameCustomerRequestValidator _validator = new();

    [Fact]
    public void RenameCustomerRequestValidator_WhenInvalid_HasErrors()
    {
        var request = new RenameCustomerRequest(NewName: "");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RenameCustomerRequest.NewName));
    }

    [Fact]
    public void RenameCustomerRequestValidator_WhenValid_Passes()
    {
        var request = new RenameCustomerRequest("Augusta Ada");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
