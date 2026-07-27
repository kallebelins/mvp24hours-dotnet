using CustomerAPI.Entities;
using Mvp24Hours.Core.Contract.Data;

namespace CustomerAPI.Data;

/// <summary>
/// Seeds development Customer documents when the collection is empty.
/// </summary>
public static class MongoDbContextSeed
{
    public static async Task SeedAsync(
        this IUnitOfWorkAsync unitOfWork,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        IRepositoryAsync<Customer> repository = unitOfWork.GetRepository<Customer>();
        if (await repository.ListAnyAsync(cancellationToken))
        {
            return;
        }

        await repository.AddAsync(GetCustomers(timeProvider), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static List<Customer> GetCustomers(TimeProvider timeProvider)
    {
        return
        [
            new Customer
            {
                Created = timeProvider.GetUtcNow().UtcDateTime,
                Name = "Cherokee Macdonald",
                Active = true,
                Note = "Customer charged via standard charge."
            },
            new Customer
            {
                Created = timeProvider.GetUtcNow().UtcDateTime,
                Name = "Jonah Harvey",
                Active = true,
                Note = "Customer charged via standard charge."
            }
        ];
    }
}
