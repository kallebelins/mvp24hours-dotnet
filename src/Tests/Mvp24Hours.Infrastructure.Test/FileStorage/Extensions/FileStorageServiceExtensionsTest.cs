//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Extensions;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.FileStorage.Providers;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Extensions;

[Trait("Category", "Unit")]
public class FileStorageServiceExtensionsTest
{
    [Fact]
    public void AddFileStorage_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => FileStorageServiceExtensions.AddFileStorage(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddFileStorage_ShouldRegisterLocalProviderByDefault()
    {
        var services = new ServiceCollection();
        services.AddFileStorage(options => options.BasePath = "uploads");

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IFileStorage>().Should().BeOfType<LocalFileStorageProvider>();
        sp.GetRequiredService<IOptions<FileStorageOptions>>().Value.BasePath.Should().Be("uploads");
    }

    [Fact]
    public void AddInMemoryFileStorage_ShouldRegisterInMemoryProvider()
    {
        var services = new ServiceCollection();
        services.AddInMemoryFileStorage();

        IFileStorage storage = services.BuildServiceProvider().GetRequiredService<IFileStorage>();

        storage.Should().BeOfType<InMemoryFileStorageProvider>();
    }

    [Fact]
    public void AddLocalFileStorage_WithTempPath_ShouldRegisterLocalProvider()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "mvp24hours-fs-" + Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();

        services.AddLocalFileStorage(options => options.BasePath = tempPath);

        IFileStorage storage = services.BuildServiceProvider().GetRequiredService<IFileStorage>();

        storage.Should().BeOfType<LocalFileStorageProvider>();
    }

    [Fact]
    public void AddFileStorageWithProvider_WithNullFactory_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddFileStorageWithProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public void AddFileStorageWithProvider_ShouldUseCustomFactory()
    {
        var services = new ServiceCollection();
        services.AddFileStorageWithProvider((_, options) => new InMemoryFileStorageProvider(options));

        IFileStorage storage = services.BuildServiceProvider().GetRequiredService<IFileStorage>();

        storage.Should().BeOfType<InMemoryFileStorageProvider>();
    }

    [Fact]
    public void AddAzureBlobStorage_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddAzureBlobStorage("  ", "container");

        act.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }

    [Fact]
    public void AddAzureBlobStorage_WithEmptyContainerName_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddAzureBlobStorage("UseDevelopmentStorage=true", "  ");

        act.Should().Throw<ArgumentException>().WithParameterName("containerName");
    }

    [Fact]
    public void AddAzureBlobStorage_ShouldRegisterAzureProvider()
    {
        var services = new ServiceCollection();
        services.AddAzureBlobStorage(
            "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
            "files");

        IFileStorage storage = services.BuildServiceProvider().GetRequiredService<IFileStorage>();

        storage.Should().BeOfType<AzureBlobStorageProvider>();
    }

    [Fact]
    public void AddAwsS3Storage_WithEmptyBucketName_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddAwsS3Storage("  ");

        act.Should().Throw<ArgumentException>().WithParameterName("bucketName");
    }

    [Fact]
    public void AddAwsS3Storage_ShouldRegisterS3Provider()
    {
        var services = new ServiceCollection();
        services.AddAwsS3Storage(
            bucketName: "my-bucket",
            accessKeyId: "AKIAIOSFODNN7EXAMPLE",
            secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            region: "us-east-1");

        IFileStorage storage = services.BuildServiceProvider().GetRequiredService<IFileStorage>();

        storage.Should().BeOfType<AwsS3StorageProvider>();
    }
}
