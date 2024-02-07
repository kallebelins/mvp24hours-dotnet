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
    /// Log writing operation
    /// </summary>
    public class FileLogWriteOperationAsync(string _filePath) : OperationBaseAsync
    {
        public override bool IsRequired => true;
        public virtual string FilePath => _filePath;

        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            if (FilePath.HasValue())
            {
                FileLogHelper.WriteLog(input.GetContentAll(), FilePath, "message", $"Token: {input.Token} / IsSuccess: {input.IsFaulty} / Warnings: {string.Join('/', input.Messages)}");
            }
            await Task.CompletedTask;
        }
    }
}
