//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom
{
    /// <summary>  
    /// Abstraction of mapping operations
    /// </summary>
    public abstract class OperationValidatorAsync : OperationBaseAsync
    {
        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            if (!await IsValid(input))
            {
                input.SetLock();
            }
        }

        public abstract Task<bool> IsValid(IPipelineMessage input);
    }
}
