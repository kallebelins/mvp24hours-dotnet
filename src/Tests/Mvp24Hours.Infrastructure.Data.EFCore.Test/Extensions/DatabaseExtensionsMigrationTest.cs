using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class DatabaseExtensionsMigrationTest
{
    [Fact]
    public void MigrateDatabase_WithSqlite_ShouldInvokeSeeder()
    {
        using SqliteHostFixture fixture = CreateFixture();
        bool seederInvoked = false;

        IHost result = fixture.Host.MigrateDatabase<TestDbContext>((context, services) =>
        {
            seederInvoked = true;
            context.Should().NotBeNull();
            services.Should().NotBeNull();
        });

        result.Should().BeSameAs(fixture.Host);
        seederInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task MigrateDatabaseAsync_WithSqlite_ShouldInvokeSeeder()
    {
        using SqliteHostFixture fixture = CreateFixture();
        bool seederInvoked = false;

        IHost result = await fixture.Host.MigrateDatabaseAsync<TestDbContext>((context, services) =>
        {
            seederInvoked = true;
            context.Should().NotBeNull();
            services.Should().NotBeNull();
        });

        result.Should().BeSameAs(fixture.Host);
        seederInvoked.Should().BeTrue();
    }

    [Fact]
    public void MigrateDatabaseSQL_WithSqlite_ShouldExecuteCommandsAndInvokeSeeder()
    {
        using SqliteHostFixture fixture = CreateFixture();
        bool seederInvoked = false;

        IHost result = fixture.Host.MigrateDatabaseSQL<TestDbContext>(
            (context, services) =>
            {
                seederInvoked = true;
                context.Database.EnsureCreated();
                context.Entities.Add(new TestEntity { Name = "SqlSeed" });
                context.SaveChanges();
            },
            ["SELECT 1;", "   ", string.Empty]);

        result.Should().BeSameAs(fixture.Host);
        seederInvoked.Should().BeTrue();

        using IServiceScope scope = fixture.Host.Services.CreateScope();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Entities.Should().ContainSingle(e => e.Name == "SqlSeed");
    }

    [Fact]
    public async Task MigrateDatabaseSQLAsync_WithSqlite_ShouldExecuteCommandsAndInvokeSeeder()
    {
        using SqliteHostFixture fixture = CreateFixture();
        bool seederInvoked = false;

        IHost result = await fixture.Host.MigrateDatabaseSQLAsync<TestDbContext>(
            (context, services) =>
            {
                seederInvoked = true;
                context.Database.EnsureCreated();
                context.Entities.Add(new TestEntity { Name = "SqlAsyncSeed" });
                context.SaveChanges();
            },
            ["SELECT 1;", "\t\n"]);

        result.Should().BeSameAs(fixture.Host);
        seederInvoked.Should().BeTrue();

        using IServiceScope scope = fixture.Host.Services.CreateScope();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Entities.Should().ContainSingle(e => e.Name == "SqlAsyncSeed");
    }

    [Fact]
    public void ReadSqlScriptFile_WithoutGoDelimiter_ShouldReturnSingleBatch()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mvp_sql_single_{Guid.NewGuid():N}.sql");
        try
        {
            File.WriteAllText(path, "SELECT 1;\nSELECT 2;");

            string[] batches = DatabaseExtensions.ReadSqlScriptFile(path);

            batches.Should().HaveCount(1);
            batches[0].Should().Contain("SELECT 1");
            batches[0].Should().Contain("SELECT 2");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static SqliteHostFixture CreateFixture()
    {
        return new SqliteHostFixture();
    }

    private sealed class SqliteHostFixture : IDisposable
    {
        public SqliteConnection KeepAlive { get; }
        public IHost Host { get; }

        public SqliteHostFixture()
        {
            string connectionString = $"Data Source=file:migrate_{Guid.NewGuid():N}?mode=memory&cache=shared";
            KeepAlive = new SqliteConnection(connectionString);
            KeepAlive.Open();

            Host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDbContext<TestDbContext>(options => options.UseSqlite(connectionString));
                })
                .Build();
        }

        public void Dispose()
        {
            Host.Dispose();
            KeepAlive.Dispose();
        }
    }
}
