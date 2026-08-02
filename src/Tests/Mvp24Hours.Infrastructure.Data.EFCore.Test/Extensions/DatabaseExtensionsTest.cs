using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class DatabaseExtensionsTest
{
    [Fact]
    public void ReadSqlScriptFile_WithGoBatches_ShouldSplit()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mvp_sql_{Guid.NewGuid():N}.sql");
        try
        {
            File.WriteAllText(path, """
                CREATE TABLE A (Id INT);
                GO
                CREATE TABLE B (Id INT);
                go
                INSERT INTO A VALUES (1);
                """);

            string[] batches = DatabaseExtensions.ReadSqlScriptFile(path);

            batches.Should().HaveCountGreaterThanOrEqualTo(2);
            batches.Should().Contain(b => b.Contains("CREATE TABLE A", StringComparison.OrdinalIgnoreCase));
            batches.Should().Contain(b => b.Contains("CREATE TABLE B", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ReadSqlScriptFileAsync_WithGoBatches_ShouldSplit()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mvp_sql_async_{Guid.NewGuid():N}.sql");
        try
        {
            await File.WriteAllTextAsync(path, """
                SELECT 1;
                GO
                SELECT 2;
                """);

            string[] batches = await DatabaseExtensions.ReadSqlScriptFileAsync(path);

            batches.Should().HaveCountGreaterThanOrEqualTo(2);
            string joined = string.Join("|", batches);
            joined.Should().Contain("SELECT 1");
            joined.Should().Contain("SELECT 2");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ReadSqlScriptFile_WhenMissing_ShouldThrowFileNotFound()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.sql");

        Func<string[]> act = () => DatabaseExtensions.ReadSqlScriptFile(missing);

        act.Should().Throw<FileNotFoundException>().WithMessage("*script*");
    }

    [Fact]
    public async Task ReadSqlScriptFileAsync_WhenMissing_ShouldThrowFileNotFound()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing_async_{Guid.NewGuid():N}.sql");

        Func<Task<string[]>> act = async () => await DatabaseExtensions.ReadSqlScriptFileAsync(missing);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
