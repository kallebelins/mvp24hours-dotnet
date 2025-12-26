//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Mvp24Hours.Infrastructure.FileStorage.Extensions
{
    /// <summary>
    /// Extension methods for registering file storage services.
    /// </summary>
    public static class FileStorageServiceExtensions
    {
        /// <summary>
        /// Adds file storage services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for file storage options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the file storage infrastructure with dependency injection.
        /// By default, it uses <see cref="LocalFileStorageProvider"/> if no provider is specified.
        /// </para>
        /// <para>
        /// To use a different provider, call one of the specific extension methods:
        /// - <see cref="AddLocalFileStorage"/>
        /// - <see cref="AddInMemoryFileStorage"/>
        /// - <see cref="AddAzureBlobStorage"/> (requires Azure.Storage.Blobs package)
        /// - <see cref="AddAwsS3Storage"/> (requires AWSSDK.S3 package)
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddFileStorage(options =>
        /// {
        ///     options.BasePath = "uploads";
        ///     options.MaxFileSize = 10 * 1024 * 1024; // 10MB
        ///     options.AllowedExtensions = new[] { "pdf", "jpg", "png" };
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddFileStorage(
            this IServiceCollection services,
            Action<FileStorageOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<FileStorageOptions>(_ => { });
            }

            services.AddSingleton<IFileStorage>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new LocalFileStorageProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds local filesystem file storage to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for file storage options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers <see cref="LocalFileStorageProvider"/> as the file storage implementation.
        /// Files are stored on the local filesystem.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddLocalFileStorage(options =>
        /// {
        ///     options.BasePath = "C:\\Uploads";
        ///     options.MaxFileSize = 10 * 1024 * 1024; // 10MB
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddLocalFileStorage(
            this IServiceCollection services,
            Action<FileStorageOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<FileStorageOptions>(_ => { });
            }

            services.AddSingleton<IFileStorage>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new LocalFileStorageProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds in-memory file storage to the service collection (for testing).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for file storage options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers <see cref="InMemoryFileStorageProvider"/> as the file storage implementation.
        /// Files are stored in memory and are lost when the application restarts.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddInMemoryFileStorage(options =>
        /// {
        ///     options.MaxFileSize = 5 * 1024 * 1024; // 5MB
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddInMemoryFileStorage(
            this IServiceCollection services,
            Action<FileStorageOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<FileStorageOptions>(_ => { });
            }

            services.AddSingleton<IFileStorage>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new InMemoryFileStorageProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds file storage with a custom provider factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory function to create the file storage provider.</param>
        /// <param name="configure">Optional configuration action for file storage options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method allows registering a custom file storage provider implementation.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddFileStorageWithProvider((serviceProvider, options) =>
        /// {
        ///     return new CustomFileStorageProvider(options);
        /// }, options =>
        /// {
        ///     options.BasePath = "custom";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddFileStorageWithProvider(
            this IServiceCollection services,
            Func<IServiceProvider, FileStorageOptions, IFileStorage> factory,
            Action<FileStorageOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<FileStorageOptions>(_ => { });
            }

            services.AddSingleton<IFileStorage>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return factory(serviceProvider, options);
            });

            return services;
        }

        /// <summary>
        /// Adds Azure Blob Storage file storage to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        /// <param name="containerName">The name of the blob container.</param>
        /// <param name="configure">Optional configuration action for file storage options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="AzureBlobStorageProvider"/> as the file storage implementation.
        /// Files are stored in Azure Blob Storage.
        /// </para>
        /// <para>
        /// <strong>Required Package:</strong>
        /// Azure.Storage.Blobs
        /// </para>
        /// <para>
        /// The container will be created automatically if <see cref="FileStorageOptions.CreateDirectoriesIfNotExists"/>
        /// is <c>true</c> (default).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddAzureBlobStorage(
        ///     connectionString: "DefaultEndpointsProtocol=https;AccountName=...",
        ///     containerName: "my-container",
        ///     options =>
        ///     {
        ///         options.MaxFileSize = 100 * 1024 * 1024; // 100MB
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddAzureBlobStorage(
            this IServiceCollection services,
            string connectionString,
            string containerName,
            Action<FileStorageOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<FileStorageOptions>(_ => { });
            }

            services.AddSingleton<IFileStorage>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new AzureBlobStorageProvider(options, connectionString, containerName);
            });

            return services;
        }

        /// <summary>
        /// Adds Amazon S3 file storage to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="bucketName">The name of the S3 bucket.</param>
        /// <param name="accessKeyId">Optional AWS access key ID. If not provided, uses default credential chain.</param>
        /// <param name="secretAccessKey">Optional AWS secret access key. Required if accessKeyId is provided.</param>
        /// <param name="region">Optional AWS region (e.g., "us-east-1"). Uses default if not provided.</param>
        /// <param name="configure">Optional configuration action for file storage options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="AwsS3StorageProvider"/> as the file storage implementation.
        /// Files are stored in Amazon S3.
        /// </para>
        /// <para>
        /// <strong>Required Package:</strong>
        /// AWSSDK.S3
        /// </para>
        /// <para>
        /// The bucket will be created automatically if <see cref="FileStorageOptions.CreateDirectoriesIfNotExists"/>
        /// is <c>true</c> (default) and the credentials have permission to create buckets.
        /// </para>
        /// <para>
        /// If accessKeyId and secretAccessKey are not provided, the AWS SDK will use the default credential chain:
        /// - Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
        /// - IAM role (when running on EC2/ECS/Lambda)
        /// - AWS credentials file (~/.aws/credentials)
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Using explicit credentials
        /// services.AddAwsS3Storage(
        ///     bucketName: "my-bucket",
        ///     accessKeyId: "AKIAIOSFODNN7EXAMPLE",
        ///     secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
        ///     region: "us-east-1",
        ///     options =>
        ///     {
        ///         options.MaxFileSize = 100 * 1024 * 1024; // 100MB
        ///     });
        ///
        /// // Using default credential chain (IAM role, environment variables, etc.)
        /// services.AddAwsS3Storage(
        ///     bucketName: "my-bucket",
        ///     region: "us-east-1");
        /// </code>
        /// </example>
        public static IServiceCollection AddAwsS3Storage(
            this IServiceCollection services,
            string bucketName,
            string? accessKeyId = null,
            string? secretAccessKey = null,
            string? region = null,
            Action<FileStorageOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentException("Bucket name cannot be null or empty.", nameof(bucketName));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<FileStorageOptions>(_ => { });
            }

            services.AddSingleton<IFileStorage>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new AwsS3StorageProvider(options, bucketName, accessKeyId, secretAccessKey, region);
            });

            return services;
        }
    }
}

