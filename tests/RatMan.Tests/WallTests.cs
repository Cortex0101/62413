using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class WallTests
{
    [TestMethod]
    public void Constructor_WhenTilesAreNotAdjacent_Throws()
    {
        Assert.ThrowsException<ArgumentException>(
            () => new Wall(new GridPosition(1, 1), new GridPosition(2, 2)));
    }

    [TestMethod]
    public void Constructor_WhenTileOrderIsReversed_CreatesEqualWall()
    {
        GridPosition firstTile = new(1, 2);
        GridPosition secondTile = new(1, 3);

        Wall forward = new(firstTile, secondTile);
        Wall reverse = new(secondTile, firstTile);

        Assert.AreEqual(forward, reverse);
        Assert.AreEqual(firstTile, reverse.FirstTile);
        Assert.AreEqual(secondTile, reverse.SecondTile);
    }
}
