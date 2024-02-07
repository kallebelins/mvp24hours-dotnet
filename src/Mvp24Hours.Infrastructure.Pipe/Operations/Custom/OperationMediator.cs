//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom
{
    /// <summary>  
    /// Abstraction of mediators and mapping operations
    /// </summary>
    public abstract class OperationMediator<T, U> : OperationBase
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

        public override void Execute(IPipelineMessage input)
        {
            ModelRequest = MapperRequest(input);
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

            Mediator(input);

            ModelResponse = MapperResponse(input);
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

        public abstract T MapperRequest(IPipelineMessage input);
        public abstract void Mediator(IPipelineMessage input);
        public abstract U MapperResponse(IPipelineMessage input);
    }
}
