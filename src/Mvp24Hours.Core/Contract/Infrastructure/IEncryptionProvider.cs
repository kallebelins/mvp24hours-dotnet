//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure
{
    /// <summary>
    /// Provides encryption and decryption services for sensitive data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts encryption logic, allowing different implementations
    /// for various scenarios (AES, RSA, Azure Key Vault, AWS KMS, etc.).
    /// </para>
    /// <para>
    /// Common use cases:
    /// <list type="bullet">
    /// <item>Encrypting PII (Personal Identifiable Information) in databases</item>
    /// <item>Field-level encryption for sensitive columns</item>
    /// <item>Secure storage of secrets</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using with EF Core Value Converter:
    /// modelBuilder.Entity&lt;Customer&gt;()
    ///     .Property(c => c.CreditCardNumber)
    ///     .HasEncryptedConversion(encryptionProvider);
    ///     
    /// // Manual encryption:
    /// var encrypted = encryptionProvider.Encrypt("sensitive data");
    /// var decrypted = encryptionProvider.Decrypt(encrypted);
    /// </code>
    /// </example>
    public interface IEncryptionProvider
    {
        /// <summary>
        /// Encrypts a plain text string.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The encrypted text, typically Base64 encoded.</returns>
        /// <remarks>
        /// Returns null or empty string if the input is null or empty.
        /// </remarks>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts an encrypted string.
        /// </summary>
        /// <param name="cipherText">The encrypted text to decrypt.</param>
        /// <returns>The decrypted plain text.</returns>
        /// <remarks>
        /// Returns null or empty string if the input is null or empty.
        /// </remarks>
        string Decrypt(string cipherText);
    }

    /// <summary>
    /// Extended encryption provider with additional capabilities.
    /// </summary>
    public interface IExtendedEncryptionProvider : IEncryptionProvider
    {
        /// <summary>
        /// Encrypts binary data.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// Decrypts binary data.
        /// </summary>
        /// <param name="encryptedData">The encrypted data.</param>
        /// <returns>The decrypted data.</returns>
        byte[] Decrypt(byte[] encryptedData);

        /// <summary>
        /// Computes a deterministic hash for data that needs to be searchable.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>A deterministic hash of the data.</returns>
        /// <remarks>
        /// Use this for fields that need to be queried/indexed while encrypted.
        /// The hash is deterministic, so the same input always produces the same output.
        /// </remarks>
        string ComputeBlindIndex(string data);
    }

    /// <summary>
    /// Options for configuring encryption behavior.
    /// </summary>
    public class EncryptionOptions
    {
        /// <summary>
        /// Gets or sets the encryption key.
        /// Should be at least 256 bits (32 bytes) for AES-256.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the initialization vector (IV).
        /// For AES, should be 128 bits (16 bytes).
        /// If null, a random IV is generated for each encryption.
        /// </summary>
        public string InitializationVector { get; set; }

        /// <summary>
        /// Gets or sets whether to use deterministic encryption.
        /// When true, the same plaintext always produces the same ciphertext.
        /// This allows for equality searches but is less secure.
        /// Default is false (random IV per encryption).
        /// </summary>
        public bool Deterministic { get; set; } = false;

        /// <summary>
        /// Gets or sets the key identifier for key rotation scenarios.
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the salt for blind index computation.
        /// </summary>
        public string BlindIndexSalt { get; set; }
    }
}

