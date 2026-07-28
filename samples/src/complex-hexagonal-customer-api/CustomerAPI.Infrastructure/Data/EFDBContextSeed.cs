using CustomerAPI.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Data
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public static class EFDBContextSeed
    {
        public static async Task SeedAsync(EFDBContext dbContext, TimeProvider timeProvider)
        {
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
                    Name = "Leanne Graham",
                    Active = true,
                    Note = "Seeded from hexagonal sample.",
                    Contacts =
                    [
                        new Contact
                        {
                            Created = timeProvider.GetUtcNow().UtcDateTime,
                            Description = "leanne.graham@example.com",
                            Type = Core.Enums.ContactType.Email,
                            Active = true
                        }
                    ]
                },
                new Customer
                {
                    Created = timeProvider.GetUtcNow().UtcDateTime,
                    Name = "Ervin Howell",
                    Active = true,
                    Note = "Seeded from hexagonal sample.",
                    Contacts =
                    [
                        new Contact
                        {
                            Created = timeProvider.GetUtcNow().UtcDateTime,
                            Description = "010-692-6593 x09125",
                            Type = Core.Enums.ContactType.CellPhone,
                            Active = true
                        }
                    ]
                }
            ];
        }
    }
}
