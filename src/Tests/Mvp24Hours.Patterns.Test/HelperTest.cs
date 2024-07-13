//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Patterns.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class HelperTest
    {
        [Fact, Priority(1)]
        public void ToDeserializeBusinessResult()
        {
            // arrange
            var result = "{\"data\":\"77ec9da6-71c8-4be5-95e7-fc70fae45320\",\"messages\":[{\"key\":\"OPERATION_SUCCESS\",\"message\":\"Operação realizada com sucesso.\",\"type\":\"Success\"}],\"hasErrors\":false}".ToDeserializeBusinessResult<Guid>();
            // assert
            Assert.NotNull(result);
        }
    }
}
