//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Runtime.CompilerServices;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Stream request for getting items incrementally.
/// </summary>
public class GetItemsStreamRequest : IStreamRequest<int>
{
    public int Count { get; set; } = 10;
    public int DelayMs { get; set; } = 0;
}

/// <summary>
/// Handler for GetItemsStreamRequest.
/// </summary>
public class GetItemsStreamHandler : IStreamRequestHandler<GetItemsStreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        GetItemsStreamRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (request.DelayMs > 0)
                await Task.Delay(request.DelayMs, cancellationToken);

            yield return i;
        }
    }
}

/// <summary>
/// Stream request for getting strings.
/// </summary>
public class GetNamesStreamRequest : IStreamRequest<string>
{
    public List<string> Names { get; set; } = new();
}

/// <summary>
/// Handler for GetNamesStreamRequest.
/// </summary>
public class GetNamesStreamHandler : IStreamRequestHandler<GetNamesStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(
        GetNamesStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var name in request.Names)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return name;
            await Task.Yield();
        }
    }
}

