using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Rollbacks
{
    public class RollbackOperationTestStep2 : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
            input.AddContent("key-test-step2", 2);
        }

        public override void Rollback(IPipelineMessage input)
        {
            RollbackTestContext.Results.Add("key-test-rollback-step2");
            input.AddContent("key-test-rollback-step2", 20);
        }
    }
}
