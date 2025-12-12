//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Converters
{
    /// <summary>
    /// Value converter that encrypts string values when storing and decrypts when reading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this converter for sensitive data like PII (Personal Identifiable Information),
    /// credit card numbers, social security numbers, etc.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Encrypted values cannot be used in queries unless using
    /// deterministic encryption with blind index.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In OnModelCreating:
    /// modelBuilder.Entity&lt;Customer&gt;()
    ///     .Property(c => c.CreditCardNumber)
    ///     .HasConversion(new EncryptedStringConverter(encryptionProvider));
    ///     
    /// // Or using extension method:
    /// modelBuilder.Entity&lt;Customer&gt;()
    ///     .Property(c => c.CreditCardNumber)
    ///     .HasEncryptedConversion(encryptionProvider);
    /// </code>
    /// </example>
    public class EncryptedStringConverter : ValueConverter<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptedStringConverter"/> class.
        /// </summary>
        /// <param name="encryptionProvider">The encryption provider to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when encryptionProvider is null.</exception>
        public EncryptedStringConverter(IEncryptionProvider encryptionProvider)
            : base(
                v => encryptionProvider.Encrypt(v),
                v => encryptionProvider.Decrypt(v))
        {
            if (encryptionProvider == null)
                throw new ArgumentNullException(nameof(encryptionProvider));
        }

        /// <summary>
        /// Creates a new converter instance with the specified encryption provider.
        /// </summary>
        /// <param name="encryptionProvider">The encryption provider.</param>
        /// <returns>A new EncryptedStringConverter instance.</returns>
        public static EncryptedStringConverter Create(IEncryptionProvider encryptionProvider)
        {
            return new EncryptedStringConverter(encryptionProvider);
        }
    }

    /// <summary>
    /// Value converter that encrypts nullable string values.
    /// </summary>
    public class NullableEncryptedStringConverter : ValueConverter<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableEncryptedStringConverter"/> class.
        /// </summary>
        /// <param name="encryptionProvider">The encryption provider to use.</param>
        public NullableEncryptedStringConverter(IEncryptionProvider encryptionProvider)
            : base(
                v => string.IsNullOrEmpty(v) ? v : encryptionProvider.Encrypt(v),
                v => string.IsNullOrEmpty(v) ? v : encryptionProvider.Decrypt(v))
        {
            if (encryptionProvider == null)
                throw new ArgumentNullException(nameof(encryptionProvider));
        }
    }

    /// <summary>
    /// Value converter that encrypts byte array values.
    /// </summary>
    /// <remarks>
    /// Use this for binary data like files, images, or serialized objects that need encryption.
    /// </remarks>
    public class EncryptedBinaryConverter : ValueConverter<byte[], byte[]>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptedBinaryConverter"/> class.
        /// </summary>
        /// <param name="encryptionProvider">The extended encryption provider to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when encryptionProvider is null.</exception>
        public EncryptedBinaryConverter(IExtendedEncryptionProvider encryptionProvider)
            : base(
                v => v == null || v.Length == 0 ? v : encryptionProvider.Encrypt(v),
                v => v == null || v.Length == 0 ? v : encryptionProvider.Decrypt(v))
        {
            if (encryptionProvider == null)
                throw new ArgumentNullException(nameof(encryptionProvider));
        }
    }

    /// <summary>
    /// Value converter for encrypted JSON data.
    /// </summary>
    /// <typeparam name="T">The type to serialize/deserialize.</typeparam>
    /// <remarks>
    /// Serializes objects to JSON, encrypts, and stores as string.
    /// Useful for encrypted complex types.
    /// </remarks>
    public class EncryptedJsonConverter<T> : ValueConverter<T, string> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptedJsonConverter{T}"/> class.
        /// </summary>
        /// <param name="encryptionProvider">The encryption provider to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when encryptionProvider is null.</exception>
        public EncryptedJsonConverter(IEncryptionProvider encryptionProvider)
            : base(
                v => EncryptJson(v, encryptionProvider),
                v => DecryptJson(v, encryptionProvider))
        {
            if (encryptionProvider == null)
                throw new ArgumentNullException(nameof(encryptionProvider));
        }

        private static string EncryptJson(T value, IEncryptionProvider provider)
        {
            if (value == null) return null;
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            return provider.Encrypt(json);
        }

        private static T DecryptJson(string encrypted, IEncryptionProvider provider)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            var json = provider.Decrypt(encrypted);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
    }

    /// <summary>
    /// Extension methods for configuring encrypted properties in EF Core.
    /// </summary>
    public static class EncryptedPropertyExtensions
    {
        /// <summary>
        /// Configures the property to be encrypted when stored in the database.
        /// </summary>
        /// <param name="builder">The property builder.</param>
        /// <param name="encryptionProvider">The encryption provider.</param>
        /// <returns>The property builder for chaining.</returns>
        /// <example>
        /// <code>
        /// modelBuilder.Entity&lt;Customer&gt;()
        ///     .Property(c => c.SocialSecurityNumber)
        ///     .HasEncryptedConversion(encryptionProvider);
        /// </code>
        /// </example>
        public static PropertyBuilder<string> HasEncryptedConversion(
            this PropertyBuilder<string> builder,
            IEncryptionProvider encryptionProvider)
        {
            return builder.HasConversion(new EncryptedStringConverter(encryptionProvider));
        }

        /// <summary>
        /// Configures the property to be encrypted when stored in the database.
        /// Also configures the column length to accommodate encrypted data.
        /// </summary>
        /// <param name="builder">The property builder.</param>
        /// <param name="encryptionProvider">The encryption provider.</param>
        /// <param name="maxPlainTextLength">Maximum length of the plain text.</param>
        /// <returns>The property builder for chaining.</returns>
        /// <remarks>
        /// Encrypted data is typically larger than plain text (IV + padding + Base64 encoding).
        /// This method calculates an appropriate column length.
        /// </remarks>
        public static PropertyBuilder<string> HasEncryptedConversion(
            this PropertyBuilder<string> builder,
            IEncryptionProvider encryptionProvider,
            int maxPlainTextLength)
        {
            // AES encryption adds: IV (16 bytes) + padding (up to 16 bytes) + Base64 overhead (~33%)
            var estimatedEncryptedLength = (int)Math.Ceiling((16 + maxPlainTextLength + 16) * 1.34);
            
            return builder
                .HasConversion(new EncryptedStringConverter(encryptionProvider))
                .HasMaxLength(estimatedEncryptedLength);
        }

        /// <summary>
        /// Configures the property to store encrypted binary data.
        /// </summary>
        /// <param name="builder">The property builder.</param>
        /// <param name="encryptionProvider">The extended encryption provider.</param>
        /// <returns>The property builder for chaining.</returns>
        public static PropertyBuilder<byte[]> HasEncryptedConversion(
            this PropertyBuilder<byte[]> builder,
            IExtendedEncryptionProvider encryptionProvider)
        {
            return builder.HasConversion(new EncryptedBinaryConverter(encryptionProvider));
        }

        /// <summary>
        /// Configures the property to store encrypted JSON data.
        /// </summary>
        /// <typeparam name="T">The type to serialize/deserialize.</typeparam>
        /// <param name="builder">The property builder.</param>
        /// <param name="encryptionProvider">The encryption provider.</param>
        /// <returns>The property builder for chaining.</returns>
        public static PropertyBuilder<T> HasEncryptedJsonConversion<T>(
            this PropertyBuilder<T> builder,
            IEncryptionProvider encryptionProvider) where T : class
        {
            return builder.HasConversion(new EncryptedJsonConverter<T>(encryptionProvider));
        }
    }

    /// <summary>
    /// Extension methods for applying encrypted converters to multiple properties.
    /// </summary>
    public static class EncryptedModelBuilderExtensions
    {
        /// <summary>
        /// Applies encryption to all string properties marked with the [Encrypted] attribute.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="encryptionProvider">The encryption provider.</param>
        /// <returns>The model builder for chaining.</returns>
        /// <example>
        /// <code>
        /// // Mark properties with attribute:
        /// public class Customer
        /// {
        ///     [Encrypted]
        ///     public string CreditCardNumber { get; set; }
        /// }
        /// 
        /// // In OnModelCreating:
        /// modelBuilder.ApplyEncryptedConverters(encryptionProvider);
        /// </code>
        /// </example>
        public static ModelBuilder ApplyEncryptedConverters(
            this ModelBuilder modelBuilder,
            IEncryptionProvider encryptionProvider)
        {
            if (encryptionProvider == null)
                throw new ArgumentNullException(nameof(encryptionProvider));

            var converter = new EncryptedStringConverter(encryptionProvider);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string))
                    {
                        var propertyInfo = property.PropertyInfo;
                        if (propertyInfo != null)
                        {
                            var hasEncryptedAttribute = propertyInfo.GetCustomAttributes(
                                typeof(EncryptedAttribute), true).Length > 0;

                            if (hasEncryptedAttribute)
                            {
                                property.SetValueConverter(converter);
                            }
                        }
                    }
                }
            }

            return modelBuilder;
        }

        /// <summary>
        /// Applies encryption to all properties with the specified column type or annotation.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="encryptionProvider">The encryption provider.</param>
        /// <param name="propertyPredicate">Predicate to determine which properties to encrypt.</param>
        /// <returns>The model builder for chaining.</returns>
        public static ModelBuilder ApplyEncryptedConverters(
            this ModelBuilder modelBuilder,
            IEncryptionProvider encryptionProvider,
            Func<Microsoft.EntityFrameworkCore.Metadata.IMutableProperty, bool> propertyPredicate)
        {
            if (encryptionProvider == null)
                throw new ArgumentNullException(nameof(encryptionProvider));
            if (propertyPredicate == null)
                throw new ArgumentNullException(nameof(propertyPredicate));

            var converter = new EncryptedStringConverter(encryptionProvider);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && propertyPredicate(property))
                    {
                        property.SetValueConverter(converter);
                    }
                }
            }

            return modelBuilder;
        }
    }

    /// <summary>
    /// Attribute to mark properties that should be encrypted in the database.
    /// </summary>
    /// <remarks>
    /// Use this attribute in combination with <see cref="EncryptedModelBuilderExtensions.ApplyEncryptedConverters"/>
    /// to automatically apply encryption to marked properties.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Customer
    /// {
    ///     public int Id { get; set; }
    ///     public string Name { get; set; }
    ///     
    ///     [Encrypted]
    ///     public string CreditCardNumber { get; set; }
    ///     
    ///     [Encrypted]
    ///     public string SocialSecurityNumber { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EncryptedAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to create a blind index for this property.
        /// Default is false.
        /// </summary>
        public bool CreateBlindIndex { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the blind index property.
        /// If not specified, defaults to "{PropertyName}Index".
        /// </summary>
        public string BlindIndexPropertyName { get; set; }
    }
}

