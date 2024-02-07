using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Operations
{
    public class OperationTest : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
            input.AddContent("key-test", 1);
        }
    }
}
