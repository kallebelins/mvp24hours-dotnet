using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Pipe.Test.Operations
{
    public class OperationTestAsync : OperationBaseAsync
    {
        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            input.AddContent("key-test", 1);
            await Task.CompletedTask;
        }
    }
}
