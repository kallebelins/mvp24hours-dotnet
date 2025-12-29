//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Options;
using Mvp24Hours.Core.Extensions.Options;
using Xunit;

namespace Mvp24Hours.Core.Test.Options;

/// <summary>
/// Tests for IOptions validation extensions and validators.
/// </summary>
public class OptionsValidationTest
{
    #region Test Options Classes

    public class TestOptions
    {
        [Required(ErrorMessage = "ConnectionString is required.")]
        public string? ConnectionString { get; set; }

        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int Port { get; set; } = 5432;

        [Range(1, 3600, ErrorMessage = "Timeout must be between 1 and 3600 seconds.")]
        public int TimeoutSeconds { get; set; } = 30;

        [EmailAddress(ErrorMessage = "NotificationEmail must be a valid email address.")]
        public string? NotificationEmail { get; set; }

        public bool EnableLogging { get; set; } = true;
    }

    public class TestOptionsValidator : OptionsValidatorBase<TestOptions>
    {
        protected override void ConfigureValidation(
            OptionsValidationContext<TestOptions> context,
            TestOptions options)
        {
            // Custom validation: if logging is enabled, notification email is required
            if (options.EnableLogging && string.IsNullOrEmpty(options.NotificationEmail))
            {
                context.AddPropertyError(nameof(options.NotificationEmail),
                    "NotificationEmail is required when logging is enabled.");
            }

            // Validate timeout is reasonable
            if (options.TimeoutSeconds > 300 && !options.EnableLogging)
            {
                context.AddError("Long timeouts (>300s) require logging to be enabled for debugging.");
            }
        }
    }

    public class SimpleTestOptions
    {
        public string? ApiKey { get; set; }
    }

    public class SimpleApiKeyValidator : SimpleOptionsValidatorBase<SimpleTestOptions>
    {
        protected override string FailureMessage => "ApiKey is required and must start with 'API_'.";

        protected override bool IsValid(SimpleTestOptions options)
            => !string.IsNullOrEmpty(options.ApiKey) && options.ApiKey.StartsWith("API_");
    }

    #endregion

    #region OptionsValidationResult Tests

    [Fact]
    public void OptionsValidationResult_Success_ReturnsSucceededTrue()
    {
        // Arrange & Act
        var result = OptionsValidationResult.Success();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Failures);
        Assert.Null(result.FailureMessage);
    }

    [Fact]
    public void OptionsValidationResult_FailSingleMessage_ReturnsSucceededFalse()
    {
        // Arrange
        var errorMessage = "Connection failed.";

        // Act
        var result = OptionsValidationResult.Fail(errorMessage);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Failures);
        Assert.Equal(errorMessage, result.Failures[0]);
        Assert.Equal(errorMessage, result.FailureMessage);
    }

    [Fact]
    public void OptionsValidationResult_FailMultipleMessages_CombinesMessages()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = OptionsValidationResult.Fail(errors);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(3, result.Failures.Count);
        Assert.Equal("Error 1; Error 2; Error 3", result.FailureMessage);
    }

    #endregion

    #region OptionsValidatorBase Tests

    [Fact]
    public void OptionsValidatorBase_ValidOptions_ReturnsSuccess()
    {
        // Arrange
        var validator = new TestOptionsValidator();
        var options = new TestOptions
        {
            ConnectionString = "Host=localhost;Database=test",
            Port = 5432,
            TimeoutSeconds = 30,
            NotificationEmail = "admin@test.com",
            EnableLogging = true
        };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void OptionsValidatorBase_MissingConnectionString_ReturnsFailure()
    {
        // Arrange
        var validator = new TestOptionsValidator();
        var options = new TestOptions
        {
            ConnectionString = null, // Required field
            Port = 5432,
            NotificationEmail = "admin@test.com"
        };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.Contains("ConnectionString"));
    }

    [Fact]
    public void OptionsValidatorBase_InvalidPort_ReturnsFailure()
    {
        // Arrange
        var validator = new TestOptionsValidator();
        var options = new TestOptions
        {
            ConnectionString = "Host=localhost",
            Port = 99999, // Invalid port
            NotificationEmail = "admin@test.com"
        };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.Contains("Port"));
    }

    [Fact]
    public void OptionsValidatorBase_CustomValidation_EnforcesBusinessRules()
    {
        // Arrange
        var validator = new TestOptionsValidator();
        var options = new TestOptions
        {
            ConnectionString = "Host=localhost",
            Port = 5432,
            EnableLogging = true,
            NotificationEmail = null // Required when logging is enabled
        };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.Contains("NotificationEmail"));
    }

    [Fact]
    public void OptionsValidatorBase_NullOptions_ReturnsFailure()
    {
        // Arrange
        var validator = new TestOptionsValidator();

        // Act
        var result = validator.Validate(null!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.Contains("cannot be null"));
    }

    #endregion

    #region SimpleOptionsValidatorBase Tests

    [Fact]
    public void SimpleOptionsValidator_ValidApiKey_ReturnsSuccess()
    {
        // Arrange
        var validator = new SimpleApiKeyValidator();
        var options = new SimpleTestOptions { ApiKey = "API_12345" };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void SimpleOptionsValidator_InvalidApiKey_ReturnsFailure()
    {
        // Arrange
        var validator = new SimpleApiKeyValidator();
        var options = new SimpleTestOptions { ApiKey = "INVALID_KEY" };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.Contains("API_"));
    }

    [Fact]
    public void SimpleOptionsValidator_NullApiKey_ReturnsFailure()
    {
        // Arrange
        var validator = new SimpleApiKeyValidator();
        var options = new SimpleTestOptions { ApiKey = null };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.Succeeded);
    }

    #endregion

    #region DelegateOptionsValidator Tests

    [Fact]
    public void DelegateOptionsValidator_Create_ValidatesCorrectly()
    {
        // Arrange
        var validator = DelegateOptionsValidator<SimpleTestOptions>.Create(
            opts => !string.IsNullOrEmpty(opts.ApiKey),
            "ApiKey is required.");

        // Act
        var validResult = validator.Validate(new SimpleTestOptions { ApiKey = "test" });
        var invalidResult = validator.Validate(new SimpleTestOptions { ApiKey = null });

        // Assert
        Assert.True(validResult.Succeeded);
        Assert.False(invalidResult.Succeeded);
    }

    #endregion

    #region CompositeOptionsValidator Tests

    [Fact]
    public void CompositeOptionsValidator_CombinesMultipleValidators()
    {
        // Arrange
        var validator1 = DelegateOptionsValidator<SimpleTestOptions>.Create(
            opts => !string.IsNullOrEmpty(opts.ApiKey),
            "ApiKey is required.");

        var validator2 = DelegateOptionsValidator<SimpleTestOptions>.Create(
            opts => opts.ApiKey == null || opts.ApiKey.Length >= 5,
            "ApiKey must be at least 5 characters.");

        var composite = new CompositeOptionsValidator<SimpleTestOptions>(new[] { validator1, validator2 });

        // Act
        var result = composite.Validate(new SimpleTestOptions { ApiKey = "ab" }); // Too short

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.Contains("5 characters"));
    }

    #endregion

    #region DI Extension Tests

    [Fact]
    public void AddOptionsWithValidation_RegistersAndValidates()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:ConnectionString"] = "Host=localhost",
                ["Test:Port"] = "5432",
                ["Test:TimeoutSeconds"] = "30",
                ["Test:NotificationEmail"] = "admin@test.com"
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddOptionsWithValidation<TestOptions>(
            configuration.GetSection("Test"),
            validateOnStart: false); // Don't validate on start for this test

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TestOptions>>();

        // Assert
        Assert.NotNull(options.Value);
        Assert.Equal("Host=localhost", options.Value.ConnectionString);
        Assert.Equal(5432, options.Value.Port);
    }

    [Fact]
    public void AddOptionsWithValidation_WithCustomValidator_RegistersValidator()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:ConnectionString"] = "Host=localhost",
                ["Test:NotificationEmail"] = "admin@test.com"
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddOptionsWithValidation<TestOptions, TestOptionsValidator>(
            configuration.GetSection("Test"),
            validateOnStart: false);

        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<TestOptions>>();

        // Assert
        Assert.NotEmpty(validators);
    }

    [Fact]
    public void AddOptionsValidator_RegistersValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleTestOptions>();

        // Act
        services.AddOptionsValidator<SimpleTestOptions, SimpleApiKeyValidator>();

        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<SimpleTestOptions>>();

        // Assert
        Assert.NotEmpty(validators);
    }

    [Fact]
    public void ValidateWithDataAnnotations_ValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new TestOptions
        {
            ConnectionString = "Host=localhost",
            Port = 5432,
            TimeoutSeconds = 30
        };

        // Act
        var result = OptionsValidationExtensions.ValidateWithDataAnnotations(options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithDataAnnotations_InvalidOptions_ReturnsFailure()
    {
        // Arrange
        var options = new TestOptions
        {
            ConnectionString = null, // Required
            Port = 99999, // Out of range
            TimeoutSeconds = 0 // Out of range
        };

        // Act
        var result = OptionsValidationExtensions.ValidateWithDataAnnotations(options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Failures);
    }

    #endregion

    #region IOptionsMonitor Tests

    [Fact]
    public void AddOptionsForMonitor_RegistersOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:ConnectionString"] = "Host=localhost"
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddOptionsForMonitor<TestOptions>(configuration.GetSection("Test"));

        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetService<IOptionsMonitor<TestOptions>>();

        // Assert
        Assert.NotNull(optionsMonitor);
        Assert.Equal("Host=localhost", optionsMonitor.CurrentValue.ConnectionString);
    }

    #endregion

    #region IOptionsSnapshot Tests

    [Fact]
    public void AddOptionsForSnapshot_RegistersOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:ConnectionString"] = "Host=localhost"
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddOptionsForSnapshot<TestOptions>(configuration.GetSection("Test"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var optionsSnapshot = scope.ServiceProvider.GetService<IOptionsSnapshot<TestOptions>>();

        // Assert
        Assert.NotNull(optionsSnapshot);
        Assert.Equal("Host=localhost", optionsSnapshot.Value.ConnectionString);
    }

    #endregion
}

