//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom.Files
{
    /// <summary>
    /// Operation to read file log token
    /// </summary>
    public class FileTokenReadOperationAsync<T>(string _filePath) : OperationBaseAsync
    {
        public override bool IsRequired => true;
        private readonly string filePath = _filePath;
        public virtual string FilePath => filePath;

        public override Task ExecuteAsync(IPipelineMessage input)
        {
            if (FilePath.HasValue())
            {
                var dto = FileLogHelper.ReadLogToken<T>(input.Token, typeof(T).Name.ToLower(), FilePath);
                if (dto != null)
                {
                    input.AddContent(dto);
                }
            }
            return Task.CompletedTask;
        }
    }
}
