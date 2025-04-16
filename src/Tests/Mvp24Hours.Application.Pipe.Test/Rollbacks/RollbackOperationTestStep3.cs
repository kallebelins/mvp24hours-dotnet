using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Rollbacks
{
    public class RollbackOperationTestStep3 : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
            input.AddContent("key-test-step3", 3);
            throw new System.Exception();
        }

        public override void Rollback(IPipelineMessage input)
        {
            input.AddContent("key-test-rollback-step3", 30);
        }
    }
}
