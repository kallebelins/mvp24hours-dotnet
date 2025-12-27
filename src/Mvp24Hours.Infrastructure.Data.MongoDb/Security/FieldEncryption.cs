//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Security
{
    /// <summary>
    /// Provides field-level encryption for MongoDB documents using AES-256 encryption.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a client-side field-level encryption implementation that encrypts
    /// sensitive data before it's sent to MongoDB and decrypts it when retrieved.
    /// </para>
    /// <para>
    /// <strong>Important Security Notes:</strong>
    /// <list type="bullet">
    ///   <item>Store encryption keys securely (use Azure Key Vault, AWS KMS, etc.)</item>
    ///   <item>Use different keys for different tenants in multi-tenant scenarios</item>
    ///   <item>Rotate keys periodically</item>
    ///   <item>This is NOT the same as MongoDB's native CSFLE (Client-Side Field Level Encryption)</item>
    /// </list>
    /// </para>
    /// <para>
    /// For production environments with strict compliance requirements, consider using
    /// MongoDB's native CSFLE with automatic encryption. This implementation provides
    /// a simpler alternative for basic encryption needs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure encryption:
    /// var encryptor = new FieldEncryptor(Convert.FromBase64String("your-32-byte-key-base64"));
    /// 
    /// // Encrypt data:
    /// var encrypted = encryptor.Encrypt("sensitive-data");
    /// 
    /// // Decrypt data:
    /// var decrypted = encryptor.Decrypt(encrypted);
    /// 
    /// // Use with serializer:
    /// BsonClassMap.RegisterClassMap&lt;MyEntity&gt;(cm =>
    /// {
    ///     cm.AutoMap();
    ///     cm.MapMember(c => c.SensitiveField)
    ///       .SetSerializer(new EncryptedStringSerializer(encryptor));
    /// });
    /// </code>
    /// </example>
    public interface IFieldEncryptor
    {
        /// <summary>
        /// Encrypts a plaintext string.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <returns>The encrypted text as a Base64 string.</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts an encrypted string.
        /// </summary>
        /// <param name="cipherText">The encrypted text (Base64).</param>
        /// <returns>The decrypted plaintext.</returns>
        string Decrypt(string cipherText);

        /// <summary>
        /// Encrypts binary data.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        byte[] EncryptBytes(byte[] data);

        /// <summary>
        /// Decrypts binary data.
        /// </summary>
        /// <param name="encryptedData">The encrypted data.</param>
        /// <returns>The decrypted data.</returns>
        byte[] DecryptBytes(byte[] encryptedData);
    }

    /// <summary>
    /// AES-256 field encryptor implementation.
    /// </summary>
    public class AesFieldEncryptor : IFieldEncryptor, IDisposable
    {
        private readonly byte[] _key;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesFieldEncryptor"/> class.
        /// </summary>
        /// <param name="key">The 256-bit (32 bytes) encryption key.</param>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        /// <exception cref="ArgumentException">Thrown when key is not 32 bytes.</exception>
        public AesFieldEncryptor(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length != 32)
            {
                throw new ArgumentException("Key must be 256 bits (32 bytes).", nameof(key));
            }

            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
        }

        /// <summary>
        /// Creates a new encryptor from a Base64-encoded key.
        /// </summary>
        /// <param name="base64Key">The Base64-encoded 256-bit key.</param>
        /// <returns>A new <see cref="AesFieldEncryptor"/> instance.</returns>
        public static AesFieldEncryptor FromBase64Key(string base64Key)
        {
            if (string.IsNullOrEmpty(base64Key))
            {
                throw new ArgumentNullException(nameof(base64Key));
            }

            var key = Convert.FromBase64String(base64Key);
            return new AesFieldEncryptor(key);
        }

        /// <summary>
        /// Generates a new random 256-bit encryption key.
        /// </summary>
        /// <returns>A new random key as a byte array.</returns>
        public static byte[] GenerateKey()
        {
            var key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// Generates a new random 256-bit encryption key as Base64.
        /// </summary>
        /// <returns>A new random key as a Base64 string.</returns>
        public static string GenerateKeyAsBase64()
        {
            return Convert.ToBase64String(GenerateKey());
        }

        /// <inheritdoc />
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = EncryptBytes(plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <inheritdoc />
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            var encryptedBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = DecryptBytes(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <inheritdoc />
        public byte[] EncryptBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return data;
            }

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Write IV first (16 bytes for AES)
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public byte[] DecryptBytes(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length <= 16)
            {
                return encryptedData;
            }

            using (var aes = Aes.Create())
            {
                aes.Key = _key;

                // Extract IV from the beginning of the data
                var iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, 16);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, 16, encryptedData.Length - 16);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the encryptor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clear the key from memory
                    if (_key != null)
                    {
                        Array.Clear(_key, 0, _key.Length);
                    }
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// MongoDB serializer for encrypted string fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This serializer automatically encrypts values when writing to MongoDB
    /// and decrypts them when reading.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register class map with encrypted field:
    /// BsonClassMap.RegisterClassMap&lt;User&gt;(cm =>
    /// {
    ///     cm.AutoMap();
    ///     cm.MapMember(c => c.SocialSecurityNumber)
    ///       .SetSerializer(new EncryptedStringSerializer(encryptor));
    /// });
    /// </code>
    /// </example>
    public class EncryptedStringSerializer : SerializerBase<string>
    {
        private readonly IFieldEncryptor _encryptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptedStringSerializer"/> class.
        /// </summary>
        /// <param name="encryptor">The field encryptor to use.</param>
        public EncryptedStringSerializer(IFieldEncryptor encryptor)
        {
            _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        }

        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            var encrypted = _encryptor.Encrypt(value);
            context.Writer.WriteString(encrypted);
        }

        /// <inheritdoc />
        public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }

            var encrypted = context.Reader.ReadString();
            return _encryptor.Decrypt(encrypted);
        }
    }

    /// <summary>
    /// Attribute to mark fields for encryption.
    /// </summary>
    /// <remarks>
    /// Use this attribute to mark properties that should be encrypted.
    /// Register the <see cref="EncryptedFieldConvention"/> to automatically
    /// apply encryption to marked properties.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class User
    /// {
    ///     public string Id { get; set; }
    ///     
    ///     [EncryptedField]
    ///     public string SocialSecurityNumber { get; set; }
    ///     
    ///     [EncryptedField]
    ///     public string CreditCardNumber { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EncryptedFieldAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the encryption key name to use for this field.
        /// If null, the default key is used.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the algorithm to use. Default is "AES-256".
        /// </summary>
        public string Algorithm { get; set; } = "AES-256";
    }

    /// <summary>
    /// Provides helper methods for encryption key management.
    /// </summary>
    public static class EncryptionKeyHelper
    {
        /// <summary>
        /// Derives a key from a password using PBKDF2.
        /// </summary>
        /// <param name="password">The password to derive the key from.</param>
        /// <param name="salt">The salt (should be at least 16 bytes, unique per key).</param>
        /// <param name="iterations">Number of iterations (default 100,000 for security).</param>
        /// <returns>A 256-bit key derived from the password.</returns>
        public static byte[] DeriveKeyFromPassword(string password, byte[] salt, int iterations = 100000)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (salt == null || salt.Length < 16)
            {
                throw new ArgumentException("Salt must be at least 16 bytes.", nameof(salt));
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256 bits
            }
        }

        /// <summary>
        /// Generates a random salt.
        /// </summary>
        /// <param name="length">The length of the salt in bytes (default 32).</param>
        /// <returns>A random salt.</returns>
        public static byte[] GenerateSalt(int length = 32)
        {
            var salt = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Creates a per-tenant encryption key by combining a master key with tenant ID.
        /// </summary>
        /// <param name="masterKey">The master encryption key.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A tenant-specific 256-bit key.</returns>
        public static byte[] DerivePerTenantKey(byte[] masterKey, string tenantId)
        {
            if (masterKey == null || masterKey.Length < 16)
            {
                throw new ArgumentException("Master key must be at least 16 bytes.", nameof(masterKey));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            // Use HKDF-like derivation (simplified version using HMAC)
            var tenantBytes = Encoding.UTF8.GetBytes(tenantId);
            
            using (var hmac = new HMACSHA256(masterKey))
            {
                // Derive tenant-specific key
                var info = new byte[tenantBytes.Length + 4];
                Array.Copy(tenantBytes, 0, info, 0, tenantBytes.Length);
                BitConverter.GetBytes(1).CopyTo(info, tenantBytes.Length);
                
                return hmac.ComputeHash(info);
            }
        }
    }
}

