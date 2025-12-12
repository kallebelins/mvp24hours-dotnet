//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mvp24Hours.Core.Infrastructure.Security
{
    /// <summary>
    /// AES-256 encryption provider implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses AES-256 in CBC mode with PKCS7 padding by default.
    /// Supports both randomized (default) and deterministic encryption modes.
    /// </para>
    /// <para>
    /// <strong>Security Notes:</strong>
    /// <list type="bullet">
    /// <item>Use randomized encryption (default) for maximum security</item>
    /// <item>Use deterministic only when equality search is required</item>
    /// <item>Rotate keys periodically</item>
    /// <item>Store keys securely (Azure Key Vault, AWS KMS, etc.)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create with options:
    /// var options = new EncryptionOptions
    /// {
    ///     Key = "your-256-bit-key-base64-encoded",
    ///     Deterministic = false
    /// };
    /// var provider = new AesEncryptionProvider(options);
    /// 
    /// // Encrypt/decrypt:
    /// var encrypted = provider.Encrypt("sensitive data");
    /// var decrypted = provider.Decrypt(encrypted);
    /// 
    /// // Create from key directly:
    /// var provider2 = AesEncryptionProvider.CreateFromKey("base64-key");
    /// </code>
    /// </example>
    public class AesEncryptionProvider : IExtendedEncryptionProvider, IDisposable
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly bool _deterministic;
        private readonly byte[] _blindIndexSalt;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with the specified options.
        /// </summary>
        /// <param name="options">Encryption configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options or key is null.</exception>
        /// <exception cref="ArgumentException">Thrown when key is invalid.</exception>
        public AesEncryptionProvider(EncryptionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.Key))
                throw new ArgumentException("Encryption key is required.", nameof(options));

            _key = Convert.FromBase64String(options.Key);
            if (_key.Length != 32) // 256 bits
                throw new ArgumentException("Encryption key must be 256 bits (32 bytes).", nameof(options));

            _deterministic = options.Deterministic;

            if (!string.IsNullOrEmpty(options.InitializationVector))
            {
                _iv = Convert.FromBase64String(options.InitializationVector);
                if (_iv.Length != 16) // 128 bits
                    throw new ArgumentException("IV must be 128 bits (16 bytes).", nameof(options));
            }

            if (!string.IsNullOrEmpty(options.BlindIndexSalt))
            {
                _blindIndexSalt = Encoding.UTF8.GetBytes(options.BlindIndexSalt);
            }
        }

        /// <summary>
        /// Creates a provider from a Base64-encoded key.
        /// </summary>
        /// <param name="base64Key">The Base64-encoded 256-bit key.</param>
        /// <returns>A new AesEncryptionProvider instance.</returns>
        public static AesEncryptionProvider CreateFromKey(string base64Key)
        {
            return new AesEncryptionProvider(new EncryptionOptions { Key = base64Key });
        }

        /// <summary>
        /// Creates a provider with a newly generated random key.
        /// </summary>
        /// <returns>A new AesEncryptionProvider instance with a random key.</returns>
        /// <remarks>
        /// Use <see cref="GenerateKey"/> to get the generated key for storage.
        /// </remarks>
        public static AesEncryptionProvider CreateWithRandomKey()
        {
            var key = GenerateKey();
            return new AesEncryptionProvider(new EncryptionOptions { Key = key });
        }

        /// <summary>
        /// Generates a new random 256-bit key.
        /// </summary>
        /// <returns>A Base64-encoded 256-bit key.</returns>
        public static string GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        /// <summary>
        /// Generates a new random 128-bit IV.
        /// </summary>
        /// <returns>A Base64-encoded 128-bit IV.</returns>
        public static string GenerateIV()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateIV();
                return Convert.ToBase64String(aes.IV);
            }
        }

        /// <inheritdoc />
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            ThrowIfDisposed();

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = Encrypt(plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <inheritdoc />
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            ThrowIfDisposed();

            var cipherBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = Decrypt(cipherBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <inheritdoc />
        public byte[] Encrypt(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            ThrowIfDisposed();

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] iv;
                if (_deterministic && _iv != null)
                {
                    // Use fixed IV for deterministic encryption
                    iv = _iv;
                }
                else if (_deterministic)
                {
                    // Generate deterministic IV from data hash
                    using (var sha = SHA256.Create())
                    {
                        var hash = sha.ComputeHash(data);
                        iv = new byte[16];
                        Array.Copy(hash, iv, 16);
                    }
                }
                else
                {
                    // Generate random IV (default, most secure)
                    aes.GenerateIV();
                    iv = aes.IV;
                }

                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Prepend IV to ciphertext for randomized encryption
                    if (!_deterministic)
                    {
                        ms.Write(iv, 0, iv.Length);
                    }

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
        public byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return encryptedData;

            ThrowIfDisposed();

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] cipherText;
                if (_deterministic && _iv != null)
                {
                    // Use fixed IV
                    aes.IV = _iv;
                    cipherText = encryptedData;
                }
                else if (_deterministic)
                {
                    // For deterministic without fixed IV, we need the original data
                    // to recreate the IV, which isn't possible during decryption.
                    // This mode requires a fixed IV to be set.
                    throw new InvalidOperationException(
                        "Deterministic encryption without a fixed IV is not supported for decryption. " +
                        "Please provide an InitializationVector in EncryptionOptions.");
                }
                else
                {
                    // Extract IV from the beginning of ciphertext
                    var iv = new byte[16];
                    Array.Copy(encryptedData, 0, iv, 0, 16);
                    aes.IV = iv;
                    cipherText = new byte[encryptedData.Length - 16];
                    Array.Copy(encryptedData, 16, cipherText, 0, cipherText.Length);
                }

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var output = new MemoryStream())
                {
                    cs.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public string ComputeBlindIndex(string data)
        {
            if (string.IsNullOrEmpty(data))
                return data;

            ThrowIfDisposed();

            var salt = _blindIndexSalt ?? _key;

            using (var hmac = new HMACSHA256(salt))
            {
                var dataBytes = Encoding.UTF8.GetBytes(data.ToLowerInvariant()); // Case-insensitive
                var hash = hmac.ComputeHash(dataBytes);
                // Return first 16 bytes as hex (32 chars) for reasonable index size
                return BitConverter.ToString(hash, 0, 16).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Releases all resources used by the provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clear sensitive data from memory
                    if (_key != null) Array.Clear(_key, 0, _key.Length);
                    if (_iv != null) Array.Clear(_iv, 0, _iv.Length);
                    if (_blindIndexSalt != null) Array.Clear(_blindIndexSalt, 0, _blindIndexSalt.Length);
                }
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AesEncryptionProvider));
            }
        }
    }
}

