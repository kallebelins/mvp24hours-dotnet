using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Security;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Security;

[Trait("Category", "Unit")]
public class RowLevelSecurityHelperTest
{
    [Fact]
    public void GenerateSqlServerRls_ContainsPolicyAndFunctionNames()
    {
        var helper = new RowLevelSecurityHelper();

        var script = helper.GenerateSqlServerRls<TestTenantEntity>("dbo", "TenantEntities");

        script.Should().Contain("TenantPolicy_TenantEntities");
        script.Should().Contain("fn_tenant_predicate_TenantEntities");
        script.Should().Contain("CREATE SECURITY POLICY [dbo].[TenantPolicy_TenantEntities]");
        script.Should().Contain("SESSION_CONTEXT(N'TenantId')");
    }

    [Fact]
    public void GenerateSqlServerRlsScript_ContainsPolicyNames()
    {
        var helper = new RowLevelSecurityHelper();

        var script = helper.GenerateSqlServerRlsScript("sales", "Orders", "TenantId");

        script.Should().Contain("TenantPolicy_Orders");
        script.Should().Contain("fn_tenant_predicate_Orders");
        script.Should().Contain("[sales].[Orders]");
        script.Should().Contain("CREATE SCHEMA Security");
    }

    [Fact]
    public void GenerateSqlServerRlsScript_UsesCustomSessionContextKey()
    {
        var helper = new RowLevelSecurityHelper(sessionContextKey: "CurrentTenant");

        var script = helper.GenerateSqlServerRlsScript("dbo", "Products");

        script.Should().Contain("SESSION_CONTEXT(N'CurrentTenant')");
        script.Should().NotContain("SESSION_CONTEXT(N'TenantId')");
    }

    [Fact]
    public void GeneratePostgreSqlRls_ContainsPolicyName()
    {
        var helper = new RowLevelSecurityHelper();

        var script = helper.GeneratePostgreSqlRls<TestTenantEntity>("public", "tenant_entities");

        script.Should().Contain("tenant_isolation_policy");
        script.Should().Contain("ENABLE ROW LEVEL SECURITY");
        script.Should().Contain("\"public\".\"tenant_entities\"");
        script.Should().Contain("current_setting('app.current_tenant_id', TRUE)");
    }

    [Fact]
    public void GeneratePostgreSqlRlsScript_ContainsPolicyAndSettings()
    {
        var helper = new RowLevelSecurityHelper();

        var script = helper.GeneratePostgreSqlRlsScript("app", "customers", "TenantId");

        script.Should().Contain("CREATE POLICY tenant_isolation_policy ON \"app\".\"customers\"");
        script.Should().Contain("DROP POLICY IF EXISTS tenant_isolation_policy");
        script.Should().Contain("\"TenantId\" = current_setting('app.current_tenant_id', TRUE)");
    }

    [Fact]
    public void GenerateSqlServerDropRlsScript_ContainsDropStatements()
    {
        var helper = new RowLevelSecurityHelper();

        var script = helper.GenerateSqlServerDropRlsScript("dbo", "Products");

        script.Should().Contain("DROP SECURITY POLICY [dbo].[TenantPolicy_Products]");
        script.Should().Contain("DROP FUNCTION Security.fn_tenant_predicate_Products");
    }

    [Fact]
    public void GeneratePostgreSqlDropRlsScript_ContainsDropAndDisable()
    {
        var helper = new RowLevelSecurityHelper();

        var script = helper.GeneratePostgreSqlDropRlsScript("public", "products");

        script.Should().Contain("DROP POLICY IF EXISTS tenant_isolation_policy ON \"public\".\"products\"");
        script.Should().Contain("DISABLE ROW LEVEL SECURITY");
    }

    [Fact]
    public void GenerateRlsScriptsForModel_ReturnsScriptsForTenantEntities()
    {
        var helper = new RowLevelSecurityHelper();
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestTenantEntity>().ToTable("TenantEntities", "dbo");
        modelBuilder.Entity<TestSoftDeleteEntity>();

        var scripts = helper.GenerateRlsScriptsForModel(modelBuilder, DatabaseType.SqlServer);

        scripts.Should().ContainKey("dbo.TenantEntities");
        scripts["dbo.TenantEntities"].Should().Contain("TenantPolicy_TenantEntities");
        scripts.Keys.Should().NotContain(k => k.Contains("TestSoftDeleteEntity", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateCombinedRlsScript_IncludesHeaderAndTableScripts()
    {
        var helper = new RowLevelSecurityHelper();
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestTenantEntity>().ToTable("TenantEntities");

        var script = helper.GenerateCombinedRlsScript(modelBuilder, DatabaseType.PostgreSql, "public");

        script.Should().Contain("-- Row-Level Security Configuration");
        script.Should().Contain("-- Database Type: PostgreSql");
        script.Should().Contain("-- Table:");
        script.Should().Contain("tenant_isolation_policy");
    }

    [Fact]
    public void SetSqlServerTenantContext_NullContext_ThrowsArgumentNullException()
    {
        var helper = new RowLevelSecurityHelper();

        Action act = () => helper.SetSqlServerTenantContext(null!, "tenant-1");

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task SetSqlServerTenantContextAsync_NullContext_ThrowsArgumentNullException()
    {
        var helper = new RowLevelSecurityHelper();

        Func<Task> act = () => helper.SetSqlServerTenantContextAsync(null!, "tenant-1");

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void SetPostgreSqlTenantContext_NullContext_ThrowsArgumentNullException()
    {
        var helper = new RowLevelSecurityHelper();

        Action act = () => helper.SetPostgreSqlTenantContext(null!, "tenant-1");

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task SetPostgreSqlTenantContextAsync_NullContext_ThrowsArgumentNullException()
    {
        var helper = new RowLevelSecurityHelper();

        Func<Task> act = () => helper.SetPostgreSqlTenantContextAsync(null!, "tenant-1");

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void SetTenantContextForSqlServer_NullContext_ThrowsArgumentNullException()
    {
        DbContext? context = null;

        Action act = () => context!.SetTenantContextForSqlServer("tenant-1");

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void SetTenantContextForPostgreSql_NullContext_ThrowsArgumentNullException()
    {
        DbContext? context = null;

        Action act = () => context!.SetTenantContextForPostgreSql("tenant-1");

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }
}
