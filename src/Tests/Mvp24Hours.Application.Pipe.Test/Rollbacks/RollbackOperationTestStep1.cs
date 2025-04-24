using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Rollbacks
{
    public class RollbackOperationTestStep1 : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
            input.AddContent("key-test-step1", 1);
        }

        public override void Rollback(IPipelineMessage input)
        {
            RollbackTestContext.Results.Add("key-test-rollback-step1");
            input.AddContent("key-test-rollback-step1", 10);
        }
    }
}
