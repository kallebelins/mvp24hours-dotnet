using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Pipe.Test.Rollbacks
{
    public class RollbackOperationTestAsyncStep2 : OperationBaseAsync
    {
        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            throw new System.Exception();
        }

        public override async Task RollbackAsync(IPipelineMessage input)
        {
            input.AddContent("key-test-rollback-step2", 20);
            await Task.CompletedTask;
        }
    }
}
