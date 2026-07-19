//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Security.Cryptography.X509Certificates;
using Mvp24Hours.Infrastructure.Http.Helpers;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class CertificateHelperTest
{
    [Fact]
    public void LoadCertificate_WithNullOptions_ShouldReturnNull()
    {
        // Act
        X509Certificate2? result = CertificateHelper.LoadCertificate(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LoadCertificate_WithEmptyOptions_ShouldReturnNull()
    {
        // Arrange
        var options = new CertificateOptions();

        // Act
        X509Certificate2? result = CertificateHelper.LoadCertificate(options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LoadCertificate_WithFilePath_ShouldLoadFromFile()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        using var temp = new HelpersTestHelpers.TempDirectory();
        string filePath = Path.Combine(temp.Path, "cert.cer");
        File.WriteAllBytes(filePath, HelpersTestHelpers.ExportCertificateBytes(cert));

        var options = new CertificateOptions { FilePath = filePath };

        // Act
        using X509Certificate2? loaded = CertificateHelper.LoadCertificate(options);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Subject.Should().Contain("CN=Mvp24Hours.Test");
    }

    [Fact]
    public void LoadCertificate_WithBase64_ShouldLoadFromBase64WhenFilePathMissing()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        var options = new CertificateOptions
        {
            Base64Certificate = HelpersTestHelpers.ExportCertificateBase64(cert)
        };

        // Act
        using X509Certificate2? loaded = CertificateHelper.LoadCertificate(options);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Thumbprint.Should().Be(cert.Thumbprint);
    }

    [Fact]
    public void LoadCertificate_WithFilePathAndBase64_ShouldPreferFilePath()
    {
        // Arrange
        using var fileCert = HelpersTestHelpers.CreateSelfSignedCertificate("CN=FromFile");
        using var base64Cert = HelpersTestHelpers.CreateSelfSignedCertificate("CN=FromBase64");
        using var temp = new HelpersTestHelpers.TempDirectory();
        string filePath = Path.Combine(temp.Path, "cert.cer");
        File.WriteAllBytes(filePath, HelpersTestHelpers.ExportCertificateBytes(fileCert));

        var options = new CertificateOptions
        {
            FilePath = filePath,
            Base64Certificate = HelpersTestHelpers.ExportCertificateBase64(base64Cert)
        };

        // Act
        using X509Certificate2? loaded = CertificateHelper.LoadCertificate(options);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Subject.Should().Contain("CN=FromFile");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LoadFromFile_WithNullOrEmptyPath_ShouldThrowArgumentNullException(string? filePath)
    {
        // Act
        Action act = () => CertificateHelper.LoadFromFile(filePath!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }

    [Fact]
    public void LoadFromFile_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.cer");

        // Act
        Action act = () => CertificateHelper.LoadFromFile(missingPath);

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{missingPath}*");
    }

    [Fact]
    public void LoadFromFile_WithValidCerFile_ShouldReturnCertificate()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        using var temp = new HelpersTestHelpers.TempDirectory();
        string filePath = Path.Combine(temp.Path, "cert.cer");
        File.WriteAllBytes(filePath, HelpersTestHelpers.ExportCertificateBytes(cert));

        // Act
        using X509Certificate2 loaded = CertificateHelper.LoadFromFile(filePath);

        // Assert
        loaded.Subject.Should().Contain("CN=Mvp24Hours.Test");
        loaded.Thumbprint.Should().Be(cert.Thumbprint);
    }

    [Fact]
    public void LoadFromFile_WithValidPfxFile_ShouldReturnCertificate()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        using var temp = new HelpersTestHelpers.TempDirectory();
        const string password = "test-pwd";
        string filePath = Path.Combine(temp.Path, "cert.pfx");
        File.WriteAllBytes(filePath, HelpersTestHelpers.ExportPfxBytes(cert, password));

        // Act
        using X509Certificate2 loaded = CertificateHelper.LoadFromFile(filePath, password);

        // Assert
        loaded.Subject.Should().Contain("CN=Mvp24Hours.Test");
    }

    [Fact]
    public void LoadFromBase64_WithValidCertificate_ShouldReturnCertificate()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        string base64 = HelpersTestHelpers.ExportCertificateBase64(cert);

        // Act
        using X509Certificate2 loaded = CertificateHelper.LoadFromBase64(base64);

        // Assert
        loaded.Thumbprint.Should().Be(cert.Thumbprint);
    }

    [Fact]
    public void LoadFromBase64_WithValidPfx_ShouldReturnCertificate()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        const string password = "test-pwd";
        string base64 = HelpersTestHelpers.ExportPfxBase64(cert, password);

        // Act
        using X509Certificate2 loaded = CertificateHelper.LoadFromBase64(base64, password);

        // Assert
        loaded.Subject.Should().Contain("CN=Mvp24Hours.Test");
    }

    [Fact]
    public void LoadFromBase64_WithInvalidBase64_ShouldThrowFormatException()
    {
        // Act
        Action act = () => CertificateHelper.LoadFromBase64("not-valid-base64!!!");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    public void LoadFromBytes_WithNullOrEmptyBytes_ShouldThrowArgumentNullException(byte[]? bytes)
    {
        // Act
        Action act = () => CertificateHelper.LoadFromBytes(bytes!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("certificateBytes");
    }

    [Fact]
    public void LoadFromBytes_WithValidBytes_ShouldReturnCertificate()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();
        byte[] bytes = HelpersTestHelpers.ExportCertificateBytes(cert);

        // Act
        using X509Certificate2 loaded = CertificateHelper.LoadFromBytes(bytes);

        // Assert
        loaded.Thumbprint.Should().Be(cert.Thumbprint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LoadFromStoreByThumbprint_WithNullOrEmptyThumbprint_ShouldThrowArgumentNullException(string? thumbprint)
    {
        // Act
        Action act = () => CertificateHelper.LoadFromStoreByThumbprint(thumbprint!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("thumbprint");
    }

    [Fact]
    public void LoadFromStoreByThumbprint_WithUnknownThumbprint_ShouldReturnNull()
    {
        // Act
        X509Certificate2? result = CertificateHelper.LoadFromStoreByThumbprint(
            "0000000000000000000000000000000000000000");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LoadFromStoreBySubjectName_WithNullOrEmptySubjectName_ShouldThrowArgumentNullException(string? subjectName)
    {
        // Act
        Action act = () => CertificateHelper.LoadFromStoreBySubjectName(subjectName!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("subjectName");
    }

    [Fact]
    public void IsValid_WithNullCertificate_ShouldReturnFalse()
    {
        // Act
        bool result = CertificateHelper.IsValid(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithValidSelfSignedCertificate_ShouldReturnTrue()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();

        // Act
        bool result = CertificateHelper.IsValid(cert);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnsureValid_WithNullCertificate_ShouldThrowInvalidOperationException()
    {
        // Act
        Action act = () => CertificateHelper.EnsureValid(null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Certificate is null.*");
    }

    [Fact]
    public void EnsureValid_WithValidSelfSignedCertificate_ShouldNotThrow()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();

        // Act
        Action act = () => CertificateHelper.EnsureValid(cert);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetDaysUntilExpiration_WithNullCertificate_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => CertificateHelper.GetDaysUntilExpiration(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("certificate");
    }

    [Fact]
    public void GetDaysUntilExpiration_WithValidCertificate_ShouldReturnNonNegativeDays()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();

        // Act
        int days = CertificateHelper.GetDaysUntilExpiration(cert);

        // Assert
        days.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetCertificateInfo_WithNullCertificate_ShouldReturnNullMessage()
    {
        // Act
        string info = CertificateHelper.GetCertificateInfo(null!);

        // Assert
        info.Should().Be("Certificate is null");
    }

    [Fact]
    public void GetCertificateInfo_WithValidCertificate_ShouldContainSubjectAndThumbprint()
    {
        // Arrange
        using var cert = HelpersTestHelpers.CreateSelfSignedCertificate();

        // Act
        string info = CertificateHelper.GetCertificateInfo(cert);

        // Assert
        info.Should().Contain("Subject:");
        info.Should().Contain(cert.Subject);
        info.Should().Contain("Thumbprint:");
        info.Should().Contain(cert.Thumbprint);
    }
}
