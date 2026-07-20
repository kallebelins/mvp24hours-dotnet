//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;

namespace Mvp24Hours.Core.Test.Helpers;

/// <summary>
/// Unit tests for ContantsHelper and TelemetryHelper.
/// ReflectionHelper/ExpressionHelper/CryptoHelper do not exist — these are the remaining Core helpers.
/// </summary>
[Trait("Category", "Unit")]
[Collection("TelemetryHelper")]
public class HelpersAdvancedTest
{
#pragma warning disable CS0618
    private sealed class CaptureTelemetryService : ITelemetryService
    {
        public List<(string EventName, object[] Args)> Calls { get; } = [];

        public void Execute(string eventName, params object[] args)
        {
            Calls.Add((eventName, args));
        }
    }
#pragma warning restore CS0618

    public HelpersAdvancedTest()
    {
#pragma warning disable CS0618
        TelemetryHelper.Clear();
#pragma warning restore CS0618
    }

    #region ContantsHelper

    [Fact]
    public void ContantsHelper_Data_MaxQtyByQueryPage_Is300()
    {
        ContantsHelper.Data.MaxQtyByQueryPage.Should().Be(300);
    }

    #endregion

    #region TelemetryHelper

    [Fact]
    public void TelemetryHelper_Add_And_GetActions_RegisterHandlers()
    {
#pragma warning disable CS0618
        var names = new List<string>();
        TelemetryHelper.Add(TelemetryLevels.Information, name => names.Add(name));

        TelemetryHelper.GetActions1(TelemetryLevels.Information).Should().HaveCount(1);
        TelemetryHelper.Execute(TelemetryLevels.Information, "EvtA");

        names.Should().ContainSingle().Which.Should().Be("EvtA");
        TelemetryHelper.Remove(TelemetryLevels.Information);
        TelemetryHelper.GetActions1(TelemetryLevels.Information).Should().BeEmpty();
#pragma warning restore CS0618
    }

    [Fact]
    public void TelemetryHelper_Add_WithArgs_InvokesAction2()
    {
#pragma warning disable CS0618
        string? captured = null;
        object[]? args = null;
        TelemetryHelper.Add(TelemetryLevels.Warning, (name, a) =>
        {
            captured = name;
            args = a;
        });

        TelemetryHelper.Execute(TelemetryLevels.Warning, "EvtB", 1, "x");

        captured.Should().Be("EvtB");
        args.Should().Equal(1, "x");
        TelemetryHelper.Remove(TelemetryLevels.Warning);
#pragma warning restore CS0618
    }

    [Fact]
    public void TelemetryHelper_AddService_And_Filter_Execute()
    {
#pragma warning disable CS0618
        var service = new CaptureTelemetryService();
        TelemetryHelper.Add(TelemetryLevels.Error, service);
        TelemetryHelper.AddFilter("FilteredEvt", service);

        TelemetryHelper.Execute(TelemetryLevels.Error, "FilteredEvt", 42);

        service.Calls.Should().HaveCount(2);
        service.Calls.Should().OnlyContain(c => c.EventName == "FilteredEvt");
        TelemetryHelper.GetServices(TelemetryLevels.Error).Should().ContainSingle();
        TelemetryHelper.GetFilters("FilteredEvt").Should().ContainSingle();

        TelemetryHelper.Remove(TelemetryLevels.Error);
        TelemetryHelper.Remove("FilteredEvt");
#pragma warning restore CS0618
    }

    [Fact]
    public void TelemetryHelper_IgnoreService_SkipsExecution()
    {
#pragma warning disable CS0618
        var names = new List<string>();
        TelemetryHelper.Add(TelemetryLevels.Information, name => names.Add(name));
        TelemetryHelper.AddIgnoreService("IgnoredEvt");

        TelemetryHelper.Execute(TelemetryLevels.Information, "IgnoredEvt");
        names.Should().BeEmpty();

        TelemetryHelper.RemoveIgnoreService("IgnoredEvt");
        TelemetryHelper.Execute(TelemetryLevels.Information, "IgnoredEvt");
        names.Should().ContainSingle().Which.Should().Be("IgnoredEvt");

        TelemetryHelper.Remove(TelemetryLevels.Information);
#pragma warning restore CS0618
    }

    [Fact]
    public void TelemetryHelper_Add_WithEmptyActions_Throws()
    {
#pragma warning disable CS0618
        Action act = () => TelemetryHelper.Add(TelemetryLevels.Information, Array.Empty<Action<string>>());
        act.Should().Throw<ArgumentNullException>();
#pragma warning restore CS0618
    }

    [Fact]
    public void TelemetryHelper_AddFilter_WithEmptyName_Throws()
    {
#pragma warning disable CS0618
        Action act = () => TelemetryHelper.AddFilter("", (Action<string>)(_ => { }));
        act.Should().Throw<ArgumentNullException>();
#pragma warning restore CS0618
    }

    #endregion
}

/// <summary>
/// Serializes TelemetryHelper tests because it uses static shared state.
/// </summary>
[CollectionDefinition("TelemetryHelper", DisableParallelization = true)]
public class TelemetryHelperCollection
{
}
