//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom
{
    /// <summary>  
    /// Abstraction of mapping operations
    /// </summary>
    public abstract class OperationMapper<T> : OperationBase
    {
        /// <summary>
        /// Key defined for content attached to the message (mapped object)
        /// </summary>
        public virtual string ContentKey => null;

        public override void Execute(IPipelineMessage input)
        {
            var result = Mapper(input);
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

        public abstract T Mapper(IPipelineMessage input);
    }

    /// <summary>  
    /// Abstraction of mapping operations
    /// </summary>
    public abstract class OperationMapper<T, U> : OperationBase
    {
        /// <summary>
        /// Key defined for content attached to the message (mapped object)
        /// </summary>
        public virtual string ContentKey => null;

        /// <summary>
        /// Key to get source content
        /// </summary>
        public virtual string SourceKey => null;

        public override void Execute(IPipelineMessage input)
        {
            T content = default;
            if (!string.IsNullOrEmpty(SourceKey) && !input.HasContent(SourceKey))
                content = input.GetContent<T>(SourceKey);
            else if (!input.HasContent<T>())
                content = input.GetContent<T>();

            var result = Mapper(content);
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

        public abstract U Mapper(T content);
    }
}
