using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class ClassicMazeTests
{
    [TestMethod]
    public void Layout_IsCompactAndMirrored()
    {
        GameSession maze = ClassicMaze.CreateSession();
        Assert.AreEqual(new BoardSize(11, 12), maze.BoardSize);
        Assert.IsTrue(maze.BoardSize.Contains(maze.Player.Position));
        Assert.IsTrue(maze.Walls.Count > 0);
        foreach (Wall wall in maze.Walls)
        {
            Wall mirrored = new(
                new GridPosition(wall.FirstTile.Row, maze.BoardSize.Columns - 1 - wall.FirstTile.Column),
                new GridPosition(wall.SecondTile.Row, maze.BoardSize.Columns - 1 - wall.SecondTile.Column));
            Assert.IsTrue(maze.Walls.Contains(mirrored));
        }
    }

    [TestMethod]
    public void WallsBlockMovementFromBothSides()
    {
        GameSession maze = ClassicMaze.CreateSession();
        foreach (Wall wall in maze.Walls)
        {
            Direction forward = wall.FirstTile.Row == wall.SecondTile.Row ? Direction.Right : Direction.Down;
            Direction backward = forward == Direction.Right ? Direction.Left : Direction.Up;
            Assert.IsFalse(new GameSession(maze.BoardSize, wall.FirstTile, maze.Walls).TryMovePlayer(forward));
            Assert.IsFalse(new GameSession(maze.BoardSize, wall.SecondTile, maze.Walls).TryMovePlayer(backward));
        }
    }
}
