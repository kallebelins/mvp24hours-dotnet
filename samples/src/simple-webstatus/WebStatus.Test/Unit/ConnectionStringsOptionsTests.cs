using System.ComponentModel.DataAnnotations;
using WebStatus.Configuration;

namespace WebStatus.Test.Unit;

[Trait("Category", "Unit")]
public class ConnectionStringsOptionsTests
{
    [Fact]
    public void Validate_WhenRequiredFieldsMissing_IsInvalid()
    {
        var options = new ConnectionStringsOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        isValid.Should().BeFalse();
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WhenAllFieldsProvided_IsValid()
    {
        var options = new ConnectionStringsOptions
        {
            SqlServer = "Server=localhost;Database=master;TrustServerCertificate=True",
            PostgreSql = "Host=localhost;Database=postgres",
            MySql = "Server=localhost;Database=mysql",
            Redis = "localhost:6379",
            MongoDb = "mongodb://localhost:27017",
            RabbitMQ = "amqp://guest:guest@localhost:5672"
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }
}
