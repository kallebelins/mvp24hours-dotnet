//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Patterns.Test;

/// <summary>
/// 
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class HelperTest
{
    [Fact, Priority(1)]
    public void ToDeserializeBusinessResult()
    {
        // arrange
        IBusinessResult<Guid>? result = "{\"data\":\"77ec9da6-71c8-4be5-95e7-fc70fae45320\",\"messages\":[{\"key\":\"OPERATION_SUCCESS\",\"message\":\"Operação realizada com sucesso.\",\"type\":\"Success\"}],\"hasErrors\":false}".ToDeserializeBusinessResult<Guid>();
        // assert
        Assert.NotNull(result);
    }
}
