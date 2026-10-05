namespace RatMan.Game;

public sealed class BoardTopology
{
    private static readonly Direction[] Directions = Enum.GetValues<Direction>();
    private readonly BoardSize size;
    private readonly HashSet<Wall> walls;
    private readonly HashSet<int> rows, columns;

    public BoardTopology(BoardSize size, IEnumerable<Wall> walls,
        IEnumerable<int> openRows, IEnumerable<int> openColumns)
    {
        this.size = size;
        this.walls = walls.ToHashSet();
        rows = openRows.ToHashSet();
        columns = openColumns.ToHashSet();
    }

    public bool TryStep(GridPosition from, Direction direction, out GridPosition destination)
    {
        destination = from.AdjacentTo(direction);
        if (size.Contains(destination)) return !walls.Contains(new Wall(from, destination));
        bool open = direction is Direction.Left or Direction.Right
            ? rows.Contains(from.Row) : columns.Contains(from.Column);
        if (!open) return false;
        destination = new((destination.Row + size.Rows) % size.Rows,
            (destination.Column + size.Columns) % size.Columns);
        return true;
    }

    public IReadOnlySet<GridPosition> ReachableFrom(GridPosition start)
    {
        HashSet<GridPosition> visited = [];
        if (!size.Contains(start)) return visited;
        Queue<GridPosition> pending = new();
        visited.Add(start);
        pending.Enqueue(start);
        while (pending.TryDequeue(out GridPosition current))
            foreach (Direction direction in Directions)
                if (TryStep(current, direction, out GridPosition next) && visited.Add(next))
                    pending.Enqueue(next);
        return visited;
    }
}
