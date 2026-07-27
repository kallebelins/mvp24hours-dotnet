using CustomerAPI.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Data
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public static class EFDBContextSeed
    {
        public static async Task SeedAsync(EFDBContext dbContext)
        {
            // Seed initial customer/contact rows when the database is empty.
            // Entity-log samples rely on EF interceptors/TimeProvider for Created/Modified timestamps.

            if (!dbContext.Customer.Any())
            {
                dbContext.Customer.AddRange(GetCustomers());
                await dbContext.SaveChangesAsync();
            }

            await Task.CompletedTask;
        }

        private static List<Customer> GetCustomers()
        {
            return
            [
                new Customer
                {
                    Name = "Cherokee Macdonald",
                    Active = true,
                    Note = "Customer charged via standard charge.",
                    Contacts = 
                    [
                        new Contact
                        {
                            Description = "(800) 997-348",
                            Active = true
                        }
                    ]
                },
                new Customer
                {
                    Name = "Jonah Harvey",
                    Active = true,
                    Note = "Customer charged via standard charge.",
                    Contacts = 
                    [
                        new Contact
                        {
                            Description = "1-392-598-4254",
                            Active = true
                        }
                    ]
                }
            ];
        }
    }
}
