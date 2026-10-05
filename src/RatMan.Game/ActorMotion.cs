namespace RatMan.Game;

// All coordinates here are in tiles. WPF only projects this state into pixels.
public sealed class ActorMotion
{
    private long elapsedTicks;
    private long durationTicks;

    internal ActorMotion(GridPosition start) => Reset(start);
    public GridPosition Position { get; private set; }
    public GridPosition Destination { get; private set; }
    public Direction FacingDirection { get; private set; } = Direction.Down;
    public bool IsMoving { get; private set; }
    public bool Wraps { get; private set; }
    public bool HasMoved { get; private set; }
    public double Progress => IsMoving ? (double)elapsedTicks / durationTicks : 0;

    // A wrapped actor enters from outside the destination edge, never across the maze.
    public double Row => Coordinate(row: true);
    public double Column => Coordinate(row: false);

    private double Coordinate(bool row)
    {
        GridPosition origin = IsMoving && Wraps
            ? Destination.AdjacentTo(FacingDirection.Opposite()) : Position;
        int from = row ? origin.Row : origin.Column;
        int to = row ? Destination.Row : Destination.Column;
        return IsMoving ? from + (to - from) * Progress : from;
    }

    internal void Begin(GridPosition destination, Direction direction, bool wraps, TimeSpan duration)
    {
        Destination = destination;
        FacingDirection = direction;
        Wraps = wraps;
        IsMoving = HasMoved = true;
        elapsedTicks = 0;
        durationTicks = duration.Ticks;
    }

    internal MovementSegment Advance(TimeSpan elapsed)
    {
        MovementSegment before = Snapshot();
        if (!IsMoving) return before;
        elapsedTicks = Math.Min(durationTicks, elapsedTicks + elapsed.Ticks);
        var segment = before with { End = (double)elapsedTicks / durationTicks };
        if (elapsedTicks == durationTicks)
        {
            Position = Destination;
            IsMoving = false;
            Wraps = false;
        }
        return segment;
    }

    internal MovementSegment Snapshot() => new(Position, Destination,
        IsMoving, Wraps, Progress, Progress);

    internal void Reset(GridPosition start, Direction facing = Direction.Down)
    {
        Position = Destination = start;
        FacingDirection = facing;
        IsMoving = Wraps = HasMoved = false;
        elapsedTicks = durationTicks = 0;
    }
}

// One segment cannot cross a junction: move durations are multiples of the fixed step.
internal readonly record struct MovementSegment(GridPosition From, GridPosition To,
    bool Moving, bool Wraps, double Start, double End)
{
    public bool Arrived => Moving && End == 1;
    public GridPosition? VertexAt(double progress) => !Moving || progress == 0
        ? From : progress == 1 ? To : null;

    public double? ContactTime(MovementSegment other)
    {
        if (VertexAt(Start) is { } start && other.VertexAt(other.Start) == start) return 0;
        if (Moving && other.Moving && Wraps == other.Wraps)
        {
            double? before = null, after = null;
            if (From == other.From && To == other.To)
            {
                before = Start - other.Start;
                after = End - other.End;
            }
            else if (From == other.To && To == other.From)
            {
                before = Start + other.Start - 1;
                after = End + other.End - 1;
            }
            if (before is { } a && after is { } b && a * b <= 0)
                return a == b ? 0 : Math.Clamp(-a / (b - a), 0, 1);
        }
        return VertexAt(End) is { } end && other.VertexAt(other.End) == end ? 1 : null;
    }
}
