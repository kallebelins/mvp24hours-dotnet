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
    /// Abstraction of asynchronous mapping operations
    /// </summary>
    public abstract class OperationMapperAsync<T> : OperationBaseAsync
    {
        /// <summary>
        /// Key defined for content attached to the message (mapped object)
        /// </summary>
        public virtual string ContentKey => null;

        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            var result = await MapperAsync(input);
            if (result != null)
            {
                if (string.IsNullOrEmpty(ContentKey))
                {
                    input.AddContent(result);
                }
                else
                {
                    input.AddContent(ContentKey, result);
                }
            }
        }

        public abstract Task<T> MapperAsync(IPipelineMessage input);
    }

    /// <summary>  
    /// Abstraction of asynchronous mapping operations
    /// </summary>
    public abstract class OperationMapperAsync<T, U> : OperationBaseAsync
    {
        /// <summary>
        /// Key defined for content attached to the message (mapped object)
        /// </summary>
        public virtual string ContentKey => null;

        /// <summary>
        /// Key to get source content
        /// </summary>
        public virtual string SourceKey => null;

        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            T content = default;
            if (!string.IsNullOrEmpty(SourceKey) && !input.HasContent(SourceKey))
                content = input.GetContent<T>(SourceKey);
            else if (!input.HasContent<T>())
                content = input.GetContent<T>();

            var result = await MapperAsync(content);
            if (result != null)
            {
                if (string.IsNullOrEmpty(ContentKey))
                {
                    input.AddContent(result);
                }
                else
                {
                    input.AddContent(ContentKey, result);
                }
            }
        }

        public abstract Task<U> MapperAsync(T content);
    }
}
