using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

public sealed class TestEntitySeeder : IDataSeeder<TestDbContext>
{
    public const int DefaultSeedCount = 3;

    public void Seed(TestDbContext context)
    {
        if (context.Entities.Any())
        {
            return;
        }

        context.Entities.AddRange(
            new TestEntity { Id = 1, Name = "Seeded-1", Active = true, Score = 10 },
            new TestEntity { Id = 2, Name = "Seeded-2", Active = false, Score = 20 },
            new TestEntity { Id = 3, Name = "Seeded-3", Active = true, Score = 30 });
    }
}
