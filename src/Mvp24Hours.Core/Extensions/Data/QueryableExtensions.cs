//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Creates sort expression
        /// </summary>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderExpression(propertyName, false);
        }
        /// <summary>
        /// Adds sort expression
        /// </summary>
        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return source.OrderExpression(propertyName, true);
        }

        /// <summary>
        /// Sorting expression
        /// </summary>
        static IOrderedQueryable<T> OrderExpression<T>(this IQueryable<T> source, string propertyName, bool isThenBy)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            bool isDescending = false;
            propertyName = propertyName.Trim().ToLower();

            if (propertyName.Contains('_') || propertyName.Contains(' '))
            {
                isDescending = propertyName.EndsWith("_desc")
                    || propertyName.EndsWith(" desc");
                propertyName = propertyName
                    .RemoveEnd("_asc").RemoveEnd(" asc")
                    .RemoveEnd("_desc").RemoveEnd(" desc");
            }

            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");

            PropertyInfo pi = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException("Property for ordering not found or key attribute does not exist in the entity.");
            Expression expr = Expression.Property(arg, pi);
            type = pi.PropertyType;

            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            string methodName = isDescending ? "OrderByDescending" : "OrderBy";
            if (isThenBy)
            {
                methodName = isDescending ? "ThenByDescending" : "ThenBy";
            }
            object result = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }
    }
}
