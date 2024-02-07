//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure.Logging
{
    public interface ITelemetryService
    {
        void Execute(string eventName, params object[] args);
    }
}
