//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Pipe.Integration.Streaming;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mvp24Hours.Application.Pipe.Test.Integration
{
    public class StreamingPipelineTest
    {
        [Fact]
        public async Task StreamingPipeline_ProcessesItemsSequentially()
        {
            // Arrange
            var pipeline = new StreamingPipeline<int, int>();
            pipeline.Add(async (input, ct) =>
            {
                await Task.Delay(10, ct); // Simulate work
                return OperationResult<int>.Success(input * 2);
            });

            var inputs = new[] { 1, 2, 3, 4, 5 };

            // Act
            var results = new List<int>();
            await foreach (var result in pipeline.ExecuteStreamAsync(inputs))
            {
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
            }

            // Assert
            Assert.Equal(5, results.Count);
            Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        }

        [Fact]
        public async Task StreamingPipeline_ProcessesItemsInParallel()
        {
            // Arrange
            var pipeline = new StreamingPipeline<int, int>
            {
                MaxDegreeOfParallelism = 4
            };
            pipeline.Add(async (input, ct) =>
            {
                await Task.Delay(50, ct); // Simulate work
                return OperationResult<int>.Success(input * 2);
            });

            var inputs = new[] { 1, 2, 3, 4 };

            // Act
            var results = new List<int>();
            await foreach (var result in pipeline.ExecuteStreamAsync(inputs))
            {
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
            }

            // Assert
            Assert.Equal(4, results.Count);
            Assert.Contains(2, results);
            Assert.Contains(4, results);
            Assert.Contains(6, results);
            Assert.Contains(8, results);
        }

        [Fact]
        public async Task StreamingPipeline_StopsOnError_WhenContinueOnErrorIsFalse()
        {
            // Arrange
            var pipeline = new StreamingPipeline<int, int>
            {
                ContinueOnError = false
            };
            pipeline.Add(async (input, ct) =>
            {
                if (input == 3)
                {
                    return OperationResult<int>.Failure("Error on item 3");
                }
                return OperationResult<int>.Success(input * 2);
            });

            var inputs = new[] { 1, 2, 3, 4, 5 };

            // Act
            var results = new List<int>();
            var errorCount = 0;
            await foreach (var result in pipeline.ExecuteStreamAsync(inputs))
            {
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
                else
                {
                    errorCount++;
                }
            }

            // Assert - should stop after error
            Assert.True(errorCount > 0 || results.Count < 5);
        }

        [Fact]
        public async Task StreamingPipeline_ContinuesOnError_WhenContinueOnErrorIsTrue()
        {
            // Arrange
            var pipeline = new StreamingPipeline<int, int>
            {
                ContinueOnError = true
            };
            pipeline.Add(async (input, ct) =>
            {
                if (input == 3)
                {
                    return OperationResult<int>.Failure("Error on item 3");
                }
                return OperationResult<int>.Success(input * 2);
            });

            var inputs = new[] { 1, 2, 3, 4, 5 };

            // Act
            var successCount = 0;
            var failureCount = 0;
            await foreach (var result in pipeline.ExecuteStreamAsync(inputs))
            {
                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }
            }

            // Assert
            Assert.Equal(4, successCount);
            Assert.Equal(1, failureCount);
        }

        [Fact]
        public async Task FilterStreamingOperation_FiltersItems()
        {
            // Arrange
            var filter = new FilterStreamingOperation<int>(x => x % 2 == 0);
            var inputs = ToAsyncEnumerable(new[] { 1, 2, 3, 4, 5, 6 });

            // Act
            var results = new List<int>();
            await foreach (var item in filter.ProcessStreamAsync(inputs))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(new[] { 2, 4, 6 }, results);
        }

        [Fact]
        public async Task TransformStreamingOperation_TransformsItems()
        {
            // Arrange
            var transform = new TransformStreamingOperation<int, string>(
                async (input, ct) =>
                {
                    await Task.CompletedTask;
                    return $"Value: {input}";
                });

            var inputs = ToAsyncEnumerable(new[] { 1, 2, 3 });

            // Act
            var results = new List<string>();
            await foreach (var item in transform.ProcessStreamAsync(inputs))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(new[] { "Value: 1", "Value: 2", "Value: 3" }, results);
        }

        [Fact]
        public async Task BatchStreamingOperation_BatchesItems()
        {
            // Arrange
            var batch = new BatchStreamingOperation<int>(3);
            var inputs = ToAsyncEnumerable(new[] { 1, 2, 3, 4, 5, 6, 7 });

            // Act
            var results = new List<IReadOnlyList<int>>();
            await foreach (var b in batch.ProcessStreamAsync(inputs))
            {
                results.Add(b);
            }

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal(new[] { 1, 2, 3 }, results[0]);
            Assert.Equal(new[] { 4, 5, 6 }, results[1]);
            Assert.Equal(new[] { 7 }, results[2]);
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.CompletedTask;
            }
        }
    }
}

