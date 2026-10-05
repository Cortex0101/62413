namespace RatMan.Game;

public sealed class Cat
{
    internal Cat(CatKind kind, GridPosition spawn, int releaseCount)
    {
        Kind = kind;
        Motion = new(spawn);
        ReleaseCount = releaseCount;
    }

    public CatKind Kind { get; }
    public ActorMotion Motion { get; }
    public GridPosition Position => Motion.Position;
    public int ReleaseCount { get; }
    public CatState State { get; internal set; } = CatState.Waiting;
    public bool IsVisible => State is CatState.Chase or CatState.Scatter or CatState.ReturningHome;
    public bool CanContactPlayer => State is CatState.Chase or CatState.Scatter;
}

public interface ICatTargetStrategy
{
    GridPosition Target(Cat cat, Player player, GridPosition blinky, BoardSize size);
}

public static class CatTargets
{
    public static GridPosition Corner(CatKind kind, BoardSize size) => kind switch
    {
        CatKind.Blinky => new(0, size.Columns - 1),
        CatKind.Pinky => new(0, 0),
        CatKind.Inky => new(size.Rows - 1, size.Columns - 1),
        CatKind.Clyde => new(size.Rows - 1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static GridPosition Ahead(GridPosition player, Direction direction)
    {
        GridPosition ahead = player.AdjacentTo(direction).AdjacentTo(direction);
        return direction == Direction.Up ? ahead with { Column = ahead.Column - 2 } : ahead;
    }

    public static ICatTargetStrategy For(CatKind kind) => kind switch
    {
        CatKind.Blinky => new DirectTarget(),
        CatKind.Pinky => new AheadTarget(),
        CatKind.Inky => new VectorTarget(),
        CatKind.Clyde => new DistanceTarget(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static long SquaredDistance(GridPosition a, GridPosition b) =>
        (long)(a.Row - b.Row) * (a.Row - b.Row) + (long)(a.Column - b.Column) * (a.Column - b.Column);

    private sealed class DirectTarget : ICatTargetStrategy
    {
        public GridPosition Target(Cat cat, Player player, GridPosition blinky, BoardSize size) => player.Position;
    }
    private sealed class AheadTarget : ICatTargetStrategy
    {
        public GridPosition Target(Cat cat, Player player, GridPosition blinky, BoardSize size) => Ahead(player.Position, player.FacingDirection);
    }
    private sealed class VectorTarget : ICatTargetStrategy
    {
        public GridPosition Target(Cat cat, Player player, GridPosition blinky, BoardSize size)
        {
            GridPosition ahead = Ahead(player.Position, player.FacingDirection);
            return new(2 * ahead.Row - blinky.Row, 2 * ahead.Column - blinky.Column);
        }
    }
    private sealed class DistanceTarget : ICatTargetStrategy
    {
        public GridPosition Target(Cat cat, Player player, GridPosition blinky, BoardSize size) =>
            SquaredDistance(cat.Position, player.Position) < 64 ? Corner(CatKind.Clyde, size) : player.Position;
    }
}
