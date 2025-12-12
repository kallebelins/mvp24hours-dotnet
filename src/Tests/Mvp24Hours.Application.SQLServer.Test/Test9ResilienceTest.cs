//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// Tests for EF Core resilience features.
    /// </summary>
    public class Test9ResilienceTest
    {
        #region EFCoreResilienceOptions Tests

        [Fact]
        public void EFCoreResilienceOptions_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new EFCoreResilienceOptions();

            // Assert
            Assert.True(options.EnableRetryOnFailure);
            Assert.Equal(6, options.MaxRetryCount);
            Assert.Equal(30, options.MaxRetryDelaySeconds);
            Assert.Equal(30, options.CommandTimeoutSeconds);
            Assert.True(options.EnableDbContextPooling);
            Assert.Equal(1024, options.PoolSize);
            Assert.False(options.EnableCircuitBreaker);
            Assert.Equal(5, options.CircuitBreakerFailureThreshold);
            Assert.Equal(30, options.CircuitBreakerDurationSeconds);
            Assert.True(options.LogRetryAttempts);
            Assert.False(options.LogPoolStatistics);
        }

        [Fact]
        public void EFCoreResilienceOptions_Production_ShouldReturnOptimizedSettings()
        {
            // Arrange & Act
            var options = EFCoreResilienceOptions.Production();

            // Assert
            Assert.True(options.EnableRetryOnFailure);
            Assert.Equal(6, options.MaxRetryCount);
            Assert.True(options.EnableDbContextPooling);
            Assert.True(options.LogRetryAttempts);
            Assert.False(options.LogPoolStatistics);
        }

        [Fact]
        public void EFCoreResilienceOptions_Development_ShouldReturnDebugFriendlySettings()
        {
            // Arrange & Act
            var options = EFCoreResilienceOptions.Development();

            // Assert
            Assert.True(options.EnableRetryOnFailure);
            Assert.Equal(3, options.MaxRetryCount);
            Assert.False(options.EnableDbContextPooling); // Easier debugging
            Assert.True(options.LogRetryAttempts);
            Assert.True(options.LogPoolStatistics);
        }

        [Fact]
        public void EFCoreResilienceOptions_AzureSql_ShouldReturnCloudOptimizedSettings()
        {
            // Arrange & Act
            var options = EFCoreResilienceOptions.AzureSql();

            // Assert
            Assert.True(options.EnableRetryOnFailure);
            Assert.Equal(10, options.MaxRetryCount); // More retries for cloud
            Assert.Equal(60, options.MaxRetryDelaySeconds);
            Assert.True(options.EnableCircuitBreaker);
            Assert.NotEmpty(options.AdditionalTransientErrorNumbers);
        }

        [Fact]
        public void EFCoreResilienceOptions_NoResilience_ShouldDisableAllFeatures()
        {
            // Arrange & Act
            var options = EFCoreResilienceOptions.NoResilience();

            // Assert
            Assert.False(options.EnableRetryOnFailure);
            Assert.False(options.EnableDbContextPooling);
            Assert.False(options.EnableCircuitBreaker);
            Assert.False(options.LogRetryAttempts);
        }

        [Fact]
        public void EFCoreResilienceOptions_GetReadTimeout_ShouldReturnReadOrDefault()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                CommandTimeoutSeconds = 30,
                ReadCommandTimeoutSeconds = 60
            };

            // Act & Assert
            Assert.Equal(60, options.GetReadTimeout());

            // When ReadCommandTimeoutSeconds is null
            options.ReadCommandTimeoutSeconds = null;
            Assert.Equal(30, options.GetReadTimeout());
        }

        [Fact]
        public void EFCoreResilienceOptions_GetWriteTimeout_ShouldReturnWriteOrDefault()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                CommandTimeoutSeconds = 30,
                WriteCommandTimeoutSeconds = 90
            };

            // Act & Assert
            Assert.Equal(90, options.GetWriteTimeout());

            // When WriteCommandTimeoutSeconds is null
            options.WriteCommandTimeoutSeconds = null;
            Assert.Equal(30, options.GetWriteTimeout());
        }

        #endregion

        #region Circuit Breaker Tests

        [Fact]
        public void CircuitBreaker_InitialState_ShouldBeClosed()
        {
            // Arrange
            var options = new EFCoreResilienceOptions { EnableCircuitBreaker = true };
            var circuitBreaker = new DbContextCircuitBreaker(options);

            // Act & Assert
            Assert.Equal(CircuitState.Closed, circuitBreaker.State);
            Assert.True(circuitBreaker.IsAllowingRequests);
        }

        [Fact]
        public void CircuitBreaker_AfterThresholdFailures_ShouldOpen()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 3
            };
            var circuitBreaker = new DbContextCircuitBreaker(options);

            // Act - Record failures up to threshold
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            Assert.Equal(CircuitState.Closed, circuitBreaker.State);

            circuitBreaker.RecordFailure(); // This should trip the circuit

            // Assert
            Assert.Equal(CircuitState.Open, circuitBreaker.State);
            Assert.False(circuitBreaker.IsAllowingRequests);
        }

        [Fact]
        public void CircuitBreaker_SuccessResetsFailureCount()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5
            };
            var circuitBreaker = new DbContextCircuitBreaker(options);

            // Act - Record some failures then a success
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            Assert.Equal(2, circuitBreaker.ConsecutiveFailures);

            circuitBreaker.RecordSuccess();

            // Assert
            Assert.Equal(0, circuitBreaker.ConsecutiveFailures);
            Assert.Equal(CircuitState.Closed, circuitBreaker.State);
        }

        [Fact]
        public void CircuitBreaker_WhenOpen_ShouldThrowException()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 1,
                CircuitBreakerDurationSeconds = 60
            };
            var circuitBreaker = new DbContextCircuitBreaker(options);

            // Act - Trip the circuit
            circuitBreaker.RecordFailure();

            // Assert
            var ex = Assert.Throws<CircuitBreakerOpenException>(() => circuitBreaker.EnsureCircuitClosed());
            Assert.True(ex.RetryAfter > TimeSpan.Zero);
        }

        [Fact]
        public void CircuitBreaker_Reset_ShouldCloseCircuit()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 1
            };
            var circuitBreaker = new DbContextCircuitBreaker(options);
            circuitBreaker.RecordFailure(); // Open the circuit

            // Act
            circuitBreaker.Reset();

            // Assert
            Assert.Equal(CircuitState.Closed, circuitBreaker.State);
            Assert.Equal(0, circuitBreaker.ConsecutiveFailures);
        }

        [Fact]
        public void CircuitBreaker_DisabledByDefault_ShouldNotThrow()
        {
            // Arrange - Circuit breaker disabled
            var options = new EFCoreResilienceOptions { EnableCircuitBreaker = false };
            var circuitBreaker = new DbContextCircuitBreaker(options);

            // Act - Record many failures
            for (int i = 0; i < 100; i++)
            {
                circuitBreaker.RecordFailure();
            }

            // Assert - Should not throw even after many failures because it's disabled
            circuitBreaker.EnsureCircuitClosed(); // Should not throw
        }

        [Fact]
        public void CircuitBreaker_Statistics_ShouldTrackCorrectly()
        {
            // Arrange
            var options = new EFCoreResilienceOptions
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 10
            };
            var circuitBreaker = new DbContextCircuitBreaker(options);

            // Act
            circuitBreaker.RecordSuccess();
            circuitBreaker.RecordSuccess();
            circuitBreaker.RecordFailure();

            // Assert
            Assert.Equal(2, circuitBreaker.TotalSuccessCount);
            Assert.Equal(1, circuitBreaker.TotalFailureCount);
        }

        #endregion

        #region Pool Statistics Tests

        [Fact]
        public void PoolStatistics_RecordHit_ShouldIncrementCounters()
        {
            // Arrange
            var stats = new DbContextPoolStatistics();

            // Act
            stats.RecordHit(TimeSpan.FromMilliseconds(10));
            stats.RecordHit(TimeSpan.FromMilliseconds(20));

            // Assert
            Assert.Equal(2, stats.PoolHits);
            Assert.Equal(0, stats.PoolMisses);
            Assert.Equal(2, stats.TotalRequests);
            Assert.Equal(2, stats.ActiveContexts);
            Assert.Equal(15, stats.AverageCheckoutTimeMs);
        }

        [Fact]
        public void PoolStatistics_RecordMiss_ShouldIncrementCounters()
        {
            // Arrange
            var stats = new DbContextPoolStatistics();

            // Act
            stats.RecordMiss();
            stats.RecordMiss();

            // Assert
            Assert.Equal(0, stats.PoolHits);
            Assert.Equal(2, stats.PoolMisses);
            Assert.Equal(2, stats.TotalRequests);
        }

        [Fact]
        public void PoolStatistics_RecordReturn_ShouldDecrementActiveContexts()
        {
            // Arrange
            var stats = new DbContextPoolStatistics();
            stats.RecordHit(TimeSpan.FromMilliseconds(10));
            stats.RecordHit(TimeSpan.FromMilliseconds(10));

            // Act
            stats.RecordReturn();

            // Assert
            Assert.Equal(1, stats.ActiveContexts);
        }

        [Fact]
        public void PoolStatistics_GetSnapshot_ShouldReturnImmutableCopy()
        {
            // Arrange
            var stats = new DbContextPoolStatistics();
            stats.RecordHit(TimeSpan.FromMilliseconds(10));

            // Act
            var snapshot = stats.GetSnapshot();
            stats.RecordHit(TimeSpan.FromMilliseconds(20)); // Modify after snapshot

            // Assert - Snapshot should not change
            Assert.Equal(1, snapshot.PoolHits);
            Assert.Equal(2, stats.PoolHits);
        }

        [Fact]
        public void PoolStatistics_Reset_ShouldClearAllCounters()
        {
            // Arrange
            var stats = new DbContextPoolStatistics();
            stats.RecordHit(TimeSpan.FromMilliseconds(10));
            stats.RecordMiss();

            // Act
            stats.Reset();

            // Assert
            Assert.Equal(0, stats.PoolHits);
            Assert.Equal(0, stats.PoolMisses);
            Assert.Equal(0, stats.TotalRequests);
            Assert.Equal(0, stats.ActiveContexts);
        }

        #endregion

        #region Dependency Injection Tests

        [Fact]
        public void DI_AddCircuitBreaker_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddMvp24HoursDbContextCircuitBreaker(options =>
            {
                options.EnableCircuitBreaker = true;
                options.CircuitBreakerFailureThreshold = 3;
            });

            var provider = services.BuildServiceProvider();

            // Assert
            var circuitBreaker = provider.GetService<DbContextCircuitBreaker>();
            Assert.NotNull(circuitBreaker);

            var options = provider.GetService<IOptions<EFCoreResilienceOptions>>();
            Assert.NotNull(options);
            Assert.True(options.Value.EnableCircuitBreaker);
            Assert.Equal(3, options.Value.CircuitBreakerFailureThreshold);
        }

        #endregion
    }
}

