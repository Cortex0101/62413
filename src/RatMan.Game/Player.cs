namespace RatMan.Game;

public sealed class Player
{
    internal Player(GridPosition startingPosition)
    {
        Position = startingPosition;
    }

    public GridPosition Position { get; private set; }

    internal void MoveTo(GridPosition position)
    {
        Position = position;
    }
}
