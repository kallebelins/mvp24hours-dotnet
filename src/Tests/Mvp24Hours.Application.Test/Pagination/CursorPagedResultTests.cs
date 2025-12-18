//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Application.Logic.Pagination;
using Xunit;

namespace Mvp24Hours.Application.Test.Pagination
{
    /// <summary>
    /// Unit tests for CursorPagedResult functionality.
    /// </summary>
    public class CursorPagedResultTests
    {
        #region [ Create Tests ]

        [Fact]
        public void Create_WithValidParameters_ShouldCreateCursorPagedResult()
        {
            // Arrange
            var items = new[] { "Item1", "Item2", "Item3" };
            int pageSize = 10;
            bool hasMore = true;
            string nextCursor = "cursor123";

            // Act
            var result = CursorPagedResult<string>.Create(items, pageSize, hasMore, nextCursor);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(items);
            result.PageSize.Should().Be(pageSize);
            result.HasNextPage.Should().BeTrue();
            result.NextCursor.Should().Be(nextCursor);
        }

        [Fact]
        public void Empty_ShouldReturnEmptyCursorPagedResult()
        {
            // Act
            var result = CursorPagedResult<string>.Empty(25);

            // Assert
            result.Items.Should().BeEmpty();
            result.PageSize.Should().Be(25);
            result.HasNextPage.Should().BeFalse();
            result.HasPreviousPage.Should().BeFalse();
            result.NextCursor.Should().BeNull();
            result.PreviousCursor.Should().BeNull();
        }

        #endregion

        #region [ Navigation Tests ]

        [Fact]
        public void HasNextPage_WithMoreItems_ShouldBeTrue()
        {
            // Arrange
            var items = new[] { "Item1" };

            // Act
            var result = CursorPagedResult<string>.Create(
                items, 
                pageSize: 10, 
                hasMore: true, 
                nextCursor: "next");

            // Assert
            result.HasNextPage.Should().BeTrue();
            result.NextCursor.Should().Be("next");
        }

        [Fact]
        public void HasPreviousPage_WithPreviousCursor_ShouldBeTrue()
        {
            // Arrange
            var items = new[] { "Item1" };

            // Act
            var result = CursorPagedResult<string>.Create(
                items, 
                pageSize: 10, 
                hasMore: false, 
                nextCursor: null,
                previousCursor: "prev",
                hasPreviousPage: true);

            // Assert
            result.HasPreviousPage.Should().BeTrue();
            result.PreviousCursor.Should().Be("prev");
        }

        #endregion

        #region [ Count Tests ]

        [Fact]
        public void Count_ShouldReturnItemsCount()
        {
            // Arrange
            var items = new[] { "A", "B", "C" };

            // Act
            var result = CursorPagedResult<string>.Create(items, 10, false);

            // Assert
            result.Count.Should().Be(3);
        }

        #endregion

        #region [ Map Tests ]

        [Fact]
        public void Map_ShouldTransformItems()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = CursorPagedResult<int>.Create(
                items, 
                pageSize: 10, 
                hasMore: true, 
                nextCursor: "next");

            // Act
            var mappedResult = result.Map(x => x.ToString());

            // Assert
            mappedResult.Items.Should().BeEquivalentTo("1", "2", "3");
            mappedResult.PageSize.Should().Be(result.PageSize);
            mappedResult.HasNextPage.Should().Be(result.HasNextPage);
            mappedResult.NextCursor.Should().Be(result.NextCursor);
        }

        #endregion

        #region [ Strongly-Typed Cursor Tests ]

        [Fact]
        public void StronglyTypedCursor_ShouldSerializeCursorToBase64()
        {
            // Arrange
            var items = new[] { "Item1" };
            Guid cursorValue = Guid.NewGuid();

            // Act
            var result = CursorPagedResult<string, Guid>.Create(
                items, 
                pageSize: 10, 
                nextCursorValue: cursorValue,
                hasNextPage: true);

            // Assert
            result.NextCursorValue.Should().Be(cursorValue);
            result.NextCursor.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void DeserializeCursor_ShouldReturnOriginalValue()
        {
            // Arrange
            Guid originalValue = Guid.NewGuid();
            var items = new[] { "Item1" };
            var result = CursorPagedResult<string, Guid>.Create(
                items, 
                pageSize: 10, 
                nextCursorValue: originalValue,
                hasNextPage: true);

            // Act
            var deserializedValue = CursorPagedResult<string, Guid>.DeserializeCursor(result.NextCursor);

            // Assert
            deserializedValue.Should().Be(originalValue);
        }

        [Fact]
        public void DeserializeCursor_WithInvalidString_ShouldReturnNull()
        {
            // Act
            var result = CursorPagedResult<string, Guid>.DeserializeCursor("invalid-base64!");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeCursor_WithNullString_ShouldReturnNull()
        {
            // Act
            var result = CursorPagedResult<string, Guid>.DeserializeCursor(null);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region [ CompositeCursor Tests ]

        [Fact]
        public void CompositeCursor_ShouldStoreMultipleFields()
        {
            // Arrange & Act
            var cursor = CompositeCursor.Create()
                .WithField("Id", 123)
                .WithField("CreatedAt", DateTime.UtcNow.ToString("O"));

            // Assert
            cursor.Fields.Should().ContainKey("Id");
            cursor.Fields.Should().ContainKey("CreatedAt");
        }

        [Fact]
        public void CompositeCursor_Serialize_ShouldCreateBase64String()
        {
            // Arrange
            var cursor = CompositeCursor.Create("Id", 123, "Name", "Test");

            // Act
            var serialized = cursor.Serialize();

            // Assert
            serialized.Should().NotBeNullOrEmpty();
            // Should be valid Base64
            Action deserialize = () => Convert.FromBase64String(serialized);
            deserialize.Should().NotThrow();
        }

        [Fact]
        public void CompositeCursor_Deserialize_ShouldRecoverFields()
        {
            // Arrange
            var cursor = CompositeCursor.Create("Id", 123, "Name", "Test");
            var serialized = cursor.Serialize();

            // Act
            var deserialized = CompositeCursor.Deserialize(serialized);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Fields.Should().ContainKey("Id");
            deserialized.Fields.Should().ContainKey("Name");
        }

        [Fact]
        public void CompositeCursor_Deserialize_WithInvalidString_ShouldReturnNull()
        {
            // Act
            var result = CompositeCursor.Deserialize("not-valid-base64!");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}

