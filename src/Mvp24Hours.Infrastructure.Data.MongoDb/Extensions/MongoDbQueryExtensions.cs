//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    public static class MongoDbQueryExtensions
    {
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? default
                : source.Any().TaskResult();
        }
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? default
                : source.Count().TaskResult();
        }
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? default
                : source.SingleOrDefault().TaskResult();
        }
        public static Task<IList<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? default
                : ((IList<TSource>)[.. source]).TaskResult();
        }

    }
}
