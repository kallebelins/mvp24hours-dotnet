//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Integration.Caching;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mvp24Hours.Application.Pipe.Test.Integration
{
    public class CachingOperationTest
    {
        [Fact]
        public async Task CachingOperation_CachesResult_OnSuccess()
        {
            // Arrange
            var cache = CreateMemoryCache();
            var executionCount = 0;
            var innerOperation = new TestOperation(() =>
            {
                executionCount++;
                return OperationResult<string>.Success("Test Result");
            });

            var cachingOperation = new CachingOperation<int, string>(
                innerOperation,
                cache,
                input => $"test-{input}");

            // Act
            var result1 = await cachingOperation.ExecuteAsync(1);
            var result2 = await cachingOperation.ExecuteAsync(1);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal("Test Result", result1.Value);
            Assert.Equal("Test Result", result2.Value);
            Assert.Equal(1, executionCount); // Should only execute once
        }

        [Fact]
        public async Task CachingOperation_DifferentKeys_ExecutesMultipleTimes()
        {
            // Arrange
            var cache = CreateMemoryCache();
            var executionCount = 0;
            var innerOperation = new TestOperation(() =>
            {
                executionCount++;
                return OperationResult<string>.Success($"Result {executionCount}");
            });

            var cachingOperation = new CachingOperation<int, string>(
                innerOperation,
                cache,
                input => $"test-{input}");

            // Act
            var result1 = await cachingOperation.ExecuteAsync(1);
            var result2 = await cachingOperation.ExecuteAsync(2);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal(2, executionCount); // Should execute twice for different keys
        }

        [Fact]
        public async Task CachingOperation_DoesNotCacheFailures_ByDefault()
        {
            // Arrange
            var cache = CreateMemoryCache();
            var executionCount = 0;
            var innerOperation = new TestOperation(() =>
            {
                executionCount++;
                return OperationResult<string>.Failure("Error");
            });

            var cachingOperation = new CachingOperation<int, string>(
                innerOperation,
                cache,
                input => $"test-{input}",
                options: new CacheOperationOptions { CacheFailedResults = false });

            // Act
            var result1 = await cachingOperation.ExecuteAsync(1);
            var result2 = await cachingOperation.ExecuteAsync(1);

            // Assert
            Assert.False(result1.IsSuccess);
            Assert.False(result2.IsSuccess);
            Assert.Equal(2, executionCount); // Should execute twice since failures are not cached
        }

        [Fact]
        public async Task CachingOperation_CachesFailures_WhenEnabled()
        {
            // Arrange
            var cache = CreateMemoryCache();
            var executionCount = 0;
            var innerOperation = new TestOperation(() =>
            {
                executionCount++;
                return OperationResult<string>.Failure("Error");
            });

            var cachingOperation = new CachingOperation<int, string>(
                innerOperation,
                cache,
                input => $"test-{input}",
                options: new CacheOperationOptions { CacheFailedResults = true });

            // Act
            var result1 = await cachingOperation.ExecuteAsync(1);
            var result2 = await cachingOperation.ExecuteAsync(1);

            // Assert
            Assert.False(result1.IsSuccess);
            Assert.False(result2.IsSuccess);
            Assert.Equal(1, executionCount); // Should execute once since failures are cached
        }

        [Fact]
        public async Task CachingOperation_InvalidateCache_ClearsEntry()
        {
            // Arrange
            var cache = CreateMemoryCache();
            var executionCount = 0;
            var innerOperation = new TestOperation(() =>
            {
                executionCount++;
                return OperationResult<string>.Success($"Result {executionCount}");
            });

            var cachingOperation = new CachingOperation<int, string>(
                innerOperation,
                cache,
                input => $"test-{input}");

            // Act
            var result1 = await cachingOperation.ExecuteAsync(1);
            await cachingOperation.InvalidateCacheAsync(1);
            var result2 = await cachingOperation.ExecuteAsync(1);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal("Result 1", result1.Value);
            Assert.Equal("Result 2", result2.Value);
            Assert.Equal(2, executionCount); // Should execute twice after invalidation
        }

        private static IDistributedCache CreateMemoryCache()
        {
            var options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        private class TestOperation : ITypedOperationAsync<int, string>
        {
            private readonly Func<IOperationResult<string>> _execute;

            public TestOperation(Func<IOperationResult<string>> execute)
            {
                _execute = execute;
            }

            public bool IsRequired => false;

            public Task<IOperationResult<string>> ExecuteAsync(int input, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_execute());
            }

            public Task RollbackAsync(int input, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}

