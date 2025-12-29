//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Support.Data;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// Tests for bulk operations (BulkInsert, BulkUpdate, BulkDelete, ExecuteUpdate, ExecuteDelete)
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test7BulkOperations
    {
        private readonly IServiceProvider _serviceProvider;

        public Test7BulkOperations()
        {
            var services = new ServiceCollection();
            
            // Add logging (required by BulkOperationsRepositoryAsync)
            services.AddLogging();
            
            // Configure in-memory database for testing
            services.AddDbContext<DataContext>(options =>
            {
                options.UseInMemoryDatabase($"BulkOperationsTest_{Guid.NewGuid()}");
            });
            services.AddScoped<DbContext, DataContext>();
            
            // Add bulk operations repository
            services.AddMvp24HoursBulkOperationsRepositoryAsync();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        #region Bulk Insert Tests

        [Fact, Priority(1)]
        public async Task BulkInsert_ShouldInsertAllEntities()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
            
            var customers = GenerateCustomers(100);

            // Act
            var result = await repository.BulkInsertAsync(customers);
            await unitOfWork.SaveChangesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.RowsAffected > 0);
            Assert.True(result.ElapsedTime.TotalMilliseconds >= 0);
        }

        [Fact, Priority(2)]
        public async Task BulkInsert_WithOptions_ShouldReportProgress()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            
            var customers = GenerateCustomers(50);
            var progressReported = new List<(int processed, int total)>();

            var options = new BulkOperationOptions
            {
                BatchSize = 10,
                ProgressCallback = (processed, total) => progressReported.Add((processed, total))
            };

            // Act
            var result = await repository.BulkInsertAsync(customers, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(progressReported);
            Assert.Equal(50, progressReported.Last().total);
        }

        [Fact, Priority(3)]
        public async Task BulkInsert_WithEmptyList_ShouldReturnSuccessWithZeroRows()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            
            var customers = new List<Customer>();

            // Act
            var result = await repository.BulkInsertAsync(customers);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.RowsAffected);
        }

        #endregion

        #region Bulk Update Tests

        [Fact, Priority(10)]
        public async Task BulkUpdate_ShouldUpdateAllEntities()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
            
            // First insert some data
            var customers = GenerateCustomers(20);
            await repository.BulkInsertAsync(customers);
            await unitOfWork.SaveChangesAsync();

            // Get the inserted customers
            var insertedCustomers = await repository.ListAsync();
            
            // Modify them
            foreach (var customer in insertedCustomers)
            {
                customer.Name = $"Updated_{customer.Name}";
            }

            // Act
            var result = await repository.BulkUpdateAsync(insertedCustomers.ToList());
            await unitOfWork.SaveChangesAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact, Priority(11)]
        public async Task BulkUpdate_WithEmptyList_ShouldReturnSuccessWithZeroRows()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            
            var customers = new List<Customer>();

            // Act
            var result = await repository.BulkUpdateAsync(customers);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.RowsAffected);
        }

        #endregion

        #region Bulk Delete Tests

        [Fact, Priority(20)]
        public async Task BulkDelete_ShouldDeleteAllEntities()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
            
            // First insert some data
            var customers = GenerateCustomers(15);
            await repository.BulkInsertAsync(customers);
            await unitOfWork.SaveChangesAsync();

            // Get the inserted customers
            var insertedCustomers = await repository.ListAsync();

            // Act
            var result = await repository.BulkDeleteAsync(insertedCustomers.ToList());
            await unitOfWork.SaveChangesAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact, Priority(21)]
        public async Task BulkDelete_WithEmptyList_ShouldReturnSuccessWithZeroRows()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            
            var customers = new List<Customer>();

            // Act
            var result = await repository.BulkDeleteAsync(customers);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.RowsAffected);
        }

        #endregion

        #region Execute Update Tests (.NET 7+)

        // NOTE: ExecuteUpdate and ExecuteDelete are not supported by the InMemory provider.
        // These tests require a real database (SQL Server, PostgreSQL, etc.)
        // The tests are marked with [Trait("Category", "RequiresRealDatabase")]
        // and are skipped during CI when using InMemory.

        [Fact(Skip = "ExecuteUpdateAsync is not supported by InMemory provider"), Priority(30)]
        [Trait("Category", "RequiresRealDatabase")]
        public async Task ExecuteUpdate_SingleProperty_ShouldUpdateMatchingEntities()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
            
            // Insert test data with some active and some inactive
            var customers = GenerateCustomers(30);
            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].Active = i % 2 == 0; // Half active, half inactive
            }
            await repository.BulkInsertAsync(customers);
            await unitOfWork.SaveChangesAsync();

            // Act - Deactivate all active customers
            var rowsAffected = await repository.ExecuteUpdateAsync(
                c => c.Active == true,
                c => c.Active,
                false);

            // Assert
            Assert.True(rowsAffected >= 0);
        }

        #endregion

        #region Execute Delete Tests (.NET 7+)

        [Fact(Skip = "ExecuteDeleteAsync is not supported by InMemory provider"), Priority(40)]
        [Trait("Category", "RequiresRealDatabase")]
        public async Task ExecuteDelete_ShouldDeleteMatchingEntities()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
            
            // Insert test data
            var customers = GenerateCustomers(25);
            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].Active = i < 10; // First 10 active, rest inactive
            }
            await repository.BulkInsertAsync(customers);
            await unitOfWork.SaveChangesAsync();

            // Act - Delete all inactive customers
            var rowsAffected = await repository.ExecuteDeleteAsync(c => c.Active == false);

            // Assert
            Assert.True(rowsAffected >= 0);
        }

        [Fact(Skip = "ExecuteDeleteAsync is not supported by InMemory provider"), Priority(41)]
        [Trait("Category", "RequiresRealDatabase")]
        public async Task ExecuteDelete_WithNoMatches_ShouldReturnZero()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Customer>>();
            
            // Act - Try to delete with impossible condition
            var rowsAffected = await repository.ExecuteDeleteAsync(c => c.Name == "NonExistentName12345");

            // Assert
            Assert.Equal(0, rowsAffected);
        }

        #endregion

        #region DbContext Extension Tests

        [Fact, Priority(50)]
        public async Task DbContextExtension_BulkInsert_ShouldWork()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            
            var customers = GenerateCustomers(10);

            // Act
            var result = await dbContext.BulkInsertAsync(customers);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact(Skip = "ExecuteDeleteAsync is not supported by InMemory provider"), Priority(51)]
        [Trait("Category", "RequiresRealDatabase")]
        public async Task DbContextExtension_ExecuteDelete_ShouldWork()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            
            // Insert test data first
            var customers = GenerateCustomers(5);
            await dbContext.BulkInsertAsync(customers);
            await dbContext.SaveChangesAsync();

            // Act
            var rowsAffected = await dbContext.ExecuteDeleteAsync<Customer>(c => c.Name.StartsWith("Customer_"));

            // Assert
            Assert.True(rowsAffected >= 0);
        }

        #endregion

        #region BulkOperationResult Tests

        [Fact]
        public void BulkOperationResult_Success_ShouldCreateCorrectResult()
        {
            // Arrange
            var rowsAffected = 100;
            var elapsedTime = TimeSpan.FromMilliseconds(500);

            // Act
            var result = BulkOperationResult.Success(rowsAffected, elapsedTime);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(rowsAffected, result.RowsAffected);
            Assert.Equal(elapsedTime, result.ElapsedTime);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void BulkOperationResult_Failure_ShouldCreateCorrectResult()
        {
            // Arrange
            var errorMessage = "Test error";
            var elapsedTime = TimeSpan.FromMilliseconds(100);

            // Act
            var result = BulkOperationResult.Failure(errorMessage, elapsedTime);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0, result.RowsAffected);
            Assert.Equal(elapsedTime, result.ElapsedTime);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        #endregion

        #region BulkOperationOptions Tests

        [Fact]
        public void BulkOperationOptions_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var options = new BulkOperationOptions();

            // Assert
            Assert.Equal(1000, options.BatchSize);
            Assert.True(options.UseTransaction);
            Assert.Null(options.ProgressCallback);
            Assert.Equal(300, options.TimeoutSeconds);
            Assert.False(options.KeepIdentity);
            Assert.False(options.UseTempTable);
            Assert.True(options.BypassChangeTracking);
        }

        #endregion

        #region Helpers

        private static List<Customer> GenerateCustomers(int count)
        {
            var customers = new List<Customer>();
            for (int i = 0; i < count; i++)
            {
                customers.Add(new Customer
                {
                    Name = $"Customer_{i}_{Guid.NewGuid():N}",
                    Active = true
                });
            }
            return customers;
        }

        #endregion
    }
}

