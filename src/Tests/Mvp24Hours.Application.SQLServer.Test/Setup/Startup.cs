//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Support.Data;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.BasicLogs;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.Basics;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.Logs;
using Mvp24Hours.Application.SQLServer.Test.Support.Enums;
using Mvp24Hours.Application.SQLServer.Test.Support.Services;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
#if !InMemory 
using Microsoft.Extensions.Configuration;
#endif

namespace Mvp24Hours.Application.SQLServer.Test.Setup
{
    public static class Startup
    {
        public static IServiceProvider Initialize(bool canLoadData = true)
        {
            var serviceProvider = ConfigureServices;

            // ensure database
            var db = serviceProvider.GetService<DataContext>();
            db.Database?.EnsureCreated();

            // load data
            if (canLoadData)
            {
                LoadData(serviceProvider);
            }
            return serviceProvider;
        }

        public static IServiceProvider InitializeBasic(bool canLoadData = true)
        {
            var serviceProvider = ConfigureServices;

            // ensure database
            var db = serviceProvider.GetService<DataContext>();
            db.Database?.EnsureCreated();

            // load data
            if (canLoadData)
            {
                LoadDataBasic(serviceProvider);
            }
            return serviceProvider;
        }

        public static IServiceProvider InitializeLog(bool canLoadData = true)
        {
            var serviceProvider = ConfigureServices;

            // ensure database
            var db = serviceProvider.GetService<DataContext>();
            db.Database?.EnsureCreated();

            // load data
            if (canLoadData)
            {
                LoadDataLog(serviceProvider);
            }
            return serviceProvider;
        }

        public static IServiceProvider InitializeBasicLog(bool canLoadData = true)
        {
            var serviceProvider = ConfigureServices;

            // ensure database
            var db = serviceProvider.GetService<DataContext>();
            db.Database?.EnsureCreated();

            // load data
            if (canLoadData)
            {
                LoadDataBasicLog(serviceProvider);
            }
            return serviceProvider;
        }

        public static void Cleanup(IServiceProvider serviceProvider)
        {
            // ensure database drop
            var db = serviceProvider?.GetService<DataContext>();
            if (db != null)
            {
                db.Database.EnsureDeleted();
                db.Dispose();
            }
        }

        private static IServiceProvider ConfigureServices
        {
            get
            {
#if InMemory
                var services = new ServiceCollection();
                services.AddDbContext<DataContext>(options =>
                    options
                        .UseInMemoryDatabase(StringHelper.GenerateKey(10)));
#else
                var services = new ServiceCollection()
                    .AddSingleton(Helpers.ConfigurationHelper.AppSettings);

                services.AddDbContext<DataContext>(options =>
                    options
                        .UseSqlServer(Helpers.ConfigurationHelper.AppSettings.GetConnectionString("DataContext")
                            .Format(StringHelper.GenerateKey(10))));
#endif
                services.AddMvp24HoursDbContext<DataContext>();
                services.AddMvp24HoursRepository(options: options =>
                {
                    options.MaxQtyByQueryPage = 100;
                    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                });

                // register my services
                services.AddScoped<CustomerService, CustomerService>();
                services.AddScoped<ContactService, ContactService>();
                services.AddScoped<CustomerBasicService, CustomerBasicService>();
                services.AddScoped<CustomerLogService, CustomerLogService>();
                services.AddScoped<CustomerBasicLogService, CustomerBasicLogService>();
                services.AddScoped<CustomerPagingService, CustomerPagingService>();

                return services.BuildServiceProvider();
            }
        }

        private static void LoadData(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<CustomerService>();
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
            service.Add(customers);
        }

        private static void LoadDataBasic(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<CustomerBasicService>();
            List<CustomerBasic> customers = [];
            for (int i = 1; i <= 10; i++)
            {
                var customer = new CustomerBasic
                {
                    Name = $"Test {i}",
                    Active = true
                };
                customer.Contacts.Add(new ContactBasic
                {
                    Description = $"202-555-014{i}",
                    Type = ContactType.CellPhone,
                    Active = true
                });
                customer.Contacts.Add(new ContactBasic
                {
                    Description = $"test{i}@sample.com",
                    Type = ContactType.Email,
                    Active = true
                });
                customers.Add(customer);
            }
            service.Add(customers);
        }

        private static void LoadDataLog(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<CustomerLogService>();
            List<CustomerLog> customers = [];
            for (int i = 1; i <= 10; i++)
            {
                var customer = new CustomerLog
                {
                    Name = $"Test {i}",
                    Active = true
                };
                customer.Contacts.Add(new ContactLog
                {
                    Description = $"202-555-014{i}",
                    Type = ContactType.CellPhone,
                    Active = true
                });
                customer.Contacts.Add(new ContactLog
                {
                    Description = $"test{i}@sample.com",
                    Type = ContactType.Email,
                    Active = true
                });
                customers.Add(customer);
            }
            service.Add(customers);
        }

        private static void LoadDataBasicLog(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<CustomerBasicLogService>();
            List<CustomerBasicLog> customers = [];
            for (int i = 1; i <= 10; i++)
            {
                var customer = new CustomerBasicLog
                {
                    Name = $"Test {i}",
                    Active = true
                };
                customer.Contacts.Add(new ContactBasicLog
                {
                    Description = $"202-555-014{i}",
                    Type = ContactType.CellPhone,
                    Active = true
                });
                customer.Contacts.Add(new ContactBasicLog
                {
                    Description = $"test{i}@sample.com",
                    Type = ContactType.Email,
                    Active = true
                });
                customers.Add(customer);
            }
            service.Add(customers);
        }
    }
}
