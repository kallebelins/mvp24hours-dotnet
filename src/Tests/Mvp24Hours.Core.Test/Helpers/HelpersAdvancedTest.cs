//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Test.Helpers;

/// <summary>
/// Unit tests for ConstantsHelper.
/// ReflectionHelper/ExpressionHelper/CryptoHelper do not exist — these are the remaining Core helpers.
/// </summary>
[Trait("Category", "Unit")]
public class HelpersAdvancedTest
{
    #region ConstantsHelper

    [Fact]
    public void ConstantsHelper_MaxQtyByQueryPage_Is300()
    {
        ConstantsHelper.Data.MaxQtyByQueryPage.Should().Be(300);
    }

    [Fact]
    public void ContantsHelper_ObsoleteShim_MatchesConstantsHelper()
    {
        // intentional: covers the obsolete ContantsHelper shim until removal in v12
#pragma warning disable CS0618
        ContantsHelper.Data.MaxQtyByQueryPage.Should().Be(ConstantsHelper.Data.MaxQtyByQueryPage);
#pragma warning restore CS0618
    }

    #endregion
}
