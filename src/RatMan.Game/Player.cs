namespace RatMan.Game;

public sealed class Player
{
    internal Player(GridPosition startingPosition)
    {
        Motion = new(startingPosition);
    }

    public ActorMotion Motion { get; }
    public GridPosition Position => Motion.Position;
    public Direction FacingDirection => Motion.FacingDirection;
    public TimeSpan BlockedRemaining { get; internal set; }

    internal void MoveTo(GridPosition position, Direction direction)
    {
        Motion.Reset(position, direction);
    }
}
