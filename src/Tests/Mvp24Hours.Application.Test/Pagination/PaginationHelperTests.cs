//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Core.ValueObjects.Logic;
using Xunit;

namespace Mvp24Hours.Application.Test.Pagination
{
    /// <summary>
    /// Unit tests for PaginationHelper functionality.
    /// </summary>
    public class PaginationHelperTests
    {
        #region [ CalculateTotalPages Tests ]

        [Theory]
        [InlineData(100, 10, 10)]   // Exact division
        [InlineData(101, 10, 11)]   // One extra item
        [InlineData(99, 10, 10)]    // Rounded up
        [InlineData(1, 10, 1)]      // Single item
        [InlineData(0, 10, 0)]      // No items
        [InlineData(50, 25, 2)]     // Different page size
        [InlineData(100, 0, 0)]     // Zero page size (edge case)
        public void CalculateTotalPages_ShouldReturnCorrectValue(
            int totalCount, int pageSize, int expectedPages)
        {
            // Act
            var result = PaginationHelper.CalculateTotalPages(totalCount, pageSize);

            // Assert
            result.Should().Be(expectedPages);
        }

        #endregion

        #region [ CalculateOffset Tests ]

        [Theory]
        [InlineData(1, 10, 0)]    // First page
        [InlineData(2, 10, 10)]   // Second page
        [InlineData(5, 10, 40)]   // Fifth page
        [InlineData(1, 25, 0)]    // Different page size
        [InlineData(3, 25, 50)]   // Third page with different size
        public void CalculateOffset_ShouldReturnCorrectValue(
            int page, int pageSize, int expectedOffset)
        {
            // Act
            var result = PaginationHelper.CalculateOffset(page, pageSize);

            // Assert
            result.Should().Be(expectedOffset);
        }

        [Fact]
        public void CalculateOffset_WithZeroPage_ShouldReturnZero()
        {
            // Act
            var result = PaginationHelper.CalculateOffset(0, 10);

            // Assert
            result.Should().Be(0); // Normalized to page 1
        }

        #endregion

        #region [ OffsetToPage Tests ]

        [Theory]
        [InlineData(0, 10, 1)]     // First page
        [InlineData(10, 10, 2)]    // Second page
        [InlineData(90, 10, 10)]   // Last page
        [InlineData(0, 25, 1)]     // Different page size
        [InlineData(25, 25, 2)]    // Second page with different size
        public void OffsetToPage_ShouldReturnCorrectValue(
            int offset, int limit, int expectedPage)
        {
            // Act
            var result = PaginationHelper.OffsetToPage(offset, limit);

            // Assert
            result.Should().Be(expectedPage);
        }

        [Fact]
        public void OffsetToPage_WithZeroLimit_ShouldReturnOne()
        {
            // Act
            var result = PaginationHelper.OffsetToPage(50, 0);

            // Assert
            result.Should().Be(1);
        }

        #endregion

        #region [ NormalizePage Tests ]

        [Theory]
        [InlineData(1, 10, 1)]    // Valid first page
        [InlineData(10, 10, 10)] // Valid last page
        [InlineData(0, 10, 1)]   // Zero normalized to 1
        [InlineData(-5, 10, 1)]  // Negative normalized to 1
        [InlineData(15, 10, 10)] // Over max normalized to last
        public void NormalizePage_ShouldReturnValidPage(
            int page, int totalPages, int expectedPage)
        {
            // Act
            var result = PaginationHelper.NormalizePage(page, totalPages);

            // Assert
            result.Should().Be(expectedPage);
        }

        [Fact]
        public void NormalizePage_WithZeroTotalPages_ShouldReturnOne()
        {
            // Act
            var result = PaginationHelper.NormalizePage(5, 0);

            // Assert
            result.Should().Be(1);
        }

        #endregion

        #region [ NormalizePageSize Tests ]

        [Theory]
        [InlineData(10, 100, 10)]   // Valid page size
        [InlineData(0, 100, 1)]    // Zero normalized to min
        [InlineData(-5, 100, 1)]   // Negative normalized to min
        [InlineData(200, 100, 100)] // Over max normalized to max
        [InlineData(50, 50, 50)]   // Custom max
        public void NormalizePageSize_ShouldReturnValidSize(
            int pageSize, int maxPageSize, int expectedSize)
        {
            // Act
            var result = PaginationHelper.NormalizePageSize(pageSize, maxPageSize);

            // Assert
            result.Should().Be(expectedSize);
        }

        #endregion

        #region [ Index Calculation Tests ]

        [Theory]
        [InlineData(1, 10, 100, 1)]   // First page start
        [InlineData(2, 10, 100, 11)]  // Second page start
        [InlineData(10, 10, 100, 91)] // Last page start
        public void GetStartIndex_ShouldReturnCorrectValue(
            int page, int pageSize, int totalCount, int expectedStart)
        {
            // Act
            var result = PaginationHelper.GetStartIndex(page, pageSize, totalCount);

            // Assert
            result.Should().Be(expectedStart);
        }

        [Fact]
        public void GetStartIndex_WithZeroTotal_ShouldReturnZero()
        {
            // Act
            var result = PaginationHelper.GetStartIndex(1, 10, 0);

            // Assert
            result.Should().Be(0);
        }

        [Theory]
        [InlineData(1, 10, 100, 10)]   // First page end
        [InlineData(10, 10, 100, 100)] // Last page end (full)
        [InlineData(10, 10, 95, 95)]   // Last page end (partial)
        public void GetEndIndex_ShouldReturnCorrectValue(
            int page, int pageSize, int totalCount, int expectedEnd)
        {
            // Act
            var result = PaginationHelper.GetEndIndex(page, pageSize, totalCount);

            // Assert
            result.Should().Be(expectedEnd);
        }

        #endregion

        #region [ FormatRange Tests ]

        [Fact]
        public void FormatRange_WithDefaultFormat_ShouldFormatCorrectly()
        {
            // Act
            var result = PaginationHelper.FormatRange(2, 10, 100);

            // Assert
            result.Should().Be("Showing 11-20 of 100");
        }

        [Fact]
        public void FormatRange_WithCustomFormat_ShouldFormatCorrectly()
        {
            // Act
            var result = PaginationHelper.FormatRange(1, 10, 50, "{0} to {1} (total: {2})");

            // Assert
            result.Should().Be("1 to 10 (total: 50)");
        }

        #endregion

        #region [ Cursor Encoding Tests ]

        [Fact]
        public void EncodeCursor_ShouldCreateBase64String()
        {
            // Arrange
            var cursorValue = new { Id = 123, Date = "2024-01-01" };

            // Act
            var encoded = PaginationHelper.EncodeCursor(cursorValue);

            // Assert
            encoded.Should().NotBeNullOrEmpty();
            // Verify it's valid Base64
            Action decode = () => Convert.FromBase64String(encoded);
            decode.Should().NotThrow();
        }

        [Fact]
        public void DecodeCursor_ShouldRecoverOriginalValue()
        {
            // Arrange
            var original = 12345;
            var encoded = PaginationHelper.EncodeCursor(original);

            // Act
            var decoded = PaginationHelper.DecodeCursor<int>(encoded);

            // Assert
            decoded.Should().Be(original);
        }

        [Fact]
        public void DecodeCursor_WithNullString_ShouldReturnDefault()
        {
            // Act
            var result = PaginationHelper.DecodeCursor<int>(null);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void EncodeCompositeCursor_ShouldEncodeMultipleFields()
        {
            // Act
            var encoded = PaginationHelper.EncodeCompositeCursor(
                ("Id", 123),
                ("Name", "Test"));

            // Assert
            encoded.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region [ GeneratePageNumbers Tests ]

        [Fact]
        public void GeneratePageNumbers_ForMiddlePage_ShouldIncludeWindowAndEdges()
        {
            // Act
            var pages = PaginationHelper.GeneratePageNumbers(5, 10, 2);

            // Assert
            // Expected: [1, -1, 3, 4, 5, 6, 7, -1, 10]
            pages.Should().Contain(1);       // First page
            pages.Should().Contain(5);       // Current page
            pages.Should().Contain(10);      // Last page
            pages.Should().Contain(3);       // Window start
            pages.Should().Contain(7);       // Window end
            pages.Should().Contain(-1);      // Ellipsis
        }

        [Fact]
        public void GeneratePageNumbers_ForFirstPage_ShouldNotHavePreviousEllipsis()
        {
            // Act
            var pages = PaginationHelper.GeneratePageNumbers(1, 10, 2);

            // Assert
            pages[0].Should().Be(1);  // No ellipsis before first
        }

        [Fact]
        public void GeneratePageNumbers_ForLastPage_ShouldNotHaveNextEllipsis()
        {
            // Act
            var pages = PaginationHelper.GeneratePageNumbers(10, 10, 2);

            // Assert
            pages[^1].Should().Be(10);  // No ellipsis after last
        }

        [Fact]
        public void GeneratePageNumbers_WithZeroPages_ShouldReturnEmpty()
        {
            // Act
            var pages = PaginationHelper.GeneratePageNumbers(1, 0, 2);

            // Assert
            pages.Should().BeEmpty();
        }

        #endregion

        #region [ Validation Tests ]

        [Fact]
        public void ValidateParameters_WithValidValues_ShouldNotThrow()
        {
            // Act & Assert
            Action act = () => PaginationHelper.ValidateParameters(1, 20, 100);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateParameters_WithZeroPage_ShouldThrow()
        {
            // Act & Assert
            Action act = () => PaginationHelper.ValidateParameters(0, 20, 100);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ValidateParameters_WithZeroPageSize_ShouldThrow()
        {
            // Act & Assert
            Action act = () => PaginationHelper.ValidateParameters(1, 0, 100);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ValidateParameters_WithPageSizeOverMax_ShouldThrow()
        {
            // Act & Assert
            Action act = () => PaginationHelper.ValidateParameters(1, 200, 100);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TryValidateParameters_WithValidValues_ShouldReturnTrue()
        {
            // Act
            var isValid = PaginationHelper.TryValidateParameters(1, 20, 100, out var error);

            // Assert
            isValid.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public void TryValidateParameters_WithInvalidValues_ShouldReturnFalseWithMessage()
        {
            // Act
            var isValid = PaginationHelper.TryValidateParameters(0, 20, 100, out var error);

            // Assert
            isValid.Should().BeFalse();
            error.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region [ FromCriteria Tests ]

        [Fact]
        public void FromCriteria_ShouldCreateMetadataFromPagingCriteria()
        {
            // Arrange
            var criteria = new PagingCriteria(limit: 25, offset: 50);

            // Act
            var metadata = PaginationHelper.FromCriteria(criteria, totalCount: 200);

            // Assert
            metadata.CurrentPage.Should().Be(3);  // offset 50 / limit 25 + 1 = 3
            metadata.PageSize.Should().Be(25);
            metadata.TotalCount.Should().Be(200);
            metadata.TotalPages.Should().Be(8);   // 200 / 25 = 8
            metadata.HasNextPage.Should().BeTrue();
            metadata.HasPreviousPage.Should().BeTrue();
        }

        #endregion
    }
}

