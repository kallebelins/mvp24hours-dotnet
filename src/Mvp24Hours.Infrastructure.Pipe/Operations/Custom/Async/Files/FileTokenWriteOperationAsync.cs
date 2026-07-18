//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom.Files;

/// <summary>
/// Operation for writing file log token
/// </summary>
public class FileTokenWriteOperationAsync<T>(string _filePath) : OperationBaseAsync where T : class
{
    public override bool IsRequired => true;

    public virtual string FilePath => _filePath;

    public override Task ExecuteAsync(IPipelineMessage input)
    {
        if (FilePath.HasValue())
        {
            T dto = input.GetContent<T>();
            if (dto != null)
            {
                FileLogHelper.WriteLogToken(input.Token, typeof(T).Name.ToLower(), dto, FilePath);
            }
        }
        return Task.CompletedTask;
    }
}
