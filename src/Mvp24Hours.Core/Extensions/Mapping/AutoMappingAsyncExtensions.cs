//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class AutoMappingAsyncExtensions
    {
        /// <summary>
        /// Convert instance to mapped object
        /// </summary>
        public static async Task<IPagingResult<TDestination>?> MapPagingToAsync<TSource, TDestination>(this IMapper mapper, Task<IPagingResult<TSource>?> sourceAsync)
        {
            IPagingResult<TSource>? source = await sourceAsync;

            if (source == null)
            {
                return null;
            }

            if (mapper == null)
                throw new ArgumentException(null, nameof(mapper));

            if (source.Messages.AnySafe())
            {
                return mapper
                    .Map<TDestination>(source.Data)
                    .ToBusinessPaging(
                        source.Paging,
                        source.Summary,
                        (source.Messages ?? []).ToList()
                    );
            }
            else
            {
                return mapper
                    .Map<TDestination>(source.Data)
                    .ToBusinessPaging(
                        source.Paging,
                        source.Summary
                    );
            }
        }

        /// <summary>
        /// Convert instance to mapped object
        /// </summary>
        public static async Task<IBusinessResult<TDestination>?> MapBusinessToAsync<TSource, TDestination>(this IMapper mapper, Task<IBusinessResult<TSource>?> sourceAsync)
        {
            IBusinessResult<TSource>? source = await sourceAsync;

            if (source == null)
            {
                return null;
            }

            if (mapper == null)
                throw new ArgumentException(null, nameof(mapper));

            if (source.Messages.AnySafe())
            {
                return mapper
                    .Map<TDestination>(source.Data)
                    .ToBusiness((source.Messages ?? []).ToList());
            }
            else
            {
                return mapper
                    .Map<TDestination>(source.Data)
                    .ToBusiness();
            }
        }
    }
}
