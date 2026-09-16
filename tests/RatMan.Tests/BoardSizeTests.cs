using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class BoardSizeTests
{
    [DataTestMethod]
    [DataRow(0, 5)]
    [DataRow(-1, 5)]
    [DataRow(5, 0)]
    [DataRow(5, -1)]
    public void Constructor_WhenDimensionIsNotPositive_Throws(int rows, int columns)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new BoardSize(rows, columns));
    }

    [DataTestMethod]
    [DataRow(0, 0, true)]
    [DataRow(4, 6, true)]
    [DataRow(-1, 0, false)]
    [DataRow(0, -1, false)]
    [DataRow(5, 0, false)]
    [DataRow(0, 7, false)]
    public void Contains_ReturnsWhetherPositionIsInsideBoard(int row, int column, bool expected)
    {
        BoardSize boardSize = new(5, 7);

        bool result = boardSize.Contains(new GridPosition(row, column));

        Assert.AreEqual(expected, result);
    }
}
