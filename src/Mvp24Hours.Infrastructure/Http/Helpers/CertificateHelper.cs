//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Options;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Mvp24Hours.Infrastructure.Http.Helpers
{
    /// <summary>
    /// Helper class for loading and managing SSL/TLS certificates.
    /// </summary>
    public static class CertificateHelper
    {
        /// <summary>
        /// Loads a certificate based on the provided options.
        /// </summary>
        /// <param name="options">The certificate options.</param>
        /// <returns>The loaded X509Certificate2, or null if options are not configured.</returns>
        public static X509Certificate2? LoadCertificate(CertificateOptions? options)
        {
            if (options == null)
            {
                return null;
            }

            // Load from file
            if (!string.IsNullOrWhiteSpace(options.FilePath))
            {
                return LoadFromFile(options.FilePath, options.Password, options.KeyStorageFlags);
            }

            // Load from base64 string
            if (!string.IsNullOrWhiteSpace(options.Base64Certificate))
            {
                return LoadFromBase64(options.Base64Certificate, options.Password, options.KeyStorageFlags);
            }

            // Load from certificate store by thumbprint
            if (!string.IsNullOrWhiteSpace(options.Thumbprint))
            {
                return LoadFromStoreByThumbprint(
                    options.Thumbprint,
                    options.StoreLocation,
                    options.StoreName);
            }

            // Load from certificate store by subject name
            if (!string.IsNullOrWhiteSpace(options.SubjectName))
            {
                return LoadFromStoreBySubjectName(
                    options.SubjectName,
                    options.StoreLocation,
                    options.StoreName);
            }

            return null;
        }

        /// <summary>
        /// Loads a certificate from a file.
        /// </summary>
        /// <param name="filePath">The path to the certificate file.</param>
        /// <param name="password">The certificate password (optional).</param>
        /// <param name="keyStorageFlags">The key storage flags.</param>
        /// <returns>The loaded certificate.</returns>
        public static X509Certificate2 LoadFromFile(
            string filePath,
            string? password = null,
            X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "Certificate file path is required.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Certificate file not found: {filePath}", filePath);
            }

            return string.IsNullOrWhiteSpace(password)
                ? new X509Certificate2(filePath)
                : new X509Certificate2(filePath, password, keyStorageFlags);
        }

        /// <summary>
        /// Loads a certificate from a base64-encoded string.
        /// </summary>
        /// <param name="base64Certificate">The base64-encoded certificate.</param>
        /// <param name="password">The certificate password (optional).</param>
        /// <param name="keyStorageFlags">The key storage flags.</param>
        /// <returns>The loaded certificate.</returns>
        public static X509Certificate2 LoadFromBase64(
            string base64Certificate,
            string? password = null,
            X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
        {
            if (string.IsNullOrWhiteSpace(base64Certificate))
            {
                throw new ArgumentNullException(nameof(base64Certificate), "Base64 certificate is required.");
            }

            var certificateBytes = Convert.FromBase64String(base64Certificate);

            return string.IsNullOrWhiteSpace(password)
                ? new X509Certificate2(certificateBytes)
                : new X509Certificate2(certificateBytes, password, keyStorageFlags);
        }

        /// <summary>
        /// Loads a certificate from a byte array.
        /// </summary>
        /// <param name="certificateBytes">The certificate bytes.</param>
        /// <param name="password">The certificate password (optional).</param>
        /// <param name="keyStorageFlags">The key storage flags.</param>
        /// <returns>The loaded certificate.</returns>
        public static X509Certificate2 LoadFromBytes(
            byte[] certificateBytes,
            string? password = null,
            X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
        {
            if (certificateBytes == null || certificateBytes.Length == 0)
            {
                throw new ArgumentNullException(nameof(certificateBytes), "Certificate bytes are required.");
            }

            return string.IsNullOrWhiteSpace(password)
                ? new X509Certificate2(certificateBytes)
                : new X509Certificate2(certificateBytes, password, keyStorageFlags);
        }

        /// <summary>
        /// Loads a certificate from the certificate store by thumbprint.
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint.</param>
        /// <param name="storeLocation">The store location.</param>
        /// <param name="storeName">The store name.</param>
        /// <returns>The loaded certificate.</returns>
        public static X509Certificate2? LoadFromStoreByThumbprint(
            string thumbprint,
            StoreLocation storeLocation = StoreLocation.CurrentUser,
            StoreName storeName = StoreName.My)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                throw new ArgumentNullException(nameof(thumbprint), "Certificate thumbprint is required.");
            }

            // Normalize thumbprint (remove spaces, convert to uppercase)
            thumbprint = thumbprint.Replace(" ", string.Empty).ToUpperInvariant();

            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: false);

            if (certificates.Count == 0)
            {
                return null;
            }

            return certificates[0];
        }

        /// <summary>
        /// Loads a certificate from the certificate store by subject name.
        /// </summary>
        /// <param name="subjectName">The certificate subject name.</param>
        /// <param name="storeLocation">The store location.</param>
        /// <param name="storeName">The store name.</param>
        /// <returns>The loaded certificate.</returns>
        public static X509Certificate2? LoadFromStoreBySubjectName(
            string subjectName,
            StoreLocation storeLocation = StoreLocation.CurrentUser,
            StoreName storeName = StoreName.My)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                throw new ArgumentNullException(nameof(subjectName), "Certificate subject name is required.");
            }

            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                subjectName,
                validOnly: false);

            if (certificates.Count == 0)
            {
                return null;
            }

            // Return the certificate with the latest NotAfter date
            X509Certificate2? latestCert = null;
            foreach (var cert in certificates)
            {
                if (latestCert == null || cert.NotAfter > latestCert.NotAfter)
                {
                    latestCert = cert;
                }
            }

            return latestCert;
        }

        /// <summary>
        /// Validates that a certificate is valid for the current date.
        /// </summary>
        /// <param name="certificate">The certificate to validate.</param>
        /// <returns>True if the certificate is valid; otherwise, false.</returns>
        public static bool IsValid(X509Certificate2? certificate)
        {
            if (certificate == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            return now >= certificate.NotBefore && now <= certificate.NotAfter;
        }

        /// <summary>
        /// Validates that a certificate is valid for the current date and throws if not.
        /// </summary>
        /// <param name="certificate">The certificate to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when the certificate is invalid.</exception>
        public static void EnsureValid(X509Certificate2? certificate)
        {
            if (certificate == null)
            {
                throw new InvalidOperationException("Certificate is null.");
            }

            var now = DateTime.UtcNow;

            if (now < certificate.NotBefore)
            {
                throw new InvalidOperationException(
                    $"Certificate is not yet valid. Valid from: {certificate.NotBefore:yyyy-MM-dd HH:mm:ss}");
            }

            if (now > certificate.NotAfter)
            {
                throw new InvalidOperationException(
                    $"Certificate has expired. Expired on: {certificate.NotAfter:yyyy-MM-dd HH:mm:ss}");
            }
        }

        /// <summary>
        /// Gets the days until a certificate expires.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>The number of days until expiration.</returns>
        public static int GetDaysUntilExpiration(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return (int)(certificate.NotAfter - DateTime.UtcNow).TotalDays;
        }

        /// <summary>
        /// Gets certificate information as a formatted string.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>A string with certificate details.</returns>
        public static string GetCertificateInfo(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                return "Certificate is null";
            }

            return $"Subject: {certificate.Subject}\n" +
                   $"Issuer: {certificate.Issuer}\n" +
                   $"Thumbprint: {certificate.Thumbprint}\n" +
                   $"Serial: {certificate.SerialNumber}\n" +
                   $"Valid From: {certificate.NotBefore:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Valid To: {certificate.NotAfter:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Days Until Expiration: {GetDaysUntilExpiration(certificate)}";
        }
    }
}

