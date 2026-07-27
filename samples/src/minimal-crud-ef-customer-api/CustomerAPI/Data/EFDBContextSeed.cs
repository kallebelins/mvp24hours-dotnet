using CustomerAPI.Entities;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;

namespace CustomerAPI.Data
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public static class EFDBContextSeed
    {
        public static void Seed(this EFDBContext dbContext)
        {
            // adicionar processamento de dados iniciais para carga
            TelemetryHelper.Execute(TelemetryLevels.Information, "efdbcontextseed-seedasync", $"Seed database associated with context {dbContext.GetType().Name}");

            if (!dbContext.Customer.Any())
            {
                dbContext.Customer.AddRange(GetCustomers());
                dbContext.SaveChanges();
            }
        }

        private static List<Customer> GetCustomers()
        {
            return
            [
                new Customer
                {
                    Created = DateTime.Now,
                    Name = "Cherokee Macdonald",
                    Active = true,
                    Note = "Customer charged via standard charge."
                },
                new Customer
                {
                    Created = DateTime.Now,
                    Name = "Jonah Harvey",
                    Active = true,
                    Note = "Customer charged via standard charge."
                }
            ];
        }
    }
}
