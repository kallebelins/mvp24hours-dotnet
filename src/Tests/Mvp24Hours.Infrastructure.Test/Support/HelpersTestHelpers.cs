//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class HelpersTestHelpers
{
    public static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mvp24h-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static X509Certificate2 CreateSelfSignedCertificate(string subject = "CN=Mvp24Hours.Test")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subject,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));
    }

    public static byte[] ExportCertificateBytes(X509Certificate2 certificate)
    {
        return certificate.Export(X509ContentType.Cert);
    }

    public static byte[] ExportPfxBytes(X509Certificate2 certificate, string password)
    {
        return certificate.Export(X509ContentType.Pfx, password);
    }

    public static string ExportCertificateBase64(X509Certificate2 certificate)
    {
        return Convert.ToBase64String(ExportCertificateBytes(certificate));
    }

    public static string ExportPfxBase64(X509Certificate2 certificate, string password)
    {
        return Convert.ToBase64String(ExportPfxBytes(certificate, password));
    }

    public sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = CreateTempDirectory();
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
