//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Validators;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Tests for validation using FluentValidation.
/// </summary>
public class ValidationIntegrationTest
{
    private readonly ProductValidator _productValidator;
    private readonly CategoryValidator _categoryValidator;

    public ValidationIntegrationTest()
    {
        _productValidator = new ProductValidator();
        _categoryValidator = new CategoryValidator();
    }

    #region [ Product Validation ]

    [Fact]
    public void ValidProduct_ShouldPassValidation()
    {
        // Arrange
        var product = new Product
        {
            Name = "Valid Product",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1,
            IsActive = true
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Product_WithEmptyName_ShouldFailValidation()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Product_WithNameTooLong_ShouldFailValidation()
    {
        // Arrange
        var product = new Product
        {
            Name = new string('A', 201), // Exceeds 200 characters
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Product_WithEmptyDescription_ShouldFailValidation()
    {
        // Arrange
        var product = new Product
        {
            Name = "Valid Name",
            Description = "",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Description");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Product_WithInvalidPrice_ShouldFailValidation(decimal price)
    {
        // Arrange
        var product = new Product
        {
            Name = "Valid Name",
            Description = "Valid Description",
            Price = price,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void Product_WithNegativeStock_ShouldFailValidation()
    {
        // Arrange
        var product = new Product
        {
            Name = "Valid Name",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = -5,
            CategoryId = 1
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Product_WithInvalidCategoryId_ShouldFailValidation(int categoryId)
    {
        // Arrange
        var product = new Product
        {
            Name = "Valid Name",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = categoryId
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void Product_WithMultipleErrors_ShouldReportAllErrors()
    {
        // Arrange
        var product = new Product
        {
            Name = "", // Error 1
            Description = "", // Error 2
            Price = -10m, // Error 3
            StockQuantity = -5, // Error 4
            CategoryId = 0 // Error 5
        };

        // Act
        var result = _productValidator.Validate(product);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(5);
    }

    #endregion

    #region [ Category Validation ]

    [Fact]
    public void ValidCategory_ShouldPassValidation()
    {
        // Arrange
        var category = new Category
        {
            Name = "Valid Category",
            Description = "Valid Description",
            IsActive = true
        };

        // Act
        var result = _categoryValidator.Validate(category);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Category_WithNullDescription_ShouldPassValidation()
    {
        // Arrange
        var category = new Category
        {
            Name = "Valid Category",
            Description = null, // Null is allowed
            IsActive = true
        };

        // Act
        var result = _categoryValidator.Validate(category);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Category_WithEmptyName_ShouldFailValidation()
    {
        // Arrange
        var category = new Category
        {
            Name = "",
            Description = "Valid Description",
            IsActive = true
        };

        // Act
        var result = _categoryValidator.Validate(category);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Category_WithDescriptionTooLong_ShouldFailValidation()
    {
        // Arrange
        var category = new Category
        {
            Name = "Valid Name",
            Description = new string('A', 301), // Exceeds 300 characters
            IsActive = true
        };

        // Act
        var result = _categoryValidator.Validate(category);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    #endregion

    #region [ Validation Async ]

    [Fact]
    public async Task ValidateAsync_ValidProduct_ShouldPass()
    {
        // Arrange
        var product = new Product
        {
            Name = "Async Valid Product",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act
        var result = await _productValidator.ValidateAsync(product);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidProduct_ShouldFail()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act
        var result = await _productValidator.ValidateAsync(product);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region [ Validation and Exception ]

    [Fact]
    public void ValidateAndThrow_InvalidProduct_ShouldThrowException()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act & Assert
        var action = () => _productValidator.ValidateAndThrow(product);
        action.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateAndThrow_ValidProduct_ShouldNotThrow()
    {
        // Arrange
        var product = new Product
        {
            Name = "Valid Product",
            Description = "Valid Description",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = 1
        };

        // Act & Assert
        var action = () => _productValidator.ValidateAndThrow(product);
        action.Should().NotThrow();
    }

    #endregion
}

