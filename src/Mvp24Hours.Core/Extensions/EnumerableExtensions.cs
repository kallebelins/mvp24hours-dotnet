//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool IsList(this object Value)
        {
            if (Value == null) return false;
            return Value is IEnumerable
                || Value is ICollection
                || Value is IList
                || (Value.GetType().IsGenericType
                        && Value.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
                || (Value.GetType().IsGenericType
                        && Value.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(ArrayList)));
        }

        public static bool IsDictionary(this object Value)
        {
            if (Value == null) return false;
            return Value is IDictionary &&
                   Value.GetType().IsGenericType &&
                   Value.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T t in list)
            {
                action(t);
                yield return t;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool AnyOrNotNull<T>(this IEnumerable<T> source)
        {
            if (source == null || !source.Any())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool AnyOrNotNull<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null || !source.Any(predicate))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool AnySafe<T>(this IEnumerable<T> source)
        {
            if (source != null && source.Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool AnySafe<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source != null && source.Any(predicate))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool ContainsKeySafe<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
        {
            if (source != null && source.ContainsKey(key))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<TSource> FirstOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> task, Func<TSource, bool> predicate = null)
        {
            var list = await task;
            return predicate == null ? list.FirstOrDefault() : list.FirstOrDefault(predicate);
        }

        public static async Task<TSource> LastOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> task, Func<TSource, bool> predicate = null)
        {
            var list = await task;
            return predicate == null ? list.LastOrDefault() : list.LastOrDefault(predicate);
        }

        public static async Task<TSource> ElementAtOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> task, int index)
        {
            var list = await task;
            return list.ElementAtOrDefault(index);
        }
    }
}
