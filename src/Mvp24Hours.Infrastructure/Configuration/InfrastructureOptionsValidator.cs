//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Configuration
{
    /// <summary>
    /// Validator for InfrastructureOptions to ensure configuration is valid at startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This validator performs basic validation on Infrastructure configuration options
    /// to ensure the application fails fast if configuration is invalid.
    /// </para>
    /// </remarks>
    internal class InfrastructureOptionsValidator : IValidateOptions<InfrastructureOptions>
    {
        /// <summary>
        /// Validates the Infrastructure options.
        /// </summary>
        /// <param name="name">The options name (not used).</param>
        /// <param name="options">The options to validate.</param>
        /// <returns>Validation result.</returns>
        public ValidateOptionsResult Validate(string? name, InfrastructureOptions options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("InfrastructureOptions cannot be null.");
            }

            var errors = new List<string>();

            // Validate HTTP options if configured
            if (options.Http != null)
            {
                ValidateHttpOptions(options.Http, errors);
            }

            // Validate Email options if configured
            if (options.Email != null)
            {
                ValidateEmailOptions(options.Email, errors);
            }

            // Validate SMS options if configured
            if (options.Sms != null)
            {
                ValidateSmsOptions(options.Sms, errors);
            }

            // Validate File Storage options if configured
            if (options.FileStorage != null)
            {
                ValidateFileStorageOptions(options.FileStorage, errors);
            }

            // Validate Security options if configured
            if (options.Security != null)
            {
                ValidateSecurityOptions(options.Security, errors);
            }

            if (errors.Any())
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }

        /// <summary>
        /// Validates HTTP client options.
        /// </summary>
        private static void ValidateHttpOptions(Http.Options.HttpClientOptions? options, List<string> errors)
        {
            if (options == null) return;

            if (options.Timeout <= TimeSpan.Zero)
            {
                errors.Add("HTTP client timeout must be greater than zero.");
            }

            if (options.MaxResponseContentBufferSize <= 0)
            {
                errors.Add("HTTP client MaxResponseContentBufferSize must be greater than zero.");
            }
        }

        /// <summary>
        /// Validates email options.
        /// </summary>
        private static void ValidateEmailOptions(Email.Options.EmailOptions? options, List<string> errors)
        {
            if (options == null) return;

            if (!string.IsNullOrWhiteSpace(options.DefaultFrom))
            {
                if (!IsValidEmail(options.DefaultFrom))
                {
                    errors.Add("Email DefaultFrom must be a valid email address.");
                }
            }

            if (!string.IsNullOrWhiteSpace(options.DefaultReplyTo))
            {
                if (!IsValidEmail(options.DefaultReplyTo))
                {
                    errors.Add("Email DefaultReplyTo must be a valid email address.");
                }
            }

            if (options.MaxAttachmentSize <= 0)
            {
                errors.Add("Email MaxAttachmentSize must be greater than zero.");
            }
        }

        /// <summary>
        /// Validates SMS options.
        /// </summary>
        private static void ValidateSmsOptions(Sms.Options.SmsOptions? options, List<string> errors)
        {
            // Basic validation - SMS options are provider-specific
            // Additional validation should be done by provider-specific validators
        }

        /// <summary>
        /// Validates file storage options.
        /// </summary>
        private static void ValidateFileStorageOptions(FileStorage.Options.FileStorageOptions? options, List<string> errors)
        {
            if (options == null) return;

            if (options.MaxFileSize <= 0)
            {
                errors.Add("FileStorage MaxFileSize must be greater than zero.");
            }

            if (!string.IsNullOrWhiteSpace(options.BasePath))
            {
                // Validate base path format (basic check)
                if (options.BasePath.Contains(".."))
                {
                    errors.Add("FileStorage BasePath cannot contain '..' (path traversal).");
                }
            }
        }

        /// <summary>
        /// Validates security options.
        /// </summary>
        private static void ValidateSecurityOptions(SecurityOptions? options, List<string> errors)
        {
            if (options == null) return;

            // Validate Azure Key Vault options if configured
            if (options.AzureKeyVault != null)
            {
                if (options.AzureKeyVault.VaultUri == null)
                {
                    errors.Add("Azure Key Vault VaultUri is required.");
                }
                else if (!options.AzureKeyVault.VaultUri.IsAbsoluteUri)
                {
                    errors.Add("Azure Key Vault VaultUri must be an absolute URI.");
                }

                if (!options.AzureKeyVault.UseManagedIdentity)
                {
                    if (string.IsNullOrWhiteSpace(options.AzureKeyVault.ClientId) ||
                        string.IsNullOrWhiteSpace(options.AzureKeyVault.ClientSecret) ||
                        string.IsNullOrWhiteSpace(options.AzureKeyVault.TenantId))
                    {
                        errors.Add("Azure Key Vault requires ClientId, ClientSecret, and TenantId when not using Managed Identity.");
                    }
                }
            }

            // Validate AWS Secrets Manager options if configured
            if (options.AwsSecretsManager != null)
            {
                if (string.IsNullOrWhiteSpace(options.AwsSecretsManager.Region))
                {
                    errors.Add("AWS Secrets Manager Region is required.");
                }
            }
        }

        /// <summary>
        /// Validates email address format (basic check).
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

