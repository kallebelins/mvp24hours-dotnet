//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Test.Pagination;

/// <summary>
/// Unit tests for PagedBusinessResult functionality.
/// </summary>
[Trait("Category", "Unit")]
public class PagedBusinessResultTest
{
    #region [ Factory Tests ]

    [Fact]
    public void Success_ShouldWrapPagedResultWithoutErrors()
    {
        IPagedResult<string> paged = PagedResult<string>.Create(["A", "B"], 1, 10, 2);

        var result = PagedBusinessResult<string>.Success(paged, "token-1");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeSameAs(paged);
        result.PagedData.Should().BeSameAs(paged);
        result.Messages.Should().BeNull();
        result.Token.Should().Be("token-1");
    }

    [Fact]
    public void Failure_ShouldHaveErrorsAndNoData()
    {
        var messages = new List<IMessageResult>
        {
            new MessageResult("Page", "Invalid page", Core.Enums.MessageType.Error)
        };

        var result = PagedBusinessResult<string>.Failure(messages, "token-2");

        result.HasErrors.Should().BeTrue();
        result.Data.Should().BeNull();
        result.PagedData.Should().BeNull();
        result.Messages.Should().BeEquivalentTo(messages);
        result.Token.Should().Be("token-2");
    }

    [Fact]
    public void HasErrors_WithEmptyMessages_ShouldBeFalse()
    {
        var result = PagedBusinessResult<string>.Failure([]);

        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void SetToken_WhenEmpty_ShouldAssignToken()
    {
        IPagedResult<string> paged = PagedResult<string>.Empty();
        var result = PagedBusinessResult<string>.Success(paged);

        result.SetToken("new-token");

        result.Token.Should().Be("new-token");
    }

    [Fact]
    public void SetToken_WhenAlreadySet_ShouldNotOverwrite()
    {
        IPagedResult<string> paged = PagedResult<string>.Empty();
        var result = PagedBusinessResult<string>.Success(paged, "original");

        result.SetToken("replacement");

        result.Token.Should().Be("original");
    }

    #endregion

    #region [ Extension Tests ]

    [Fact]
    public void ToPagedBusinessResult_ShouldCreateSuccessResult()
    {
        IPagedResult<int> paged = PagedResult<int>.Create([1, 2, 3], 2, 5, 12);

        IPagedBusinessResult<int> result = paged.ToPagedBusinessResult("ext-token");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeSameAs(paged);
        result.Token.Should().Be("ext-token");
    }

    [Fact]
    public void ToPagedBusinessResult_WithoutToken_ShouldHaveNullToken()
    {
        IPagedResult<int> paged = PagedResult<int>.Create([1], 1, 10, 1);

        IPagedBusinessResult<int> result = paged.ToPagedBusinessResult();

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeSameAs(paged);
        result.Token.Should().BeNull();
    }

    #endregion
}
