//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mvp24Hours.Core.ValueObjects;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Converters
{
    /// <summary>
    /// Value converter for strongly-typed Guid-based entity IDs.
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type (must inherit from GuidEntityId).</typeparam>
    /// <example>
    /// <code>
    /// // Usage in OnModelCreating:
    /// modelBuilder.Entity&lt;Customer&gt;()
    ///     .Property(c => c.Id)
    ///     .HasConversion(new GuidEntityIdValueConverter&lt;CustomerId&gt;());
    /// </code>
    /// </example>
    public class GuidEntityIdValueConverter<TId> : ValueConverter<TId, Guid>
        where TId : GuidEntityId<TId>
    {
        private static readonly Func<Guid, TId> _createInstance = CreateFactory();

        /// <summary>
        /// Initializes a new instance of the converter.
        /// </summary>
        public GuidEntityIdValueConverter()
            : base(
                id => id.Value,
                value => _createInstance(value))
        {
        }

        private static Func<Guid, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(Guid) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a Guid parameter.");
            }

            var parameter = Expression.Parameter(typeof(Guid), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<Guid, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// Value converter for strongly-typed int-based entity IDs.
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type (must inherit from IntEntityId).</typeparam>
    /// <example>
    /// <code>
    /// // Usage in OnModelCreating:
    /// modelBuilder.Entity&lt;Product&gt;()
    ///     .Property(p => p.Id)
    ///     .HasConversion(new IntEntityIdValueConverter&lt;ProductId&gt;());
    /// </code>
    /// </example>
    public class IntEntityIdValueConverter<TId> : ValueConverter<TId, int>
        where TId : IntEntityId<TId>
    {
        private static readonly Func<int, TId> _createInstance = CreateFactory();

        /// <summary>
        /// Initializes a new instance of the converter.
        /// </summary>
        public IntEntityIdValueConverter()
            : base(
                id => id.Value,
                value => _createInstance(value))
        {
        }

        private static Func<int, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts an int parameter.");
            }

            var parameter = Expression.Parameter(typeof(int), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<int, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// Value converter for strongly-typed long-based entity IDs.
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type (must inherit from LongEntityId).</typeparam>
    /// <example>
    /// <code>
    /// // Usage in OnModelCreating:
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .Property(o => o.Id)
    ///     .HasConversion(new LongEntityIdValueConverter&lt;OrderId&gt;());
    /// </code>
    /// </example>
    public class LongEntityIdValueConverter<TId> : ValueConverter<TId, long>
        where TId : LongEntityId<TId>
    {
        private static readonly Func<long, TId> _createInstance = CreateFactory();

        /// <summary>
        /// Initializes a new instance of the converter.
        /// </summary>
        public LongEntityIdValueConverter()
            : base(
                id => id.Value,
                value => _createInstance(value))
        {
        }

        private static Func<long, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(long) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a long parameter.");
            }

            var parameter = Expression.Parameter(typeof(long), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<long, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// Value converter for strongly-typed string-based entity IDs.
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type (must inherit from StringEntityId).</typeparam>
    /// <example>
    /// <code>
    /// // Usage in OnModelCreating:
    /// modelBuilder.Entity&lt;Document&gt;()
    ///     .Property(d => d.Id)
    ///     .HasConversion(new StringEntityIdValueConverter&lt;DocumentId&gt;());
    /// </code>
    /// </example>
    public class StringEntityIdValueConverter<TId> : ValueConverter<TId, string>
        where TId : StringEntityId<TId>
    {
        private static readonly Func<string, TId> _createInstance = CreateFactory();

        /// <summary>
        /// Initializes a new instance of the converter.
        /// </summary>
        public StringEntityIdValueConverter()
            : base(
                id => id.Value,
                value => _createInstance(value))
        {
        }

        private static Func<string, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a string parameter.");
            }

            var parameter = Expression.Parameter(typeof(string), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<string, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// Generic value converter for any strongly-typed entity ID.
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    /// <typeparam name="TValue">The underlying value type.</typeparam>
    /// <example>
    /// <code>
    /// // Usage in OnModelCreating for custom value types:
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .Property(o => o.Id)
    ///     .HasConversion(new EntityIdValueConverter&lt;OrderId, Guid&gt;());
    /// </code>
    /// </example>
    public class EntityIdValueConverter<TId, TValue> : ValueConverter<TId, TValue>
        where TId : EntityId<TId, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        private static readonly Func<TValue, TId> _createInstance = CreateFactory();

        /// <summary>
        /// Initializes a new instance of the converter.
        /// </summary>
        public EntityIdValueConverter()
            : base(
                id => id.Value,
                value => _createInstance(value))
        {
        }

        private static Func<TValue, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(TValue) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a {typeof(TValue).Name} parameter.");
            }

            var parameter = Expression.Parameter(typeof(TValue), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<TValue, TId>>(body, parameter).Compile();
        }
    }
}

