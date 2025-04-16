using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Pipe.Test.Rollbacks
{
    public class RollbackOperationTestAsyncStep1 : OperationBaseAsync
    {
        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            input.AddContent("key-test-step1", 1);
            await Task.CompletedTask;
        }

        public override async Task RollbackAsync(IPipelineMessage input)
        {
            input.AddContent("key-test-rollback-step1", 10);
            await Task.CompletedTask;
        }
    }
}
