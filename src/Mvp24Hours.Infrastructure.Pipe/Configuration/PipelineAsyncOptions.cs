//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;

namespace Mvp24Hours.Infrastructure.Pipe.Configuration
{
    [Serializable]
    public sealed class PipelineAsyncOptions
    {
        public bool IsBreakOnFail { get; set; }
    }
}
