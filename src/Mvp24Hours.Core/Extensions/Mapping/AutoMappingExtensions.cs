//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using AutoMapper;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Extensions;

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
        {
            throw new ArgumentException(null, nameof(map));
        }

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
        {
            throw new ArgumentException(null, nameof(map));
        }

        return map.ForMember(targetMember, opt => opt.MapFrom(sourceMember));
    }

    /// <summary>
    /// Convert instance to mapped object
    /// </summary>
    public static IPagingResult<TDestination>? MapPagingTo<TSource, TDestination>(this IMapper mapper, IPagingResult<TSource>? source)
    {
        if (source == null)
        {
            return null;
        }

        if (mapper == null)
        {
            throw new ArgumentException(null, nameof(mapper));
        }

        if (source.Messages.AnySafe())
        {
            return mapper
                .Map<TDestination>(source.Data)
                .ToBusinessPaging(
                    source.Paging,
                    source.Summary,
                    [.. (source.Messages ?? [])]
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
    public static IBusinessResult<TDestination>? MapBusinessTo<TSource, TDestination>(this IMapper mapper, IBusinessResult<TSource>? source)
    {
        if (source == null)
        {
            return null;
        }

        if (mapper == null)
        {
            throw new ArgumentException(null, nameof(mapper));
        }

        if (source.Messages.AnySafe())
        {
            return mapper
                .Map<TDestination>(source.Data)
                .ToBusiness([.. (source.Messages ?? [])]);
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
    /// <param name="mapper">AutoMapper instance.</param>
    /// <param name="sources">The sources.</param>
    /// <returns></returns>
    /// <example><![CDATA[
    /// Retrieve the person, address and comment entities 
    /// and map them on to a person view model entity.
    /// 
    /// var personId = 23;
    /// var person = _personTasks.GetPerson(personId);
    /// var address = _personTasks.GetAddress(personId);
    /// var comment = _personTasks.GetComment(personId);
    /// 
    /// var personViewModel = EntityMapper.Map<PersonViewModel>(person, address, comment);
    /// ]]></example>
    public static T? MapMerge<T>(this IMapper mapper, IList<object>? sources) where T : class
    {
        return MapMerge<T>(mapper, sources?.ToArray());
    }

    /// <summary>
    /// Maps the specified sources to the specified destination type.
    /// </summary>
    /// <typeparam name="T">The type of the destination</typeparam>
    /// <param name="mapper">AutoMapper instance.</param>
    /// <param name="sources">The sources.</param>
    /// <returns></returns>
    /// <example><![CDATA[
    /// Retrieve the person, address and comment entities 
    /// and map them on to a person view model entity.
    /// 
    /// var personId = 23;
    /// var person = _personTasks.GetPerson(personId);
    /// var address = _personTasks.GetAddress(personId);
    /// var comment = _personTasks.GetComment(personId);
    /// 
    /// var personViewModel = EntityMapper.Map<PersonViewModel>(person, address, comment);
    /// ]]></example>
    public static T? MapMerge<T>(this IMapper mapper, params object?[]? sources) where T : class
    {
        // If there are no sources just return the destination object
        if (mapper == null || !sources.AnySafe())
        {
            return null;
        }

        // Get the inital source and map it
        object initialSource = sources![0]!;
        T? mappingResult = Map<T>(mapper, initialSource);

        // Now map the remaining source objects
        if (sources.Length > 1 && mappingResult != null)
        {
            Map(mapper, mappingResult, [.. sources.Skip(1)]);
        }

        // return the destination object
        return mappingResult;
    }

    /// <summary>
    /// Maps the specified sources to the specified destination.
    /// </summary>
    /// <param name="mapper"></param>
    /// <param name="destination">The destination.</param>
    /// <param name="sources">The sources.</param>
    private static void Map(IMapper mapper, object destination, params object?[] sources)
    {
        // If there are no sources just return the destination object
        if (sources.Length == 0)
        {
            return;
        }

        // Get the destination type
        Type destinationType = destination.GetType();

        // Itereate through all of the sources...
        foreach (object? source in sources)
        {
            if (source == null)
            {
                continue;
            }

            // ... get the source type and map the source to the destination
            Type sourceType = source.GetType();
            mapper.Map(source, destination, sourceType, destinationType);
        }
    }

    /// <summary>
    /// Maps the specified source to the destination.
    /// </summary>
    /// <typeparam name="T">type of teh destination</typeparam>
    /// <param name="mapper"></param>
    /// <param name="source">The source.</param>
    /// <returns></returns>
    private static T? Map<T>(IMapper mapper, object source) where T : class
    {
        // Get thr source and destination types
        Type destinationType = typeof(T);
        Type sourceType = source.GetType();

        // Get the destination using AutoMapper's Map
        object mappingResult = mapper.Map(source, sourceType, destinationType);

        // Return the destination
        return mappingResult as T;
    }
}
