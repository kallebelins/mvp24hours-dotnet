//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Support.Data;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Enums;
using Mvp24Hours.Application.SQLServer.Test.Support.Services.Async;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Extensions;

#if !InMemory 
using Microsoft.Extensions.Configuration;
using Mvp24Hours.Helpers;
#endif

namespace Mvp24Hours.Application.SQLServer.Test.Setup
{
    public static class StartupAsync
    {
        public static IServiceProvider Initialize(bool canLoadData = true)
        {
            ServiceProvider serviceProvider = ConfigureServicesAsync();

            // ensure database
            DataContext? db = serviceProvider.GetRequiredService<DataContext>();
            db.Database?.EnsureCreated();

            // load data
            if (canLoadData)
            {
                LoadDataAsync(serviceProvider).Wait();
            }
            return serviceProvider;
        }

        public static void Cleanup(IServiceProvider serviceProvider)
        {
            // ensure database drop
            DataContext? db = serviceProvider?.GetRequiredService<DataContext>();
            if (db != null)
            {
                db.Database.EnsureDeleted();
                db.Dispose();
            }
        }

        private static ServiceProvider ConfigureServicesAsync()
        {
#if InMemory
            var services = new ServiceCollection();
            services.AddDbContext<DataContext>(options =>
                options
                    .UseInMemoryDatabase(StringHelper.GenerateKey(10)));
#else
            var services = new ServiceCollection()
                .AddSingleton(ConfigurationHelper.AppSettings);

            services.AddDbContext<DataContext>(options =>
                options
                    .UseSqlServer((ConfigurationHelper.AppSettings.GetConnectionString("DataContext")
                        ?? throw new InvalidOperationException("Connection string 'DataContext' not found."))
                        .Format(StringHelper.GenerateKey(10))));
#endif

            services.AddMvp24HoursDbContext<DataContext>();
            services.AddMvp24HoursRepositoryAsync(options: options =>
            {
                options.MaxQtyByQueryPage = 100;
            });

            // register my services
            services.AddScoped<CustomerServiceAsync, CustomerServiceAsync>();
            services.AddScoped<ContactServiceAsync, ContactServiceAsync>();
            services.AddScoped<CustomerPagingServiceAsync, CustomerPagingServiceAsync>();

            return services.BuildServiceProvider();
        }

        private static async Task LoadDataAsync(IServiceProvider serviceProvider)
        {
            CustomerServiceAsync? service = serviceProvider.GetRequiredService<CustomerServiceAsync>();
            List<Customer> customers = [];
            for (int i = 1; i <= 10; i++)
            {
                var customer = new Customer
                {
                    Name = $"Test {i}",
                    Active = true
                };
                customer.Contacts.Add(new Contact
                {
                    Description = $"202-555-014{i}",
                    Type = ContactType.CellPhone,
                    Active = true
                });
                customer.Contacts.Add(new Contact
                {
                    Description = $"test{i}@sample.com",
                    Type = ContactType.Email,
                    Active = true
                });
                customers.Add(customer);
            }
            await service.AddAsync(customers);
        }
    }
}
