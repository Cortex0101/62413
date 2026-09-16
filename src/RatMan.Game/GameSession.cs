namespace RatMan.Game;

public sealed class GameSession
{
    public GameSession(BoardSize boardSize, GridPosition playerStartingPosition)
    {
        ArgumentNullException.ThrowIfNull(boardSize);

        if (!boardSize.Contains(playerStartingPosition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerStartingPosition),
                playerStartingPosition,
                "The player must start inside the board.");
        }

        BoardSize = boardSize;
        Player = new Player(playerStartingPosition);
    }

    public BoardSize BoardSize { get; }

    public Player Player { get; }

    public bool TryMovePlayer(Direction direction)
    {
        GridPosition destination = Player.Position.AdjacentTo(direction);

        if (!BoardSize.Contains(destination))
        {
            return false;
        }

        Player.MoveTo(destination);
        return true;
    }
}
