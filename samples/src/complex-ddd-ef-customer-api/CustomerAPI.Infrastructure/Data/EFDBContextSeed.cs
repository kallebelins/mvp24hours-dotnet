using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using CustomerAPI.Core.ValueObjects.Domain;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Data
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public static class EFDBContextSeed
    {
        public static async Task SeedAsync(EFDBContext dbContext, TimeProvider timeProvider)
        {
            if (dbContext.Customer.Any()) return;

            // Use aggregate factory and domain methods — seed demonstrates correct aggregate usage.
            var cherokee = Customer.Create(
                new CustomerName("Cherokee Macdonald"),
                timeProvider,
                "Customer charged via standard charge.");
            cherokee.AddContact(ContactType.Phone, new ContactDescription("(800) 997-348"), timeProvider);
            cherokee.ClearDomainEvents();

            var jonah = Customer.Create(
                new CustomerName("Jonah Harvey"),
                timeProvider,
                "Customer charged via standard charge.");
            jonah.AddContact(ContactType.Cell, new ContactDescription("1-392-598-4254"), timeProvider);
            jonah.ClearDomainEvents();

            var prospect = Customer.Create(
                new CustomerName("Alice Prospect"),
                timeProvider,
                "New prospect — needs follow-up.");
            prospect.AddContact(ContactType.Email, new ContactDescription("alice@example.com"), timeProvider);
            prospect.ClearDomainEvents();

            dbContext.Customer.AddRange(cherokee, jonah, prospect);
            await dbContext.SaveChangesAsync();
        }
    }
}
