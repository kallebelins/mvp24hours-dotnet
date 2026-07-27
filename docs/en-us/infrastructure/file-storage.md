# File Storage

`IFileStorage` provides one asynchronous API for upload, download, streaming, existence checks, metadata, listing, copy, move, and hard delete. The shipped working providers are local filesystem and in-memory storage. Cloud provider registrations exist, but their operations are currently stubs; see [Provider status](#provider-status).

## Install

The file-storage API and providers are part of `Mvp24Hours.Infrastructure`:

```bash
dotnet add package Mvp24Hours.Infrastructure
```

The project does **not** currently reference `Azure.Storage.Blobs` or `AWSSDK.S3`. The Azure and AWS source files name those packages, but installing either package in a consuming application does not complete the stub implementation.

## Register one provider

`AddFileStorage` and `AddLocalFileStorage` both register `LocalFileStorageProvider` as singleton `IFileStorage`. With an empty `BasePath`, local storage defaults to an `uploads` directory under the current working directory.

```csharp
using Mvp24Hours.Infrastructure.FileStorage.Extensions;

builder.Services.AddLocalFileStorage(options =>
{
    options.BasePath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
    options.OverwriteExistingFiles = false;
    options.DefaultContentType = "application/octet-stream";
});
```

For development or tests:

```csharp
builder.Services.AddInMemoryFileStorage(options =>
{
    options.BasePath = "test-files";
    options.OverwriteExistingFiles = false;
});
```

A custom registration can use the verified factory overload:

```csharp
using Mvp24Hours.Infrastructure.FileStorage.Providers;

builder.Services.AddFileStorageWithProvider(
    (_, options) => new InMemoryFileStorageProvider(options),
    options => options.BasePath = "files");
```

All built-in registration methods configure `IOptions<FileStorageOptions>` and register one singleton `IFileStorage`. Calling more than one of them does not create named providers; use keyed services when multiple implementations are required.

## Use `IFileStorage`

```csharp
using Mvp24Hours.Infrastructure.FileStorage.Contract;

public sealed class DocumentStore(IFileStorage storage)
{
    public async Task<bool> SaveAsync(
        string path,
        Stream content,
        CancellationToken cancellationToken)
    {
        var result = await storage.UploadFromStreamAsync(
            path,
            content,
            "application/pdf",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "document-service"
            },
            cancellationToken: cancellationToken);

        return result.Success;
    }
}
```

Use `UploadAsync`/`DownloadAsync` for byte arrays and `UploadFromStreamAsync`/`DownloadToStreamAsync` for large files. The caller owns every input or destination stream and must dispose it. `UploadFromChunksAsync` consumes an `IAsyncEnumerable<byte[]>`; `DownloadAsChunksAsync` yields chunks. These `IFileStorage` methods are streaming shapes, not resumable multipart sessions.

Upload and download methods return `FileUploadResult` and `FileDownloadResult`. Check `Success`, `ErrorMessage`, and `Exception`; a missing download can also be identified through `IsNotFound`.

## `FileStorageOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `BasePath` | `string` | `""` | Root directory or provider prefix. Local storage substitutes `<current directory>/uploads` when empty. |
| `MaxFileSize` | `long?` | `104857600` (100 MiB) | Intended maximum file size; `null` disables the rule. See the validation note below. |
| `MinFileSize` | `long?` | `null` | Intended minimum file size. |
| `AllowedExtensions` | `IList<string>?` | `null` | Case-insensitive allow list, without leading dots. |
| `BlockedExtensions` | `IList<string>?` | `null` | Case-insensitive deny list, without leading dots; conflicts are reported by `Validate()`. |
| `AllowedContentTypes` | `IList<string>?` | `null` | Case-insensitive MIME allow list. |
| `BlockedContentTypes` | `IList<string>?` | `null` | Case-insensitive MIME deny list; conflicts are reported by `Validate()`. |
| `CreateDirectoriesIfNotExists` | `bool` | `true` | Creates the local base directory and upload directories when needed. |
| `OverwriteExistingFiles` | `bool` | `true` | Allows local and in-memory upload/copy/move operations to replace a destination. |
| `DefaultContentType` | `string` | `"application/octet-stream"` | Used by local and in-memory uploads when the supplied content type is blank. |
| `ChunkSize` | `int` | `65536` (64 KiB) | Configured streaming chunk size. Callers can separately pass `chunkSize` to `DownloadAsChunksAsync`. |
| `ValidateFileContent` | `bool` | `false` | Signals an intended content-aware validation policy; built-in providers do not inspect this flag themselves. |
| `ProviderOptions` | `IDictionary<string, object>` | Empty dictionary | Extension point for custom provider settings; current working providers do not consume it. |

`FileStorageOptions.Validate()` reports configuration contradictions: negative sizes, minimum greater than maximum, non-positive chunk size, and values present in both allow and block lists.

> Important: the built-in DI registrations do not call `FileStorageOptions.Validate()`, and the built-in providers do not enforce the size, extension, MIME, or `ValidateFileContent` properties directly. Local and in-memory providers enforce upload validation only when an `IFileValidator` is passed to their constructors. The standard registration extensions construct those providers without resolving a validator. For production upload policy, validate options at startup and register a custom provider factory that supplies your `IFileValidator`.

```csharp
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.FileStorage.Providers;

builder.Services.AddSingleton<IFileValidator, UploadPolicyValidator>();
builder.Services.AddFileStorageWithProvider(
    (sp, options) =>
    {
        IList<string> errors = options.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        return new LocalFileStorageProvider(
            options,
            sp.GetRequiredService<IFileValidator>());
    },
    options =>
    {
        options.MaxFileSize = 10 * 1024 * 1024;
        options.AllowedExtensions = ["pdf"];
        options.AllowedContentTypes = ["application/pdf"];
    });
```

### Presets

- `Default` returns the defaults in the table.
- `ForImages` allows JPG/JPEG, PNG, GIF, WebP, and BMP; limits size to 10 MiB; defaults content type to `image/jpeg`.
- `ForDocuments` allows common PDF, Office, text, and RTF extensions/MIME types; limits size to 50 MiB; defaults content type to `application/pdf`.
- `ForSecureUploads` limits size to 5 MiB, blocks a list of executable/script extensions and MIME types, and sets `ValidateFileContent = true`.

Each preset returns a new mutable instance. Presets describe policy but require a validator wiring as explained above to enforce upload limits and allow/block lists.

## Provider status

| Provider | Registration | Persistence and path behavior | Current capability |
|---|---|---|---|
| Local | `AddFileStorage` or `AddLocalFileStorage` | Filesystem-backed; relative `BasePath` is resolved from the current working directory; rejects normalized paths outside its base directory. | Implements `IFileStorage`, including streams, chunk enumeration, metadata, listing, copy, move, and delete. |
| In-memory | `AddInMemoryFileStorage` | Per-provider dictionary; data is lost when the process/provider is discarded; `BasePath` prefixes normalized logical paths. | Implements `IFileStorage`; intended for development and tests, not production persistence. |
| Azure Blob | `AddAzureBlobStorage(connectionString, containerName, configure?)` | Constructor and DI registration are available. | Every `IFileStorage` operation throws `NotImplementedException`; `Azure.Storage.Blobs` is not packaged. |
| AWS S3 | `AddAwsS3Storage(bucketName, accessKeyId?, secretAccessKey?, region?, configure?)` | Constructor and DI registration are available. | Every `IFileStorage` operation throws `NotImplementedException`; `AWSSDK.S3` is not packaged. |

Do not use the cloud registrations as production adapters in the current source state. Their unit tests intentionally assert `NotImplementedException`.

## Multiple providers with keyed DI

The Core package defines `ServiceKeys.FileStorage.Local`, `InMemory`, `Azure`, `AwsS3`, and `Default`. File-storage registration extensions do not register these keys automatically. Use the native keyed factory overload and construct providers with their required options:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.KeyedServices;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.FileStorage.Providers;

builder.Services.AddKeyedSingleton<IFileStorage>(
    ServiceKeys.FileStorage.Local,
    (_, _) => new LocalFileStorageProvider(new FileStorageOptions
    {
        BasePath = "uploads"
    }));

builder.Services.AddKeyedSingleton<IFileStorage>(
    ServiceKeys.FileStorage.InMemory,
    (_, _) => new InMemoryFileStorageProvider(FileStorageOptions.Default));

builder.Services.SetDefaultKeyedService<IFileStorage>(
    ServiceKeys.FileStorage.Local);
```

Resolve with `[FromKeyedServices(ServiceKeys.FileStorage.InMemory)]` or `GetRequiredKeyedService<IFileStorage>(key)`. See [Keyed Services](../modernization/keyed-services.md).

## Resumable uploads, multipart URLs, and soft delete

The module defines optional capability interfaces:

- `IChunkedUploadStorage` models a resumable session: initiate, upload numbered chunks, inspect status/list uploaded chunks, complete, or abort.
- `IPresignedUrlStorage` generates temporary upload/download URLs and multipart part URLs through `GenerateMultipartUploadUrlsAsync`.
- `ISoftDeleteStorage` models soft delete, restore, permanent deletion, retention cleanup, listing, and status checks.
- `IFileVersioningStorage`, `IImageProcessingStorage`, and `ICdnStorage` define other optional capabilities.

Detect and cast capabilities with `FileStorageAdvancedExtensions`:

```csharp
using Mvp24Hours.Infrastructure.FileStorage.Advanced;

if (storage.SupportsChunkedUpload() &&
    storage.AsChunkedUploadStorage() is { } chunked)
{
    string? uploadId = await chunked.InitiateChunkedUploadAsync(
        "archives/data.zip",
        "application/zip",
        totalSize,
        chunkSize,
        cancellationToken: cancellationToken);
}

if (storage.SupportsSoftDelete() &&
    storage.AsSoftDeleteStorage() is { } softDelete)
{
    await softDelete.SoftDeleteAsync(
        "documents/old.pdf",
        reason: "Replaced",
        cancellationToken: cancellationToken);
}
```

None of the four built-in provider classes implements these optional interfaces. Result classes such as `ChunkedUploadStatus`, `MultipartUploadInfo`, `FileVersion`, and `SoftDeletedFile` are data models, not provider implementations.

## Health check

```csharp
using Mvp24Hours.Infrastructure.HealthChecks;

builder.Services.AddHealthChecks()
    .AddFileStorageHealthCheck(
        name: "file-storage",
        configureOptions: options =>
        {
            options.TestFilePath = "health-check/storage.txt";
            options.TimeoutSeconds = 10;
        },
        timeout: TimeSpan.FromSeconds(12));
```

The check writes a text file, verifies existence, downloads it, verifies size and optionally bytes, then deletes it. It is an active read/write probe: use a dedicated path/prefix and grant delete permission. Upload, existence, download, or content failures are unhealthy. Total duration controls degraded/unhealthy thresholds.

### `FileStorageHealthCheckOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `TestFilePath` | `string?` | `null` | Probe path; null generates `health-check/<guid>.txt`. |
| `TestContent` | `string` | `"Health check test content"` | UTF-8 content written and read by the probe. |
| `TimeoutSeconds` | `int` | `10` | Internal timeout for all probe operations. |
| `SkipContentVerification` | `bool` | `false` | Skips byte-by-byte comparison, but size is still checked. |
| `DegradedThresholdMs` | `int` | `1000` | Total response time at or above which the check is degraded. |
| `FailureThresholdMs` | `int` | `5000` | Total response time at or above which the check is unhealthy. |
| `Tags` | `IEnumerable<string>` | `["file-storage", "storage", "ready"]` | Default tags when registration does not supply tags. |

The health check cannot succeed against the current Azure or AWS stubs.

## Observability

The providers do not publish a dedicated file-storage meter or activity source. Observe operation results, exceptions, duration, file size, and provider identity in the calling service without logging file content, credentials, presigned URLs, or sensitive paths. The health check exposes upload, exists, download, delete, and total response timings in its result data.

## Testing

For provider behavior close to production code, use `AddInMemoryFileStorage`. For configurable failures and test assertions, use `AddFakeFileStorage`:

```csharp
using Mvp24Hours.Infrastructure.Testing.Extensions;
using Mvp24Hours.Infrastructure.Testing.Fakes;

services.AddFakeFileStorage(fake =>
{
    fake.ShouldUploadFail = false;
    fake.SimulatedDelay = TimeSpan.FromMilliseconds(5);
});

IFakeFileStorage fake =
    serviceProvider.GetRequiredService<IFakeFileStorage>();

fake.SeedFile("fixtures/input.txt", "hello"u8.ToArray(), "text/plain");
```

`FakeFileStorage` supports seeding, inspection, clearing, simulated delay, upload/download failure switches, and custom result factories. It also normalizes separators and supports the complete base `IFileStorage` surface. Use temporary directories for local-provider tests and clean them in test teardown. Use provider integration tests—not the cloud stub tests—to verify credentials, permissions, multipart behavior, and eventual consistency in a future cloud implementation.

## Related

- [Infrastructure Modules](home.md)
- [Keyed Services](../modernization/keyed-services.md)
- [Observability](../observability/home.md)
- [Options validation](../core/options-validation.md)
- [Azure Blob Storage for .NET](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet)
