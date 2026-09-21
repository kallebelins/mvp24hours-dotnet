//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

public class AuthorizedAdminCommand : IMediatorCommand<string>, IAuthorized
{
    public IEnumerable<string> RequiredRoles => ["Admin"];
}

public class AuthorizedAdminCommandHandler : IMediatorCommandHandler<AuthorizedAdminCommand, string>
{
    public Task<string> Handle(AuthorizedAdminCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("admin-ok");
    }
}

public class RestrictedCommand : IMediatorCommand<string>, IAuthorized
{
    public IEnumerable<string> RequiredRoles => ["SuperAdmin"];

    public IEnumerable<string> RequiredPermissions => ["Orders.Delete"];
}

public class RestrictedCommandHandler : IMediatorCommandHandler<RestrictedCommand, string>
{
    public Task<string> Handle(RestrictedCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("restricted-ok");
    }
}

public class CacheableTestQuery : IMediatorQuery<string>, ICacheable
{
    public string Id { get; init; } = "1";

    public string? CacheKey => $"query:{Id}";
}

public class CacheableTestQueryHandler : IMediatorQueryHandler<CacheableTestQuery, string>
{
    public static int ExecutionCount { get; set; }

    public Task<string> Handle(CacheableTestQuery request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult($"value-{request.Id}-{ExecutionCount}");
    }
}

public class NonCacheableTestQuery : IMediatorQuery<string>
{
    public string Id { get; init; } = "1";
}

public class NonCacheableTestQueryHandler : IMediatorQueryHandler<NonCacheableTestQuery, string>
{
    public static int ExecutionCount { get; set; }

    public Task<string> Handle(NonCacheableTestQuery request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult($"value-{request.Id}-{ExecutionCount}");
    }
}

public class CacheInvalidatingCommand : IMediatorCommand<string>, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => ["query:1"];
}

public class CacheInvalidatingCommandHandler : IMediatorCommandHandler<CacheInvalidatingCommand, string>
{
    public Task<string> Handle(CacheInvalidatingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("invalidated");
    }
}

public class NonRetryableTestCommand : IMediatorCommand<string>
{
    public static int AttemptCount { get; set; }

    public int FailUntilAttempt { get; init; } = 2;
}

public class NonRetryableTestCommandHandler : IMediatorCommandHandler<NonRetryableTestCommand, string>
{
    public Task<string> Handle(NonRetryableTestCommand request, CancellationToken cancellationToken)
    {
        NonRetryableTestCommand.AttemptCount++;
        if (NonRetryableTestCommand.AttemptCount < request.FailUntilAttempt)
        {
            throw new TimeoutException("Transient failure");
        }

        return Task.FromResult("no-retry-success");
    }
}

public class RetryTestCommand : IMediatorCommand<string>, IRetryable
{
    public static int AttemptCount { get; set; }

    public int FailUntilAttempt { get; init; } = 2;

    public int MaxRetryAttempts => 3;

    public TimeSpan RetryDelay => TimeSpan.FromMilliseconds(1);

    public bool UseExponentialBackoff => false;
}

public class RetryTestCommandHandler : IMediatorCommandHandler<RetryTestCommand, string>
{
    public Task<string> Handle(RetryTestCommand request, CancellationToken cancellationToken)
    {
        RetryTestCommand.AttemptCount++;
        if (RetryTestCommand.AttemptCount < request.FailUntilAttempt)
        {
            throw new TimeoutException("Transient failure");
        }

        return Task.FromResult("retry-success");
    }
}

public class NonIdempotentTestCommand : IMediatorCommand<int>
{
    public string OperationKey { get; init; } = "op-1";
}

public class NonIdempotentTestCommandHandler : IMediatorCommandHandler<NonIdempotentTestCommand, int>
{
    public static int ExecutionCount { get; set; }

    public Task<int> Handle(NonIdempotentTestCommand request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult(ExecutionCount * 10);
    }
}

public class IdempotentTestCommand : IMediatorCommand<int>, IIdempotentCommand
{
    public string OperationKey { get; init; } = "op-1";

    public string? IdempotencyKey => OperationKey;
}

public class IdempotentTestCommandHandler : IMediatorCommandHandler<IdempotentTestCommand, int>
{
    public static int ExecutionCount { get; set; }

    public Task<int> Handle(IdempotentTestCommand request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult(ExecutionCount * 10);
    }
}

public class ResilientTestQuery : IMediatorQuery<string>, INativeResilient
{
    public static int ExecutionCount { get; set; }
}

public class ResilientTestQueryHandler : IMediatorQueryHandler<ResilientTestQuery, string>
{
    public Task<string> Handle(ResilientTestQuery request, CancellationToken cancellationToken)
    {
        ResilientTestQuery.ExecutionCount++;
        return Task.FromResult("resilient-ok");
    }
}

public class FailingResilientTestQuery : IMediatorQuery<string>, INativeResilient
{
    public NativeCqrsResilienceOptions? ResilienceOptions => new()
    {
        EnableRetry = true,
        RetryMaxAttempts = 2,
        RetryDelay = TimeSpan.FromMilliseconds(1),
        EnableCircuitBreaker = false,
        EnableTimeout = false
    };
}

public class FailingResilientTestQueryHandler : IMediatorQueryHandler<FailingResilientTestQuery, string>
{
    public static int ExecutionCount { get; set; }

    public Task<string> Handle(FailingResilientTestQuery request, CancellationToken cancellationToken)
    {
        FailingResilientTestQueryHandler.ExecutionCount++;
        if (FailingResilientTestQueryHandler.ExecutionCount < 2)
        {
            throw new HttpRequestException("Network error");
        }

        return Task.FromResult("recovered");
    }
}

public sealed class TestUserContext : IUserContext
{
    public TestUserContext(bool isAuthenticated, params string[] roles)
    {
        IsAuthenticated = isAuthenticated;
        Roles = roles;
        Permissions = [];
    }

    public TestUserContext(bool isAuthenticated, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        IsAuthenticated = isAuthenticated;
        Roles = roles;
        Permissions = permissions;
    }

    public bool IsAuthenticated { get; }

    public string? UserId => IsAuthenticated ? "user-1" : null;

    public string? UserName => IsAuthenticated ? "test-user" : null;

    public IEnumerable<string> Roles { get; }

    public IEnumerable<string> Permissions { get; }
}
