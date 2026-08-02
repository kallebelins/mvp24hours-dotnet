//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.Extensions.Exceptions;

namespace Mvp24Hours.Core.Test.Extensions;

/// <summary>
/// Unit tests for remaining Core extension helpers (Exception, Task, GenerateKey).
/// String/Enumerable/Convert already have dedicated suites — this fills the gaps for phase 18.8.
/// </summary>
[Trait("Category", "Unit")]
public class AdvancedExtensionsTest
{
    private sealed class KeyEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    #region ExceptionExtensions

    [Fact]
    public void ToBusinessResult_MapsExceptionMessageAndCode()
    {
        var ex = new NotFoundException("missing");

        IBusinessResult<string> result = ex.ToBusinessResult<string>();

        result.HasErrors.Should().BeTrue();
        result.Messages.Should().ContainSingle(m => m.Message == "missing");
        ex.GetErrorCode().Should().Be("NOT_FOUND");
    }

    [Fact]
    public void ToBusinessResult_WithCustomMessage_UsesCustomText()
    {
        var ex = new ValidationException("internal");

        IBusinessResult<int> result = ex.ToBusinessResult<int>("User friendly");

        result.Messages.Should().ContainSingle(m => m.Message == "User friendly");
        ex.GetErrorCode().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public void ToBusinessResult_WithNullException_Throws()
    {
        Exception? ex = null;
        Action act = () => ex!.ToBusinessResult<string>();
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(typeof(NotFoundException), HttpStatusCode.NotFound)]
    [InlineData(typeof(ConflictException), HttpStatusCode.Conflict)]
    [InlineData(typeof(UnauthorizedException), HttpStatusCode.Unauthorized)]
    [InlineData(typeof(ForbiddenException), HttpStatusCode.Forbidden)]
    [InlineData(typeof(ValidationException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(TimeoutException), HttpStatusCode.RequestTimeout)]
    public void ToHttpStatusCode_MapsKnownExceptions(Type exceptionType, HttpStatusCode expected)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType, "msg")!;

        ex.ToHttpStatusCode().Should().Be(expected);
        ex.ToHttpStatusCodeInt().Should().Be((int)expected);
    }

    [Fact]
    public void IsClientError_And_IsServerError_ClassifyStatusRanges()
    {
        new NotFoundException("x").IsClientError().Should().BeTrue();
        new NotFoundException("x").IsServerError().Should().BeFalse();
        new ConfigurationException("x").IsServerError().Should().BeTrue();
        new ConfigurationException("x").IsClientError().Should().BeFalse();
    }

    [Fact]
    public void ToUserFriendlyMessage_ReturnsExpectedText()
    {
        new UnauthorizedException("x").ToUserFriendlyMessage().Should().Contain("Authentication");
        new ForbiddenException("x").ToUserFriendlyMessage().Should().Contain("permission");
        new ValidationException("bad field").ToUserFriendlyMessage().Should().Contain("Validation failed");
        new TimeoutException().ToUserFriendlyMessage().Should().Contain("timed out");
        new Exception("raw").ToUserFriendlyMessage(includeDetails: true).Should().Contain("[Exception]");
    }

    [Fact]
    public void GetErrorCode_CoversCommonExceptionTypes()
    {
        new ArgumentNullException().GetErrorCode().Should().Be("ARGUMENT_NULL");
        new ArgumentException().GetErrorCode().Should().Be("ARGUMENT_INVALID");
        new InvalidOperationException().GetErrorCode().Should().Be("INVALID_OPERATION");
        new OperationCanceledException().GetErrorCode().Should().Be("OPERATION_CANCELLED");
        new Exception().GetErrorCode().Should().Be("INTERNAL_ERROR");
        new BusinessException("b", "BIZ").GetErrorCode().Should().Be("BIZ");
    }

    #endregion

    #region TaskExtensions

    [Fact]
    public void RunSync_ExecutesVoidAndResultTasks()
    {
        bool ran = false;
        ((Func<Task>)(async () =>
        {
            await Task.Yield();
            ran = true;
        })).RunSync();
        ran.Should().BeTrue();

        int value = ((Func<Task<int>>)(async () =>
        {
            await Task.Yield();
            return 7;
        })).RunSync();
        value.Should().Be(7);
    }

    [Fact]
    public async Task TaskResult_And_TaskComplete_Work()
    {
        string result = await "hello".TaskResult();
        result.Should().Be("hello");

        Func<Task> act = async () => await Mvp24Hours.Extensions.TaskExtensions.TaskComplete();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TaskIntComparisons_Work()
    {
        (await Task.FromResult(5).IsGreaterThanZeroAsync()).Should().BeTrue();
        (await Task.FromResult(0).IsEqualToZeroAsync()).Should().BeTrue();
        (await Task.FromResult(-1).IsLessThanZeroAsync()).Should().BeTrue();
        (await Task.FromResult(true).IsTrueAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task TaskList_FirstLastElementHelpers_Work()
    {
        Task<IList<int>> listTask = Task.FromResult<IList<int>>([10, 20, 30]);

        (await listTask.FirstOrDefaultAsync()).Should().Be(10);
        (await listTask.FirstOrDefaultAsync(x => x > 15)).Should().Be(20);
        (await listTask.LastOrDefaultAsync()).Should().Be(30);
        (await listTask.LastOrDefaultAsync(x => x < 25)).Should().Be(20);
        (await listTask.ElementAtOrDefaultAsync(1)).Should().Be(20);
        (await Task.FromResult<IList<int>>(null!).FirstOrDefaultAsync()).Should().Be(0);
    }

    #endregion

    #region GenerateKeyExtensions

    [Fact]
    public void ToKey_And_ToHash_ProduceStableOutputForSameEntity()
    {
        var a = new KeyEntity { Id = "1", Name = "A", Status = "Active" };
        var b = new KeyEntity { Id = "1", Name = "A", Status = "Active" };
        var c = new KeyEntity { Id = "2", Name = "A", Status = "Active" };

        string keyA = a.ToKey();
        string keyB = b.ToKey();
        byte[] hashA = GenerateKeyExtensions.ToHash(a);
        byte[] hashC = GenerateKeyExtensions.ToHash(c);

        keyA.Should().Be(keyB);
        hashA.Should().NotBeEmpty();
        hashA.Should().NotBeEquivalentTo(hashC);
    }

    #endregion
}
