using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class GridPositionTests
{
    [TestMethod]
    public void EqualCoordinates_HaveValueEquality()
    {
        GridPosition first = new(2, 3);
        GridPosition second = new(2, 3);

        Assert.AreEqual(first, second);
    }

    [DataTestMethod]
    [DataRow(Direction.Up, 1, 2)]
    [DataRow(Direction.Down, 3, 2)]
    [DataRow(Direction.Left, 2, 1)]
    [DataRow(Direction.Right, 2, 3)]
    public void AdjacentTo_ReturnsPositionInDirection(Direction direction, int expectedRow, int expectedColumn)
    {
        GridPosition position = new(2, 2);

        GridPosition adjacent = position.AdjacentTo(direction);

        Assert.AreEqual(new GridPosition(expectedRow, expectedColumn), adjacent);
    }
}
