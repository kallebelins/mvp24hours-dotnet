//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Exceptions;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Exceptions;

[Trait("Category", "Unit")]
public class BulkheadRejectedExceptionTest
{
    [Fact]
    public void DefaultConstructor_ShouldUseDefaultMessage()
    {
        var ex = new BulkheadRejectedException();

        ex.Message.Should().Be("Bulkhead is full. Operation cannot be executed.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void MessageConstructor_ShouldPreserveMessage()
    {
        var ex = new BulkheadRejectedException("bulkhead full");

        ex.Message.Should().Be("bulkhead full");
    }

    [Fact]
    public void MessageAndInnerConstructor_ShouldPreserveBoth()
    {
        var inner = new TimeoutException("waited too long");
        var ex = new BulkheadRejectedException("rejected", inner);

        ex.Message.Should().Be("rejected");
        ex.InnerException.Should().BeSameAs(inner);
    }
}
