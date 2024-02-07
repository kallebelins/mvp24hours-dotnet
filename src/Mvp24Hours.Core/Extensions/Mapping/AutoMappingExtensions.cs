//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class AutoMappingExtensions
    {
        /// <summary>
        /// Maps properties as ignored.
        /// </summary>
        public static IMappingExpression<TSource, TDestination> MapIgnore<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> map,
            Expression<Func<TDestination, object>> selector)
        {
            if (map == null)
                throw new ArgumentException(null, nameof(map));
            return map.ForMember(selector, config => config.Ignore());
        }

        /// <summary>
        /// Sets mapping from source property to destination property. Convenient extension method. 
        /// </summary>
        public static IMappingExpression<TSource, TDestination> MapProperty<TSource, TDestination, TProperty>(
            this IMappingExpression<TSource, TDestination> map,
            Expression<Func<TSource, TProperty>> sourceMember,
            Expression<Func<TDestination, object>> targetMember)
        {
            if (map == null)
                throw new ArgumentException(null, nameof(map));
            return map.ForMember(targetMember, opt => opt.MapFrom(sourceMember));
        }

        /// <summary>
        /// Convert instance to mapped object
        /// </summary>
        public static IPagingResult<TDestination> MapPagingTo<TSource, TDestination>(this IMapper mapper, IPagingResult<TSource> source)
        {
            if (source == null)
            {
                return default;
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
                        source.Messages.ToList()
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
        public static IBusinessResult<TDestination> MapBusinessTo<TSource, TDestination>(this IMapper mapper, IBusinessResult<TSource> source)
        {
            if (source == null)
            {
                return default;
            }

            if (mapper == null)
                throw new ArgumentException(null, nameof(mapper));

            if (source.Messages.AnySafe())
            {
                return mapper
                    .Map<TDestination>(source.Data)
                    .ToBusiness(source.Messages.ToList());
            }
            else
            {
                return mapper
                    .Map<TDestination>(source.Data)
                    .ToBusiness();
            }
        }

        /// <summary>
        /// Maps the specified sources to the specified destination type.
        /// </summary>
        /// <typeparam name="T">The type of the destination</typeparam>
        /// <param name="sources">The sources.</param>
        /// <returns></returns>
        /// <example>
        /// Retrieve the person, address and comment entities 
        /// and map them on to a person view model entity.
        /// 
        /// var personId = 23;
        /// var person = _personTasks.GetPerson(personId);
        /// var address = _personTasks.GetAddress(personId);
        /// var comment = _personTasks.GetComment(personId);
        /// 
        /// var personViewModel = EntityMapper.Map<PersonViewModel>(person, address, comment);
        /// </example>
        public static T MapMerge<T>(this IMapper mapper, IList<object> sources) where T : class
        {
            return MapMerge<T>(mapper, sources?.ToArray());
        }

        /// <summary>
        /// Maps the specified sources to the specified destination type.
        /// </summary>
        /// <typeparam name="T">The type of the destination</typeparam>
        /// <param name="sources">The sources.</param>
        /// <returns></returns>
        /// <example>
        /// Retrieve the person, address and comment entities 
        /// and map them on to a person view model entity.
        /// 
        /// var personId = 23;
        /// var person = _personTasks.GetPerson(personId);
        /// var address = _personTasks.GetAddress(personId);
        /// var comment = _personTasks.GetComment(personId);
        /// 
        /// var personViewModel = EntityMapper.Map<PersonViewModel>(person, address, comment);
        /// </example>
        public static T MapMerge<T>(this IMapper mapper, params object[] sources) where T : class
        {
            // If there are no sources just return the destination object
            if (mapper == null || !sources.AnySafe())
            {
                return default;
            }

            // Get the inital source and map it
            var initialSource = sources[0];
            var mappingResult = Map<T>(mapper, initialSource);

            // Now map the remaining source objects
            if (sources.Length > 1)
            {
                Map(mapper, mappingResult, sources.Skip(1).ToArray());
            }

            // return the destination object
            return mappingResult;
        }

        /// <summary>
        /// Maps the specified sources to the specified destination.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="sources">The sources.</param>
        private static void Map(IMapper mapper, object destination, params object[] sources)
        {
            // If there are no sources just return the destination object
            if (sources.Length == 0)
            {
                return;
            }

            // Get the destination type
            var destinationType = destination.GetType();

            // Itereate through all of the sources...
            foreach (var source in sources)
            {
                // ... get the source type and map the source to the destination
                var sourceType = source.GetType();
                mapper.Map(source, destination, sourceType, destinationType);
            }
        }

        /// <summary>
        /// Maps the specified source to the destination.
        /// </summary>
        /// <typeparam name="T">type of teh destination</typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        private static T Map<T>(IMapper mapper, object source) where T : class
        {
            // Get thr source and destination types
            var destinationType = typeof(T);
            var sourceType = source.GetType();

            // Get the destination using AutoMapper's Map
            var mappingResult = mapper.Map(source, sourceType, destinationType);

            // Return the destination
            return mappingResult as T;
        }
    }
}
