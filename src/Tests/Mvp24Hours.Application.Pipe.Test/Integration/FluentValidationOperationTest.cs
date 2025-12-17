//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Mvp24Hours.Infrastructure.Pipe.Integration.FluentValidation;
using System.Threading.Tasks;
using Xunit;

namespace Mvp24Hours.Application.Pipe.Test.Integration
{
    public class FluentValidationOperationTest
    {
        [Fact]
        public async Task FluentValidationOperation_ReturnsSuccess_WhenValid()
        {
            // Arrange
            var validator = new TestOrderValidator();
            var operation = new FluentValidationOperation<TestOrder>(new[] { validator });

            var order = new TestOrder
            {
                CustomerName = "John Doe",
                Amount = 100.00m
            };

            // Act
            var result = await operation.ExecuteAsync(order);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(order, result.Value);
        }

        [Fact]
        public async Task FluentValidationOperation_ReturnsFailure_WhenInvalid()
        {
            // Arrange
            var validator = new TestOrderValidator();
            var operation = new FluentValidationOperation<TestOrder>(new[] { validator });

            var order = new TestOrder
            {
                CustomerName = "", // Invalid - empty
                Amount = -10 // Invalid - negative
            };

            // Act
            var result = await operation.ExecuteAsync(order);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.Messages);
        }

        [Fact]
        public async Task FluentValidationOperation_ReturnsSuccess_WhenNoValidators()
        {
            // Arrange
            var operation = new FluentValidationOperation<TestOrder>(new IValidator<TestOrder>[0]);

            var order = new TestOrder
            {
                CustomerName = "", // Would be invalid if validators existed
                Amount = -10
            };

            // Act
            var result = await operation.ExecuteAsync(order);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task FluentValidationOperation_ThrowsException_WhenConfigured()
        {
            // Arrange
            var validator = new TestOrderValidator();
            var options = new FluentValidationOptions
            {
                ThrowValidationException = true
            };
            var operation = new FluentValidationOperation<TestOrder>(new[] { validator }, options: options);

            var order = new TestOrder
            {
                CustomerName = "", // Invalid
                Amount = 100
            };

            // Act & Assert
            await Assert.ThrowsAsync<Mvp24Hours.Core.Exceptions.ValidationException>(
                () => operation.ExecuteAsync(order));
        }

        [Fact]
        public async Task FluentValidationOperation_MultipleValidators_AllRun()
        {
            // Arrange
            var validator1 = new TestOrderValidator();
            var validator2 = new TestOrderAmountValidator();
            var operation = new FluentValidationOperation<TestOrder>(new IValidator<TestOrder>[] { validator1, validator2 });

            var order = new TestOrder
            {
                CustomerName = "John",
                Amount = 50000 // Too high for validator2
            };

            // Act
            var result = await operation.ExecuteAsync(order);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.Messages);
        }

        public class TestOrder
        {
            public string CustomerName { get; set; } = "";
            public decimal Amount { get; set; }
        }

        public class TestOrderValidator : AbstractValidator<TestOrder>
        {
            public TestOrderValidator()
            {
                RuleFor(x => x.CustomerName)
                    .NotEmpty()
                    .WithMessage("Customer name is required");

                RuleFor(x => x.Amount)
                    .GreaterThan(0)
                    .WithMessage("Amount must be positive");
            }
        }

        public class TestOrderAmountValidator : AbstractValidator<TestOrder>
        {
            public TestOrderAmountValidator()
            {
                RuleFor(x => x.Amount)
                    .LessThanOrEqualTo(10000)
                    .WithMessage("Amount cannot exceed 10,000");
            }
        }
    }
}

