# One-shot scaffold for remaining sample smoke tests. Safe to re-run (skips existing files).
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location (Join-Path $repoRoot 'samples')

$efTestingJson = @'
{
  "ConnectionStrings": {
    "EFDBContext": "Server=(localdb)\\mssqllocaldb;Database=Sample_Testing;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
'@

$customerDbTestingJson = @'
{
  "ConnectionStrings": {
    "CustomerDbContext": "Server=(localdb)\\mssqllocaldb;Database=SampleLog_Testing;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
'@

$mongoTestingJson = @'
{
  "ConnectionStrings": {
    "MongoDbContext": "mongodb://localhost:27017"
  }
}
'@

$redisTestingJson = @'
{
  "ConnectionStrings": {
    "RedisDbContext": "localhost:6379"
  }
}
'@

$rabbitTestingJson = @'
{
  "ConnectionStrings": {
    "EFDBContext": "Server=(localdb)\\mssqllocaldb;Database=RabbitSample_Testing;Trusted_Connection=True;TrustServerCertificate=True;",
    "RabbitMQContext": "amqp://guest:guest@localhost:5672"
  }
}
'@

$pipelineTestingJson = @'
{
  "Settings": {
    "TypicodeCustomerUrl": "https://jsonplaceholder.typicode.com/users"
  }
}
'@

$efFactory = @'
using CustomerAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerAPI.Test.Integration;

public class CustomerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<EFDBContext>) ||
                d.ServiceType == typeof(EFDBContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<EFDBContext>)).ToList();

            foreach (ServiceDescriptor? d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<EFDBContext>(o =>
                o.UseInMemoryDatabase("Smoke_" + Guid.NewGuid()));
        });
    }
}
'@

$minimalEfFactory = @'
using CustomerAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerAPI.Test.Integration;

public class CustomerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<EFDBContext>) ||
                d.ServiceType == typeof(EFDBContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<EFDBContext>)).ToList();

            foreach (ServiceDescriptor? d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<EFDBContext>(o =>
                o.UseInMemoryDatabase("Smoke_" + Guid.NewGuid()));
        });
    }
}
'@

$mongoFactory = @'
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CustomerAPI.Test.Integration;

public sealed class CustomerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDbContext"] = "mongodb://localhost:27017"
            });
        });
    }
}
'@

$simpleFactory = @'
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CustomerAPI.Test.Integration;

public class CustomerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
'@

$smokeTests = @'
namespace CustomerAPI.Test.Integration;

[Trait("Category", "Unit")]
public class OpenApiSmokeTests : IClassFixture<CustomerApiFactory>
{
    private readonly CustomerApiFactory _factory;

    public OpenApiSmokeTests(CustomerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOpenApiDocument_WhenTestingHost_ReturnsNon5xx()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}
'@

$globalUsings = @'
global using FluentAssertions;
global using Xunit;
'@

function New-TestCsproj([string]$HostProjectRelative) {
@"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\$HostProjectRelative" />
  </ItemGroup>

</Project>
"@
}

function Add-PartialProgram([string]$ProgramPath) {
    if (-not (Test-Path $ProgramPath)) { return }
    $content = Get-Content $ProgramPath -Raw
    if ($content -match 'public partial class Program') { return }
    Add-Content -Path $ProgramPath -Value "`npublic partial class Program { }"
}

function Add-TestingAppSettings([string]$HostDir, [string]$Json) {
    $path = Join-Path $HostDir 'appsettings.Testing.json'
    if (-not (Test-Path $path)) {
        Set-Content -Path $path -Value $Json -NoNewline
    }
}

function Wrap-StartupDbBlock([string]$ProgramPath) {
    if (-not (Test-Path $ProgramPath)) { return }
    $content = Get-Content $ProgramPath -Raw
    if ($content -match 'IsEnvironment\("Testing"\)') { return }

    $patterns = @(
        '(?ms)(^\s*await using \(.*?AsyncServiceScope scope = app\.Services\.CreateAsyncScope\(\)\)\s*\{.*?\}\s*)',
        '(?ms)(^\s*await using \(var scope = app\.Services\.CreateAsyncScope\(\)\)\s*\{.*?\}\s*)'
    )

    foreach ($pattern in $patterns) {
        if ($content -match $pattern) {
            $block = $Matches[1]
            $wrapped = "if (!app.Environment.IsEnvironment(`"Testing`"))`n    {`n$block    }`n"
            $content = $content.Replace($block, $wrapped)
            Set-Content -Path $ProgramPath -Value $content -NoNewline
            return
        }
    }
}

function Add-TestingGuardToExtension([string]$ExtensionPath, [string]$MethodPattern) {
    if (-not (Test-Path $ExtensionPath)) { return }
    $content = Get-Content $ExtensionPath -Raw
    if ($content -match 'IsEnvironment\("Testing"\)') { return }
    $content = $content -replace "($MethodPattern\s*\{)", "`$1`n        if (app.Environment.IsEnvironment(`"Testing`"))`n        {`n            return;`n        }`n"
    Set-Content -Path $ExtensionPath -Value $content -NoNewline
}

function Update-Slnx([string]$SlnxPath, [string]$TestRelativePath) {
    if (-not (Test-Path $SlnxPath)) { return }
    $content = Get-Content $SlnxPath -Raw
    if ($content -match [regex]::Escape($TestRelativePath)) { return }
    $content = $content -replace '</Solution>', "  <Project Path=`"$TestRelativePath`" />`n</Solution>"
    Set-Content -Path $SlnxPath -Value $content -NoNewline
}

function Scaffold-Sample {
    param(
        [string]$Sample,
        [string]$HostProject, # relative to sample dir e.g. CustomerAPI.WebAPI
        [string]$FactoryKind, # ef, minimal-ef, mongo, simple
        [string]$TestingJson,
        [string]$SlnxFile,
        [switch]$WrapDbStartup,
        [switch]$GuardMinimalExtension
    )

    $sampleDir = Join-Path 'src' $Sample
    $hostDir = Join-Path $sampleDir $HostProject
    $testDir = Join-Path $sampleDir 'CustomerAPI.Test'
    $integrationDir = Join-Path $testDir 'Integration'
    New-Item -ItemType Directory -Force -Path $integrationDir | Out-Null

    $hostCsproj = if ($HostProject -eq 'CustomerAPI') { 'CustomerAPI/CustomerAPI.csproj' } else { "$HostProject/$HostProject.csproj" }
    $csprojPath = Join-Path $testDir 'CustomerAPI.Test.csproj'
    if (-not (Test-Path $csprojPath)) {
        Set-Content -Path $csprojPath -Value (New-TestCsproj $hostCsproj)
    }

    $globalPath = Join-Path $testDir 'GlobalUsings.cs'
    if (-not (Test-Path $globalPath)) { Set-Content -Path $globalPath -Value $globalUsings }

    $factoryPath = Join-Path $integrationDir 'CustomerApiFactory.cs'
    if (-not (Test-Path $factoryPath)) {
        $factory = switch ($FactoryKind) {
            'ef' { $efFactory }
            'minimal-ef' { $minimalEfFactory }
            'mongo' { $mongoFactory }
            default { $simpleFactory }
        }
        Set-Content -Path $factoryPath -Value $factory
    }

    $smokePath = Join-Path $integrationDir 'OpenApiSmokeTests.cs'
    if (-not (Test-Path $smokePath)) { Set-Content -Path $smokePath -Value $smokeTests }

    Add-TestingAppSettings $hostDir $TestingJson
    Add-PartialProgram (Join-Path $hostDir 'Program.cs')

    if ($WrapDbStartup) {
        Wrap-StartupDbBlock (Join-Path $hostDir 'Program.cs')
    }

    if ($GuardMinimalExtension) {
        $ext = Join-Path $hostDir 'Extensions/ApplicationBuilderExtensions.cs'
        if ($Sample -like '*mongodb*') {
            Add-TestingGuardToExtension $ext 'public static async Task SeedDatabaseAsync\(this WebApplication app\)'
        } else {
            Add-TestingGuardToExtension $ext 'public static async Task MigrateDatabaseAsync\(this WebApplication app\)'
        }
    }

    Update-Slnx (Join-Path $sampleDir $SlnxFile) 'CustomerAPI.Test/CustomerAPI.Test.csproj'
    Write-Output "Scaffolded $Sample"
}

$definitions = @(
    @{ Sample='complex-crud-ef-dapper-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$efTestingJson; Slnx='ComplexCrudEfDapperCustomerApi.slnx'; Wrap=$true },
    @{ Sample='complex-crud-ef-entitylog-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$efTestingJson; Slnx='ComplexCrudEfEntitylogCustomerApi.slnx'; Wrap=$true },
    @{ Sample='complex-crud-ef-only-entity-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$efTestingJson; Slnx='ComplexCrudEfOnlyEntityCustomerApi.slnx'; Wrap=$true },
    @{ Sample='complex-pipeline-ef-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$efTestingJson; Slnx='ComplexPipelineEfCustomerApi.slnx'; Wrap=$true },
    @{ Sample='complex-pipeline-builder-customer-api'; Host='CustomerAPI.WebAPI'; Factory='simple'; Json=$pipelineTestingJson; Slnx='ComplexPipelineBuilderCustomerApi.slnx'; Wrap=$false },
    @{ Sample='complex-pipeline-customer-api'; Host='CustomerAPI.WebAPI'; Factory='simple'; Json=$pipelineTestingJson; Slnx='ComplexPipelineCustomerApi.slnx'; Wrap=$false },
    @{ Sample='complex-pipeline-ports-adapters-customer-api'; Host='CustomerAPI.WebAPI'; Factory='simple'; Json=$pipelineTestingJson; Slnx='ComplexPipelinePortsAdaptersCustomerApi.slnx'; Wrap=$false },
    @{ Sample='minimal-crud-ef-customer-api'; Host='CustomerAPI'; Factory='minimal-ef'; Json=$efTestingJson; Slnx='MinimalCrudEfCustomerApi.slnx'; Wrap=$false; Minimal=$true },
    @{ Sample='minimal-crud-mongodb-customer-api'; Host='CustomerAPI'; Factory='mongo'; Json=$mongoTestingJson; Slnx='MinimalCrudMongodbCustomerApi.slnx'; Wrap=$false; Minimal=$true; Mongo=$true },
    @{ Sample='minimal-pipeline-customer-api'; Host='CustomerAPI'; Factory='simple'; Json=$pipelineTestingJson; Slnx='MinimalPipelineCustomerApi.slnx'; Wrap=$false },
    @{ Sample='simple-crud-ef-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$efTestingJson; Slnx='SimpleCrudEfCustomerApi.slnx'; Wrap=$true },
    @{ Sample='simple-crud-ef-dapper-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$efTestingJson; Slnx='SimpleCrudEfDapperCustomerApi.slnx'; Wrap=$true },
    @{ Sample='simple-crud-ef-entitylog-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$customerDbTestingJson; Slnx='SimpleCrudEfEntitylogCustomerApi.slnx'; Wrap=$true },
    @{ Sample='simple-crud-mongodb-customer-api'; Host='CustomerAPI.WebAPI'; Factory='mongo'; Json=$mongoTestingJson; Slnx='SimpleCrudMongodbCustomerApi.slnx'; Wrap=$false },
    @{ Sample='simple-crud-redis-customer-api'; Host='CustomerAPI.WebAPI'; Factory='simple'; Json=$redisTestingJson; Slnx='SimpleCrudRedisCustomerApi.slnx'; Wrap=$false },
    @{ Sample='simple-pipeline-customer-api'; Host='CustomerAPI.WebAPI'; Factory='simple'; Json=$pipelineTestingJson; Slnx='SimplePipelineCustomerApi.slnx'; Wrap=$false },
    @{ Sample='simple-rabbitmq-customer-api'; Host='CustomerAPI.WebAPI'; Factory='ef'; Json=$rabbitTestingJson; Slnx='SimpleRabbitmqCustomerApi.slnx'; Wrap=$true }
)

foreach ($d in $definitions) {
    Scaffold-Sample -Sample $d.Sample -HostProject $d.Host -FactoryKind $d.Factory -TestingJson $d.Json -SlnxFile $d.Slnx -WrapDbStartup:($d.Wrap) -GuardMinimalExtension:($d.Minimal -or $d.Mongo)
}

foreach ($d in $definitions) {
    $proj = Join-Path 'src' ($d.Sample + '/CustomerAPI.Test/CustomerAPI.Test.csproj')
    dotnet sln Mvp24Hours.Samples.slnx add $proj 2>$null
}

Write-Output 'Done scaffolding.'
