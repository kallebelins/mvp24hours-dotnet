using CustomerAPI.Entities;

namespace CustomerAPI.Data
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public static class EFDBContextSeed
    {
        public static void Seed(this EFDBContext dbContext, TimeProvider timeProvider)
        {
            // Seed development customers when the database is empty.

            if (!dbContext.Customer.Any())
            {
                dbContext.Customer.AddRange(GetCustomers(timeProvider));
                dbContext.SaveChanges();
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
}
