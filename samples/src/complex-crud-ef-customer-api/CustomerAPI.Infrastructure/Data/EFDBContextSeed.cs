using CustomerAPI.Core.Entities;

namespace CustomerAPI.Infrastructure.Data;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
public static class EFDBContextSeed
{
    public static async Task SeedAsync(EFDBContext dbContext, TimeProvider timeProvider)
    {
        // Seed baseline customers when the database is empty.
        if (!dbContext.Customer.Any())
        {
            dbContext.Customer.AddRange(GetCustomers(timeProvider));
            await dbContext.SaveChangesAsync();
        }
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
                Note = "Customer charged via standard charge.",
                Contacts =
                [
                    new Contact
                    {
                        Created = timeProvider.GetUtcNow().UtcDateTime,
                        Description = "(800) 997-348",
                        Active = true
                    }
                ]
            },
            new Customer
            {
                Created = timeProvider.GetUtcNow().UtcDateTime,
                Name = "Jonah Harvey",
                Active = true,
                Note = "Customer charged via standard charge.",
                Contacts =
                [
                    new Contact
                    {
                        Created = timeProvider.GetUtcNow().UtcDateTime,
                        Description = "1-392-598-4254",
                        Active = true
                    }
                ]
            }
        ];
    }
}
