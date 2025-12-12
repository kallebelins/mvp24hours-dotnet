//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Infrastructure.Data.EFCore.Converters;
using System;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring strongly-typed IDs in Entity Framework Core.
    /// </summary>
    public static class EntityIdModelBuilderExtensions
    {
        /// <summary>
        /// Configures a Guid-based strongly-typed ID property.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
        /// <param name="builder">The property builder.</param>
        /// <returns>The property builder for chaining.</returns>
        /// <example>
        /// <code>
        /// modelBuilder.Entity&lt;Customer&gt;()
        ///     .Property(c => c.Id)
        ///     .HasGuidEntityIdConversion&lt;Customer, CustomerId&gt;();
        /// </code>
        /// </example>
        public static PropertyBuilder<TId> HasGuidEntityIdConversion<TEntity, TId>(
            this PropertyBuilder<TId> builder)
            where TId : GuidEntityId<TId>
        {
            return builder.HasConversion(new GuidEntityIdValueConverter<TId>());
        }

        /// <summary>
        /// Configures an int-based strongly-typed ID property.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
        /// <param name="builder">The property builder.</param>
        /// <returns>The property builder for chaining.</returns>
        public static PropertyBuilder<TId> HasIntEntityIdConversion<TEntity, TId>(
            this PropertyBuilder<TId> builder)
            where TId : IntEntityId<TId>
        {
            return builder.HasConversion(new IntEntityIdValueConverter<TId>());
        }

        /// <summary>
        /// Configures a long-based strongly-typed ID property.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
        /// <param name="builder">The property builder.</param>
        /// <returns>The property builder for chaining.</returns>
        public static PropertyBuilder<TId> HasLongEntityIdConversion<TEntity, TId>(
            this PropertyBuilder<TId> builder)
            where TId : LongEntityId<TId>
        {
            return builder.HasConversion(new LongEntityIdValueConverter<TId>());
        }

        /// <summary>
        /// Configures a string-based strongly-typed ID property.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
        /// <param name="builder">The property builder.</param>
        /// <returns>The property builder for chaining.</returns>
        public static PropertyBuilder<TId> HasStringEntityIdConversion<TEntity, TId>(
            this PropertyBuilder<TId> builder)
            where TId : StringEntityId<TId>
        {
            return builder.HasConversion(new StringEntityIdValueConverter<TId>());
        }

        /// <summary>
        /// Configures a generic strongly-typed ID property.
        /// </summary>
        /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
        /// <typeparam name="TValue">The underlying value type.</typeparam>
        /// <param name="builder">The property builder.</param>
        /// <returns>The property builder for chaining.</returns>
        public static PropertyBuilder<TId> HasEntityIdConversion<TId, TValue>(
            this PropertyBuilder<TId> builder)
            where TId : EntityId<TId, TValue>
            where TValue : IEquatable<TValue>, IComparable<TValue>
        {
            return builder.HasConversion(new EntityIdValueConverter<TId, TValue>());
        }

        /// <summary>
        /// Automatically configures value converters for all strongly-typed ID properties
        /// in all entities registered in the model.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <remarks>
        /// This method scans all entity types and their properties, automatically
        /// applying the appropriate value converter for any property that inherits
        /// from one of the EntityId base classes.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnModelCreating(ModelBuilder modelBuilder)
        /// {
        ///     base.OnModelCreating(modelBuilder);
        ///     modelBuilder.ApplyStronglyTypedIdConversions();
        /// }
        /// </code>
        /// </example>
        public static ModelBuilder ApplyStronglyTypedIdConversions(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var propertyType = property.ClrType;

                    // Skip nullable types - get underlying type
                    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                    ValueConverter converter = null;

                    // Check if the property type is a strongly-typed ID
                    if (IsGuidEntityId(underlyingType))
                    {
                        var converterType = typeof(GuidEntityIdValueConverter<>).MakeGenericType(underlyingType);
                        converter = (ValueConverter)Activator.CreateInstance(converterType);
                    }
                    else if (IsIntEntityId(underlyingType))
                    {
                        var converterType = typeof(IntEntityIdValueConverter<>).MakeGenericType(underlyingType);
                        converter = (ValueConverter)Activator.CreateInstance(converterType);
                    }
                    else if (IsLongEntityId(underlyingType))
                    {
                        var converterType = typeof(LongEntityIdValueConverter<>).MakeGenericType(underlyingType);
                        converter = (ValueConverter)Activator.CreateInstance(converterType);
                    }
                    else if (IsStringEntityId(underlyingType))
                    {
                        var converterType = typeof(StringEntityIdValueConverter<>).MakeGenericType(underlyingType);
                        converter = (ValueConverter)Activator.CreateInstance(converterType);
                    }
                    else if (IsGenericEntityId(underlyingType, out var valueType))
                    {
                        var converterType = typeof(EntityIdValueConverter<,>).MakeGenericType(underlyingType, valueType);
                        converter = (ValueConverter)Activator.CreateInstance(converterType);
                    }

                    if (converter != null)
                    {
                        property.SetValueConverter(converter);
                    }
                }
            }

            return modelBuilder;
        }

        /// <summary>
        /// Configures value converters for all strongly-typed ID properties
        /// in the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="builder">The entity type builder.</param>
        /// <returns>The entity type builder for chaining.</returns>
        public static EntityTypeBuilder<TEntity> ApplyStronglyTypedIdConversions<TEntity>(
            this EntityTypeBuilder<TEntity> builder)
            where TEntity : class
        {
            var entityType = typeof(TEntity);

            foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                if (IsGuidEntityId(underlyingType))
                {
                    var converterType = typeof(GuidEntityIdValueConverter<>).MakeGenericType(underlyingType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType);
                    builder.Property(property.Name).HasConversion(converter);
                }
                else if (IsIntEntityId(underlyingType))
                {
                    var converterType = typeof(IntEntityIdValueConverter<>).MakeGenericType(underlyingType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType);
                    builder.Property(property.Name).HasConversion(converter);
                }
                else if (IsLongEntityId(underlyingType))
                {
                    var converterType = typeof(LongEntityIdValueConverter<>).MakeGenericType(underlyingType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType);
                    builder.Property(property.Name).HasConversion(converter);
                }
                else if (IsStringEntityId(underlyingType))
                {
                    var converterType = typeof(StringEntityIdValueConverter<>).MakeGenericType(underlyingType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType);
                    builder.Property(property.Name).HasConversion(converter);
                }
                else if (IsGenericEntityId(underlyingType, out var valueType))
                {
                    var converterType = typeof(EntityIdValueConverter<,>).MakeGenericType(underlyingType, valueType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType);
                    builder.Property(property.Name).HasConversion(converter);
                }
            }

            return builder;
        }

        private static bool IsGuidEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(GuidEntityId<>);
        }

        private static bool IsIntEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(IntEntityId<>);
        }

        private static bool IsLongEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(LongEntityId<>);
        }

        private static bool IsStringEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(StringEntityId<>);
        }

        private static bool IsGenericEntityId(Type type, out Type valueType)
        {
            valueType = null;
            var baseType = type.BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition() == typeof(EntityId<,>))
                {
                    valueType = baseType.GetGenericArguments()[1];
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}

