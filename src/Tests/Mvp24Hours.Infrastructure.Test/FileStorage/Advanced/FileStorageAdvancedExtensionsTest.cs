//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Moq;
using Mvp24Hours.Infrastructure.FileStorage.Advanced;
using Mvp24Hours.Infrastructure.FileStorage.Contract;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Advanced;

[Trait("Category", "Unit")]
public class FileStorageAdvancedExtensionsTest
{
    private readonly Mock<IFileStorage> _basicStorageMock = new();

    [Fact]
    public void SupportsPresignedUrls_WithBasicStorage_ShouldReturnFalse()
    {
        _basicStorageMock.Object.SupportsPresignedUrls().Should().BeFalse();
    }

    [Fact]
    public void SupportsPresignedUrls_WithPresignedStorage_ShouldReturnTrue()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IPresignedUrlStorage>();

        mock.Object.SupportsPresignedUrls().Should().BeTrue();
    }

    [Fact]
    public void AsPresignedUrlStorage_WithBasicStorage_ShouldReturnNull()
    {
        _basicStorageMock.Object.AsPresignedUrlStorage().Should().BeNull();
    }

    [Fact]
    public void AsPresignedUrlStorage_WithPresignedStorage_ShouldReturnCast()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IPresignedUrlStorage>();

        mock.Object.AsPresignedUrlStorage().Should().BeSameAs(mock.As<IPresignedUrlStorage>().Object);
    }

    [Fact]
    public void SupportsVersioning_WithBasicStorage_ShouldReturnFalse()
    {
        _basicStorageMock.Object.SupportsVersioning().Should().BeFalse();
    }

    [Fact]
    public void SupportsVersioning_WithVersioningStorage_ShouldReturnTrue()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IFileVersioningStorage>();

        mock.Object.SupportsVersioning().Should().BeTrue();
    }

    [Fact]
    public void AsVersioningStorage_WithBasicStorage_ShouldReturnNull()
    {
        _basicStorageMock.Object.AsVersioningStorage().Should().BeNull();
    }

    [Fact]
    public void AsVersioningStorage_WithVersioningStorage_ShouldReturnCast()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IFileVersioningStorage>();

        mock.Object.AsVersioningStorage().Should().BeSameAs(mock.As<IFileVersioningStorage>().Object);
    }

    [Fact]
    public void SupportsSoftDelete_WithBasicStorage_ShouldReturnFalse()
    {
        _basicStorageMock.Object.SupportsSoftDelete().Should().BeFalse();
    }

    [Fact]
    public void SupportsSoftDelete_WithSoftDeleteStorage_ShouldReturnTrue()
    {
        Mock<IFileStorage> mock = CreateStorageWith<ISoftDeleteStorage>();

        mock.Object.SupportsSoftDelete().Should().BeTrue();
    }

    [Fact]
    public void AsSoftDeleteStorage_WithBasicStorage_ShouldReturnNull()
    {
        _basicStorageMock.Object.AsSoftDeleteStorage().Should().BeNull();
    }

    [Fact]
    public void AsSoftDeleteStorage_WithSoftDeleteStorage_ShouldReturnCast()
    {
        Mock<IFileStorage> mock = CreateStorageWith<ISoftDeleteStorage>();

        mock.Object.AsSoftDeleteStorage().Should().BeSameAs(mock.As<ISoftDeleteStorage>().Object);
    }

    [Fact]
    public void SupportsImageProcessing_WithBasicStorage_ShouldReturnFalse()
    {
        _basicStorageMock.Object.SupportsImageProcessing().Should().BeFalse();
    }

    [Fact]
    public void SupportsImageProcessing_WithImageProcessingStorage_ShouldReturnTrue()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IImageProcessingStorage>();

        mock.Object.SupportsImageProcessing().Should().BeTrue();
    }

    [Fact]
    public void AsImageProcessingStorage_WithBasicStorage_ShouldReturnNull()
    {
        _basicStorageMock.Object.AsImageProcessingStorage().Should().BeNull();
    }

    [Fact]
    public void AsImageProcessingStorage_WithImageProcessingStorage_ShouldReturnCast()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IImageProcessingStorage>();

        mock.Object.AsImageProcessingStorage().Should().BeSameAs(mock.As<IImageProcessingStorage>().Object);
    }

    [Fact]
    public void SupportsChunkedUpload_WithBasicStorage_ShouldReturnFalse()
    {
        _basicStorageMock.Object.SupportsChunkedUpload().Should().BeFalse();
    }

    [Fact]
    public void SupportsChunkedUpload_WithChunkedUploadStorage_ShouldReturnTrue()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IChunkedUploadStorage>();

        mock.Object.SupportsChunkedUpload().Should().BeTrue();
    }

    [Fact]
    public void AsChunkedUploadStorage_WithBasicStorage_ShouldReturnNull()
    {
        _basicStorageMock.Object.AsChunkedUploadStorage().Should().BeNull();
    }

    [Fact]
    public void AsChunkedUploadStorage_WithChunkedUploadStorage_ShouldReturnCast()
    {
        Mock<IFileStorage> mock = CreateStorageWith<IChunkedUploadStorage>();

        mock.Object.AsChunkedUploadStorage().Should().BeSameAs(mock.As<IChunkedUploadStorage>().Object);
    }

    [Fact]
    public void SupportsCdn_WithBasicStorage_ShouldReturnFalse()
    {
        _basicStorageMock.Object.SupportsCdn().Should().BeFalse();
    }

    [Fact]
    public void SupportsCdn_WithCdnStorage_ShouldReturnTrue()
    {
        Mock<IFileStorage> mock = CreateStorageWith<ICdnStorage>();

        mock.Object.SupportsCdn().Should().BeTrue();
    }

    [Fact]
    public void AsCdnStorage_WithBasicStorage_ShouldReturnNull()
    {
        _basicStorageMock.Object.AsCdnStorage().Should().BeNull();
    }

    [Fact]
    public void AsCdnStorage_WithCdnStorage_ShouldReturnCast()
    {
        Mock<IFileStorage> mock = CreateStorageWith<ICdnStorage>();

        mock.Object.AsCdnStorage().Should().BeSameAs(mock.As<ICdnStorage>().Object);
    }

    [Fact]
    public void FullFeatureStorage_ShouldSupportAllAdvancedCapabilities()
    {
        Mock<IFileStorage> mock = new();
        _ = mock.As<IPresignedUrlStorage>();
        _ = mock.As<IFileVersioningStorage>();
        _ = mock.As<ISoftDeleteStorage>();
        _ = mock.As<IImageProcessingStorage>();
        _ = mock.As<IChunkedUploadStorage>();
        _ = mock.As<ICdnStorage>();

        IFileStorage storage = mock.Object;

        storage.SupportsPresignedUrls().Should().BeTrue();
        storage.SupportsVersioning().Should().BeTrue();
        storage.SupportsSoftDelete().Should().BeTrue();
        storage.SupportsImageProcessing().Should().BeTrue();
        storage.SupportsChunkedUpload().Should().BeTrue();
        storage.SupportsCdn().Should().BeTrue();

        storage.AsPresignedUrlStorage().Should().NotBeNull();
        storage.AsVersioningStorage().Should().NotBeNull();
        storage.AsSoftDeleteStorage().Should().NotBeNull();
        storage.AsImageProcessingStorage().Should().NotBeNull();
        storage.AsChunkedUploadStorage().Should().NotBeNull();
        storage.AsCdnStorage().Should().NotBeNull();
    }

    private static Mock<IFileStorage> CreateStorageWith<TAdvanced>()
        where TAdvanced : class
    {
        Mock<IFileStorage> mock = new();
        _ = mock.As<TAdvanced>();
        return mock;
    }
}
