//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom.Files
{
    /// <summary>
    /// Operation to read file log token
    /// </summary>
    public class FileTokenReadOperation<T>(string _filePath) : OperationBase
    {
        public override bool IsRequired => true;

        public virtual string FilePath => _filePath;

        public override void Execute(IPipelineMessage input)
        {
            if (FilePath.HasValue())
            {
                var dto = FileLogHelper.ReadLogToken<T>(input.Token, typeof(T).Name.ToLower(), FilePath);
                if (dto != null)
                {
                    input.AddContent(dto);
                }
            }
        }
    }
}
