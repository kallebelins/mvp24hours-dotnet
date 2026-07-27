using CustomerAPI.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;

namespace CustomerAPI.Data
{
    /// <summary>
    /// 
    /// </summary>
    public static class MongoDbContextSeed
    {
        public static void Seed(this IUnitOfWork uoW)
        {
            // adicionar processamento de dados iniciais para carga
            TelemetryHelper.Execute(TelemetryLevels.Information, "efdbcontextseed-seedasync", $"Seed database associated with context");

            IRepository<Customer> repository = uoW.GetRepository<Customer>();
            if (repository.ListAny())
            {
                repository.Add(GetCustomers());
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
