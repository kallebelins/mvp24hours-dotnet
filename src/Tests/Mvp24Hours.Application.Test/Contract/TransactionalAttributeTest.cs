using Mvp24Hours.Application.Contract.Transaction;

namespace Mvp24Hours.Application.Test.Contract;

[Trait("Category", "Unit")]
public class TransactionalAttributeTest
{
    [Fact]
    public void Defaults_ShouldMatchExpectedValues()
    {
        var attribute = new TransactionalAttribute();

        attribute.ReadOnly.Should().BeFalse();
        attribute.TimeoutSeconds.Should().Be(0);
        attribute.RequiresNew.Should().BeFalse();
        attribute.Suppress.Should().BeFalse();
        attribute.IsolationLevel.Should().Be(TransactionIsolationLevel.Default);
        attribute.RetryOnTransientFailure.Should().BeFalse();
        attribute.MaxRetryAttempts.Should().Be(3);
    }
}
