//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Application.Logic.Pagination;
using Xunit;

namespace Mvp24Hours.Application.Test.Pagination
{
    /// <summary>
    /// Unit tests for PagedResult functionality.
    /// </summary>
    public class PagedResultTests
    {
        #region [ Create Tests ]

        [Fact]
        public void Create_WithValidParameters_ShouldCreatePagedResult()
        {
            // Arrange
            var items = new[] { "Item1", "Item2", "Item3" };
            int page = 1;
            int pageSize = 10;
            int totalCount = 100;

            // Act
            var result = PagedResult<string>.Create(items, page, pageSize, totalCount);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(items);
            result.CurrentPage.Should().Be(page);
            result.PageSize.Should().Be(pageSize);
            result.TotalCount.Should().Be(totalCount);
        }

        [Fact]
        public void Create_WithZeroItems_ShouldReturnEmptyResult()
        {
            // Arrange
            var items = Array.Empty<int>();
            int page = 1;
            int pageSize = 10;
            int totalCount = 0;

            // Act
            var result = PagedResult<int>.Create(items, page, pageSize, totalCount);

            // Assert
            result.Items.Should().BeEmpty();
            result.Count.Should().Be(0);
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        [Fact]
        public void Empty_ShouldReturnEmptyPagedResult()
        {
            // Act
            var result = PagedResult<string>.Empty(25);

            // Assert
            result.Items.Should().BeEmpty();
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(25);
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        #endregion

        #region [ TotalPages Calculation Tests ]

        [Theory]
        [InlineData(100, 10, 10)]   // Exact division
        [InlineData(101, 10, 11)]   // One extra item
        [InlineData(99, 10, 10)]    // One less item
        [InlineData(1, 10, 1)]      // Single item
        [InlineData(0, 10, 0)]      // No items
        [InlineData(25, 5, 5)]      // Different page size
        public void TotalPages_ShouldCalculateCorrectly(int totalCount, int pageSize, int expectedTotalPages)
        {
            // Arrange
            var items = Array.Empty<string>();

            // Act
            var result = PagedResult<string>.Create(items, 1, pageSize, totalCount);

            // Assert
            result.TotalPages.Should().Be(expectedTotalPages);
        }

        #endregion

        #region [ Navigation Tests ]

        [Fact]
        public void HasNextPage_OnFirstPage_WithMultiplePages_ShouldBeTrue()
        {
            // Arrange
            var items = new[] { "Item1" };

            // Act
            var result = PagedResult<string>.Create(items, 1, 10, 100);

            // Assert
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeFalse();
            result.IsFirstPage.Should().BeTrue();
            result.IsLastPage.Should().BeFalse();
        }

        [Fact]
        public void HasPreviousPage_OnMiddlePage_ShouldBeTrue()
        {
            // Arrange
            var items = new[] { "Item1" };

            // Act
            var result = PagedResult<string>.Create(items, 5, 10, 100);

            // Assert
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeTrue();
            result.IsFirstPage.Should().BeFalse();
            result.IsLastPage.Should().BeFalse();
        }

        [Fact]
        public void IsLastPage_OnLastPage_ShouldBeTrue()
        {
            // Arrange
            var items = new[] { "Item1" };

            // Act
            var result = PagedResult<string>.Create(items, 10, 10, 100);

            // Assert
            result.HasNextPage.Should().BeFalse();
            result.HasPreviousPage.Should().BeTrue();
            result.IsFirstPage.Should().BeFalse();
            result.IsLastPage.Should().BeTrue();
        }

        [Fact]
        public void Navigation_OnSinglePage_ShouldBeFirstAndLast()
        {
            // Arrange
            var items = new[] { "Item1", "Item2" };

            // Act
            var result = PagedResult<string>.Create(items, 1, 10, 2);

            // Assert
            result.HasNextPage.Should().BeFalse();
            result.HasPreviousPage.Should().BeFalse();
            result.IsFirstPage.Should().BeTrue();
            result.IsLastPage.Should().BeTrue();
        }

        #endregion

        #region [ Index Tests ]

        [Theory]
        [InlineData(1, 10, 100, 1, 10)]     // First page
        [InlineData(2, 10, 100, 11, 20)]    // Second page
        [InlineData(10, 10, 100, 91, 100)]  // Last page
        [InlineData(10, 10, 95, 91, 95)]    // Last page partial
        [InlineData(1, 20, 5, 1, 5)]        // Partial first page
        public void Indexes_ShouldCalculateCorrectly(
            int page, int pageSize, int totalCount, int expectedStart, int expectedEnd)
        {
            // Arrange
            var items = Array.Empty<string>();

            // Act
            var result = PagedResult<string>.Create(items, page, pageSize, totalCount);

            // Assert
            result.StartIndex.Should().Be(expectedStart);
            result.EndIndex.Should().Be(expectedEnd);
        }

        [Fact]
        public void StartIndex_WithZeroItems_ShouldBeZero()
        {
            // Arrange & Act
            var result = PagedResult<string>.Empty();

            // Assert
            result.StartIndex.Should().Be(0);
            result.EndIndex.Should().Be(0);
        }

        #endregion

        #region [ Map Tests ]

        [Fact]
        public void Map_ShouldTransformItems()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PagedResult<int>.Create(items, 1, 10, 100);

            // Act
            var mappedResult = result.Map(x => x.ToString());

            // Assert
            mappedResult.Items.Should().BeEquivalentTo("1", "2", "3");
            mappedResult.CurrentPage.Should().Be(result.CurrentPage);
            mappedResult.PageSize.Should().Be(result.PageSize);
            mappedResult.TotalCount.Should().Be(result.TotalCount);
        }

        #endregion

        #region [ ToMetadata Tests ]

        [Fact]
        public void ToMetadata_ShouldReturnAllMetadata()
        {
            // Arrange
            var items = new[] { "A", "B" };
            var result = PagedResult<string>.Create(items, 2, 10, 50);

            // Act
            var metadata = result.ToMetadata();

            // Assert
            metadata.Should().ContainKey("currentPage").WhoseValue.Should().Be(2);
            metadata.Should().ContainKey("pageSize").WhoseValue.Should().Be(10);
            metadata.Should().ContainKey("totalCount").WhoseValue.Should().Be(50);
            metadata.Should().ContainKey("totalPages").WhoseValue.Should().Be(5);
            metadata.Should().ContainKey("hasNextPage").WhoseValue.Should().Be(true);
            metadata.Should().ContainKey("hasPreviousPage").WhoseValue.Should().Be(true);
            metadata.Should().ContainKey("count").WhoseValue.Should().Be(2);
        }

        #endregion

        #region [ CreateFromOffset Tests ]

        [Theory]
        [InlineData(0, 10, 100, 1)]   // First page (offset 0)
        [InlineData(10, 10, 100, 2)]  // Second page (offset 10)
        [InlineData(90, 10, 100, 10)] // Last page (offset 90)
        [InlineData(20, 5, 100, 5)]   // Different page size
        public void CreateFromOffset_ShouldConvertToCorrectPage(
            int offset, int limit, int totalCount, int expectedPage)
        {
            // Arrange
            var items = Array.Empty<string>();

            // Act
            var result = PagedResult<string>.CreateFromOffset(items, offset, limit, totalCount);

            // Assert
            result.CurrentPage.Should().Be(expectedPage);
        }

        #endregion
    }
}

