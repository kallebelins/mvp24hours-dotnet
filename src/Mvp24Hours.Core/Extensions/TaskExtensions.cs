//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    public static class TaskExtensions
    {
        private static readonly TaskFactory _taskFactory = new(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        /// <summary>
        /// Executes an async Task method which has a void return value synchronously
        /// USAGE: AsyncUtil.RunSync(() => AsyncMethod());
        /// </summary>
        public static void RunSync(this Func<Task> task)
            => _taskFactory
                .StartNew(task)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        /// <summary>
        /// Executes an async Task<T> method which has a T return type synchronously
        /// USAGE: T result = AsyncUtil.RunSync(() => AsyncMethod<T>());
        /// </summary>
        public static TResult RunSync<TResult>(this Func<Task<TResult>> task)
            => _taskFactory
                .StartNew(task)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        public static Task<TResult> TaskResult<TResult>(this TResult obj)
        {
            return Task.FromResult(obj);
        }

        public static async Task TaskComplete()
        {
            await Task.Yield();
        }

        public static async Task<bool> IsGreaterThanZeroAsync(this Task<int> value)
        {
            return (await value) > 0;
        }

        public static async Task<bool> IsEqualToZeroAsync(this Task<int> value)
        {
            return (await value) == 0;
        }

        public static async Task<bool> IsLessThanZeroAsync(this Task<int> value)
        {
            return (await value) < 0;
        }

        public static async Task<bool> IsTrueAsync(this Task<bool> value)
        {
            return await value;
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this Task<IList<T>> valueAsync, Func<T, bool> predicate = null)
        {
            var value = await valueAsync;
            return predicate == null ? value.FirstOrDefault() : value.FirstOrDefault(predicate);
        }

        public static async Task<T> LastOrDefaultAsync<T>(this Task<IList<T>> valueAsync, Func<T, bool> predicate = null)
        {
            var value = await valueAsync;
            return predicate == null ? value.LastOrDefault() : value.LastOrDefault(predicate);
        }

        public static async Task<T> ElementAtOrDefaultAsync<T>(this Task<IList<T>> valueAsync, int index)
        {
            var value = await valueAsync;
            return value.ElementAtOrDefault(index);
        }
    }
}
