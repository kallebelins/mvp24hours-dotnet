//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom
{
    /// <summary>  
    /// Abstraction of mediators and mapping operations
    /// </summary>
    public abstract class OperationMediatorAsync<T, U> : OperationBaseAsync
    {
        /// <summary>
        /// Key defined for the content attached to the message (mapped object) for request
        /// </summary>
        public virtual string RequestContentKey => null;
        /// <summary>
        /// Key defined for the content attached to the message (mapped object) for response
        /// </summary>
        public virtual string ResponseContentKey => null;
        /// <summary>
        /// Represents the model used for requisition
        /// </summary>
        public virtual T ModelRequest { get; private set; }
        /// <summary>
        /// Represents the model used for response
        /// </summary>
        public virtual U ModelResponse { get; private set; }

        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            ModelRequest = await MapperRequest(input);
            if (ModelRequest != null)
            {
                if (RequestContentKey.HasValue())
                {
                    input.AddContent(RequestContentKey, ModelRequest);
                }
                else
                {
                    input.AddContent(ModelRequest);
                }
            }

            await Mediator(input);

            ModelResponse = await MapperResponse(input);
            if (ModelResponse != null)
            {
                if (ResponseContentKey.HasValue())
                {
                    input.AddContent(ResponseContentKey, ModelResponse);
                }
                else
                {
                    input.AddContent(ModelResponse);
                }
            }
        }

        public abstract Task<T> MapperRequest(IPipelineMessage input);
        public abstract Task Mediator(IPipelineMessage input);
        public abstract Task<U> MapperResponse(IPipelineMessage input);
    }
}
