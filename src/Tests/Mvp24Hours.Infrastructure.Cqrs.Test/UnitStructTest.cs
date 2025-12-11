//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for the Unit struct.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class UnitStructTest
{
    [Fact, Priority(1)]
    public void Unit_Value_ShouldBeDefault()
    {
        // Arrange & Act
        var unit = Unit.Value;

        // Assert
        Assert.Equal(default, unit);
    }

    [Fact, Priority(2)]
    public async Task Unit_Task_ShouldReturnCompletedTask()
    {
        // Arrange & Act
        var task = Unit.Task;

        // Assert
        Assert.True(task.IsCompleted);
        Assert.Equal(Unit.Value, await task);
    }

    [Fact, Priority(3)]
    public void Unit_Equals_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = new Unit();

        // Act & Assert
        Assert.True(unit1.Equals(unit2));
        Assert.True(unit2.Equals(unit1));
    }

    [Fact, Priority(4)]
    public void Unit_EqualsObject_ShouldReturnTrueForUnit()
    {
        // Arrange
        var unit = Unit.Value;
        object obj = new Unit();

        // Act & Assert
        Assert.True(unit.Equals(obj));
    }

    [Fact, Priority(5)]
    public void Unit_EqualsObject_ShouldReturnFalseForNonUnit()
    {
        // Arrange
        var unit = Unit.Value;
        object obj = "not a unit";

        // Act & Assert
        Assert.False(unit.Equals(obj));
    }

    [Fact, Priority(6)]
    public void Unit_GetHashCode_ShouldAlwaysReturnZero()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = new Unit();

        // Act & Assert
        Assert.Equal(0, unit1.GetHashCode());
        Assert.Equal(0, unit2.GetHashCode());
    }

    [Fact, Priority(7)]
    public void Unit_CompareTo_ShouldAlwaysReturnZero()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = new Unit();

        // Act & Assert
        Assert.Equal(0, unit1.CompareTo(unit2));
    }

    [Fact, Priority(8)]
    public void Unit_CompareToObject_ShouldAlwaysReturnZero()
    {
        // Arrange
        IComparable unit = Unit.Value;
        object other = new Unit();

        // Act & Assert
        Assert.Equal(0, unit.CompareTo(other));
    }

    [Fact, Priority(9)]
    public void Unit_ToString_ShouldReturnParentheses()
    {
        // Arrange
        var unit = Unit.Value;

        // Act & Assert
        Assert.Equal("()", unit.ToString());
    }

    [Fact, Priority(10)]
    public void Unit_EqualityOperator_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = new Unit();

        // Act & Assert
        Assert.True(unit1 == unit2);
    }

    [Fact, Priority(11)]
    public void Unit_InequalityOperator_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = new Unit();

        // Act & Assert
        Assert.False(unit1 != unit2);
    }

    [Fact, Priority(12)]
    public async Task Unit_Task_ShouldBeReusable()
    {
        // Arrange & Act
        var task1 = Unit.Task;
        var task2 = Unit.Task;

        // Assert - Same cached task instance
        Assert.Same(task1, task2);
        Assert.True(task1.IsCompleted);
        Assert.Equal(Unit.Value, await task1);
    }
}

