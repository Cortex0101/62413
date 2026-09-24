using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class GameSessionTests
{
    [TestMethod]
    public void Constructor_WhenPlayerStartsOutsideBoard_Throws()
    {
        BoardSize boardSize = new(5, 7);

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new GameSession(boardSize, new GridPosition(5, 0)));
    }

    [TestMethod]
    public void Constructor_WhenWallIsOutsideBoard_Throws()
    {
        BoardSize boardSize = new(5, 7);
        Wall wall = new(new GridPosition(4, 3), new GridPosition(5, 3));

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new GameSession(boardSize, new GridPosition(2, 3), [wall]));
    }

    [DataTestMethod]
    [DataRow(Direction.Up, 1, 3)]
    [DataRow(Direction.Down, 3, 3)]
    [DataRow(Direction.Left, 2, 2)]
    [DataRow(Direction.Right, 2, 4)]
    public void TryMovePlayer_WhenDestinationIsInsideBoard_MovesOneCell(
        Direction direction,
        int expectedRow,
        int expectedColumn)
    {
        GameSession session = TestGameSessionFactory.Create();

        bool moved = session.TryMovePlayer(direction);

        Assert.IsTrue(moved);
        Assert.AreEqual(new GridPosition(expectedRow, expectedColumn), session.Player.Position);
    }

    [DataTestMethod]
    [DataRow(Direction.Up, 0, 3)]
    [DataRow(Direction.Down, 4, 3)]
    [DataRow(Direction.Left, 2, 0)]
    [DataRow(Direction.Right, 2, 6)]
    public void TryMovePlayer_WhenDestinationIsOutsideBoard_DoesNotMove(
        Direction direction,
        int startingRow,
        int startingColumn)
    {
        GameSession session = TestGameSessionFactory.Create(
            playerRow: startingRow,
            playerColumn: startingColumn);
        GridPosition originalPosition = session.Player.Position;

        bool moved = session.TryMovePlayer(direction);

        Assert.IsFalse(moved);
        Assert.AreEqual(originalPosition, session.Player.Position);
    }

    [TestMethod]
    public void TryMovePlayer_WhenCalledRepeatedly_UpdatesFromCurrentPosition()
    {
        GameSession session = TestGameSessionFactory.Create();

        session.TryMovePlayer(Direction.Right);
        session.TryMovePlayer(Direction.Down);
        session.TryMovePlayer(Direction.Left);

        Assert.AreEqual(new GridPosition(3, 3), session.Player.Position);
    }

    [DataTestMethod]
    [DataRow(2, 3, Direction.Right)]
    [DataRow(2, 4, Direction.Left)]
    public void TryMovePlayer_WhenWallSeparatesTiles_DoesNotMove(
        int startingRow,
        int startingColumn,
        Direction direction)
    {
        Wall wall = new(new GridPosition(2, 3), new GridPosition(2, 4));
        GameSession session = new(
            new BoardSize(5, 7),
            new GridPosition(startingRow, startingColumn),
            [wall]);

        bool moved = session.TryMovePlayer(direction);

        Assert.IsFalse(moved);
        Assert.AreEqual(new GridPosition(startingRow, startingColumn), session.Player.Position);
    }
}
