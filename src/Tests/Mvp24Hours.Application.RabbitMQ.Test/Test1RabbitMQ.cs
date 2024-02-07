//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.RabbitMQ.Test
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test1RabbitMQ : IAsyncLifetime
    {
        #region [ Container ]
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithExposedPort(5672)
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();

        public async Task InitializeAsync()
            => await _rabbitMqContainer.StartAsync().ConfigureAwait(false);

        public async Task DisposeAsync()
            => await _rabbitMqContainer.DisposeAsync().ConfigureAwait(false);
        #endregion

        #region [ Fields ]
        private IServiceProvider serviceProvider;
        #endregion

        #region [ Configure ]
        public Test1RabbitMQ() { }

        private void SetupTypeAssembly()
        {
            var services = new ServiceCollection();

            services.AddScoped<CustomerConsumer, CustomerConsumer>();
            services.AddScoped<CustomerWithCtorConsumer, CustomerWithCtorConsumer>();
            services.AddTransient(x => new CustomerEvent() { Name = "event" });

            services.AddMvp24HoursRabbitMQ(
                typeof(CustomerConsumer).Assembly,
                connectionOptions =>
                {
                    connectionOptions.ConnectionString = _rabbitMqContainer.GetConnectionString();
                    connectionOptions.DispatchConsumersAsync = true;
                    connectionOptions.RetryCount = 3;
                },
                clientOptions =>
                {
                    clientOptions.MaxRedeliveredCount = 1;
                }
            );
            serviceProvider = services.BuildServiceProvider();
        }

        private void SetupTypeAssemblyWithoutInjection()
        {
            var services = new ServiceCollection();

            services.AddMvp24HoursRabbitMQ(
                new List<Type> { typeof(CustomerConsumer) },
                connectionOptions =>
                {
                    connectionOptions.ConnectionString = _rabbitMqContainer.GetConnectionString();
                    connectionOptions.DispatchConsumersAsync = true;
                    connectionOptions.RetryCount = 3;
                },
                clientOptions =>
                {
                    clientOptions.MaxRedeliveredCount = 1;
                }
            );
            serviceProvider = services.BuildServiceProvider();
        }

        private void SetupTypeDefined()
        {
            var services = new ServiceCollection();

            services.AddScoped<CustomerWithCtorConsumer, CustomerWithCtorConsumer>();
            services.AddTransient(x => new CustomerEvent() { Name = "event" });

            services.AddMvp24HoursRabbitMQ(
                new List<Type> { typeof(CustomerWithCtorConsumer) },
                connectionOptions =>
                {
                    connectionOptions.ConnectionString = _rabbitMqContainer.GetConnectionString();
                    connectionOptions.DispatchConsumersAsync = true;
                    connectionOptions.RetryCount = 3;
                },
                clientOptions =>
                {
                    clientOptions.MaxRedeliveredCount = 1;
                }
            );
            serviceProvider = services.BuildServiceProvider();
        }

        private void SetupTypeDefinedList()
        {
            var services = new ServiceCollection();

            services.AddScoped<CustomerConsumer, CustomerConsumer>();
            services.AddScoped<CustomerWithCtorConsumer, CustomerWithCtorConsumer>();
            services.AddTransient(x => new CustomerEvent() { Name = "event" });

            var consumers = typeof(Test1RabbitMQ).Assembly
                    .GetExportedTypes()
                    .Where(t => t.InheritsOrImplements(typeof(IMvpRabbitMQConsumer)))
                    .ToArray();

            services.AddMvp24HoursRabbitMQ(
                consumers,
                connectionOptions =>
                {
                    connectionOptions.ConnectionString = _rabbitMqContainer.GetConnectionString();
                    connectionOptions.DispatchConsumersAsync = true;
                    connectionOptions.RetryCount = 3;
                },
                clientOptions =>
                {
                    clientOptions.MaxRedeliveredCount = 1;
                }
            );
            serviceProvider = services.BuildServiceProvider();
        }
        #endregion

        [Fact]
        public void CreateProducerAssembly()
        {
            SetupTypeAssembly();
            // arrange
            var client = serviceProvider.GetService<MvpRabbitMQClient>();

            // act
            string result = client.Publish(new CustomerEvent
            {
                Id = 1,
                Name = "Test 1",
                Active = true
            }, typeof(CustomerEvent).Name);

            // assert
            Assert.True(result.HasValue());
        }

        [Fact]
        public void CreateConsumerAssembly()
        {
            SetupTypeAssembly();
            var client = serviceProvider.GetService<MvpRabbitMQClient>();

            // arrange
            client.Publish(new CustomerEvent
            {
                Id = 2,
                Name = "Test 2",
                Active = true
            }, typeof(CustomerEvent).Name);


            // act
            client.Consume();

            // assert
            Assert.True(true);
        }

        [Fact]
        public void CreateConsumerWithoutInjection()
        {
            SetupTypeAssemblyWithoutInjection();
            var client = serviceProvider.GetService<MvpRabbitMQClient>();

            // arrange
            client.Publish(new CustomerEvent
            {
                Id = 2,
                Name = "Test 2",
                Active = true
            }, typeof(CustomerEvent).Name);


            // act
            client.Consume();

            // assert
            Assert.True(true);
        }

        [Fact]
        public void CreateProducerDefined()
        {
            SetupTypeDefined();
            // arrange
            var client = serviceProvider.GetService<MvpRabbitMQClient>();

            // act
            string result = client.Publish(new CustomerEvent
            {
                Id = 1,
                Name = "Test 1",
                Active = true
            }, typeof(CustomerEvent).Name);

            // assert
            Assert.True(result.HasValue());
        }

        [Fact]
        public void CreateConsumerDefined()
        {
            SetupTypeDefined();
            var client = serviceProvider.GetService<MvpRabbitMQClient>();

            // arrange
            client.Publish(new CustomerEvent
            {
                Id = 2,
                Name = "Test 2",
                Active = true
            }, typeof(CustomerEvent).Name);


            // act
            client.Consume();

            // assert
            Assert.True(true);
        }

        [Fact]
        public void CreateConsumerDefinedList()
        {
            SetupTypeDefinedList();
            var client = serviceProvider.GetService<MvpRabbitMQClient>();

            // arrange
            client.Publish(new CustomerEvent
            {
                Id = 2,
                Name = "Test 2",
                Active = true
            }, typeof(CustomerEvent).Name);


            // act
            client.Consume();

            // assert
            Assert.True(true);
        }
    }
}
