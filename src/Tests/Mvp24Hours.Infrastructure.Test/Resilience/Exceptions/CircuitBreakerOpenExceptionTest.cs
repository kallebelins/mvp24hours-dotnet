//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Exceptions;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Exceptions;

[Trait("Category", "Unit")]
public class CircuitBreakerOpenExceptionTest
{
    [Fact]
    public void DefaultConstructor_ShouldUseDefaultMessage()
    {
        var ex = new CircuitBreakerOpenException();

        ex.Message.Should().Be("Circuit breaker is open. Operation cannot be executed.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void MessageConstructor_ShouldPreserveMessage()
    {
        var ex = new CircuitBreakerOpenException("custom open");

        ex.Message.Should().Be("custom open");
    }

    [Fact]
    public void MessageAndInnerConstructor_ShouldPreserveBoth()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new CircuitBreakerOpenException("open", inner);

        ex.Message.Should().Be("open");
        ex.InnerException.Should().BeSameAs(inner);
    }
}
