//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Core.Test.Contract;

/// <summary>
/// Unit tests for BulkOperationOptions, BulkOperationResult, SetPropertyCalls and related data contracts.
/// </summary>
[Trait("Category", "Unit")]
public class BulkOperationsContractTest
{
    private sealed class SampleEntity : IEntityBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Counter { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public object EntityKey => Id;
    }

    #region BulkOperationResult

    [Fact]
    public void BulkOperationResult_Success_SetsExpectedProperties()
    {
        var elapsed = TimeSpan.FromMilliseconds(250);

        BulkOperationResult result = BulkOperationResult.Success(42, elapsed);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(42);
        result.ElapsedTime.Should().Be(elapsed);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void BulkOperationResult_Failure_SetsErrorAndZeroRowsByDefault()
    {
        var elapsed = TimeSpan.FromSeconds(1);

        BulkOperationResult result = BulkOperationResult.Failure("timeout", elapsed);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
        result.ElapsedTime.Should().Be(elapsed);
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public void BulkOperationResult_Init_AllowsCustomConstruction()
    {
        var result = new BulkOperationResult
        {
            RowsAffected = 10,
            ElapsedTime = TimeSpan.FromMilliseconds(5),
            IsSuccess = true,
            ErrorMessage = null
        };

        result.RowsAffected.Should().Be(10);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region BulkOperationOptions

    [Fact]
    public void BulkOperationOptions_DefaultValues_AreCorrect()
    {
        var options = new BulkOperationOptions();

        options.BatchSize.Should().Be(1000);
        options.UseTransaction.Should().BeTrue();
        options.TimeoutSeconds.Should().Be(300);
        options.KeepIdentity.Should().BeFalse();
        options.UseTempTable.Should().BeFalse();
        options.BypassChangeTracking.Should().BeTrue();
        options.ProgressCallback.Should().BeNull();
    }

    [Fact]
    public void BulkOperationOptions_ProgressCallback_IsInvoked()
    {
        int processed = 0;
        int total = 0;
        var options = new BulkOperationOptions
        {
            BatchSize = 500,
            UseTransaction = false,
            TimeoutSeconds = 60,
            KeepIdentity = true,
            UseTempTable = true,
            BypassChangeTracking = false,
            ProgressCallback = (p, t) =>
            {
                processed = p;
                total = t;
            }
        };

        options.ProgressCallback!(25, 100);

        processed.Should().Be(25);
        total.Should().Be(100);
        options.BatchSize.Should().Be(500);
        options.UseTransaction.Should().BeFalse();
        options.TimeoutSeconds.Should().Be(60);
        options.KeepIdentity.Should().BeTrue();
        options.UseTempTable.Should().BeTrue();
        options.BypassChangeTracking.Should().BeFalse();
    }

    #endregion

    #region SetPropertyCalls

    [Fact]
    public void SetPropertyCalls_SetProperty_WithConstantValue_AddsSetter()
    {
        var setters = new SetPropertyCalls<SampleEntity>();

        setters.SetProperty(e => e.Name, "Updated");

        setters.Setters.Should().HaveCount(1);
        SetPropertyCall call = setters.Setters[0];
        call.Value.Should().Be("Updated");
        call.ValueExpression.Should().BeNull();
        call.Property.Should().NotBeNull();
    }

    [Fact]
    public void SetPropertyCalls_SetProperty_WithValueExpression_AddsSetter()
    {
        var setters = new SetPropertyCalls<SampleEntity>();

        setters.SetProperty(e => e.Counter, e => e.Counter + 1);

        setters.Setters.Should().ContainSingle();
        SetPropertyCall call = setters.Setters[0];
        call.Value.Should().BeNull();
        call.ValueExpression.Should().NotBeNull();
    }

    [Fact]
    public void SetPropertyCalls_FluentChaining_AccumulatesMultipleSetters()
    {
        var setters = new SetPropertyCalls<SampleEntity>();
        DateTime now = DateTime.UtcNow;

        setters
            .SetProperty(e => e.Name, "Bulk")
            .SetProperty(e => e.UpdatedAt, now)
            .SetProperty(e => e.Counter, e => e.Counter + 1);

        setters.Setters.Should().HaveCount(3);
        setters.Setters[0].Value.Should().Be("Bulk");
        setters.Setters[1].Value.Should().Be(now);
        setters.Setters[2].ValueExpression.Should().NotBeNull();
    }

    [Fact]
    public void SetPropertyCall_Init_StoresPropertyAndValue()
    {
        Expression<Func<SampleEntity, string>> property = e => e.Name;

        var call = new SetPropertyCall
        {
            Property = property,
            Value = "x",
            ValueExpression = null
        };

        call.Property.Should().BeSameAs(property);
        call.Value.Should().Be("x");
        call.ValueExpression.Should().BeNull();
    }

    #endregion

    #region Contract interfaces

    [Fact]
    public void IStreamingRepositoryAsync_ExtendsRepositoryAndStreamingQuery()
    {
        typeof(IStreamingRepositoryAsync<>).GetInterfaces()
            .Select(i => i.IsGenericType ? i.GetGenericTypeDefinition() : i)
            .Should().Contain([typeof(IRepositoryAsync<>), typeof(IStreamingQueryAsync<>)]);
    }

    [Fact]
    public void IStreamingQueryAsync_DeclaresStreamAndBatchMethods()
    {
        Type type = typeof(IStreamingQueryAsync<>);
        string[] methodNames = [.. type.GetMethods().Select(m => m.Name).Distinct()];

        methodNames.Should().Contain([
            "StreamAllAsync",
            "StreamByAsync",
            "StreamBatchesAsync"
        ]);
    }

    [Fact]
    public void IReadOnlyRepository_DeclaresSpecificationMethods()
    {
        Type type = typeof(IReadOnlyRepository<>);
        string[] methodNames = [.. type.GetMethods().Select(m => m.Name)];

        methodNames.Should().Contain([
            "AnyBySpecification",
            "CountBySpecification",
            "GetBySpecification",
            "GetSingleBySpecification",
            "GetFirstBySpecification"
        ]);
    }

    [Fact]
    public void IBulkOperationsAsync_DeclaresBulkAndExecuteMethods()
    {
        Type type = typeof(IBulkOperationsAsync<>);
        string[] methodNames = [.. type.GetMethods().Select(m => m.Name).Distinct()];

        methodNames.Should().Contain([
            "BulkInsertAsync",
            "BulkUpdateAsync",
            "BulkDeleteAsync",
            "ExecuteUpdateAsync",
            "ExecuteDeleteAsync"
        ]);
    }

    #endregion
}
