using CustomerAPI.Core.ValueObjects.Domain;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WhenEmpty_Throws(string? value)
    {
        var act = () => new CustomerName(value!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*")
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void Ctor_WhenTooLong_Throws()
    {
        var value = new string('A', 51);

        var act = () => new CustomerName(value);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 50*")
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void Ctor_WhenValid_Trims()
    {
        var name = new CustomerName("  Ada Lovelace  ");

        name.Value.Should().Be("Ada Lovelace");
    }
}
