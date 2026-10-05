namespace RatMan.Game;

public sealed class CatNavigator
{
    private static readonly Direction[] Priority = [Direction.Up, Direction.Left, Direction.Down, Direction.Right];
    private readonly BoardTopology topology;
    private readonly IReadOnlySet<GridPosition> reachable;
    private readonly GridPosition[] orderedTiles;

    public CatNavigator(BoardTopology topology, GridPosition home)
    {
        this.topology = topology;
        reachable = topology.ReachableFrom(home);
        orderedTiles = reachable.OrderBy(p => p.Row).ThenBy(p => p.Column).ToArray();
    }

    public bool CanReach(GridPosition position) => reachable.Contains(position);

    public GridPosition ResolveTarget(GridPosition target) => reachable.Contains(target)
        ? target : orderedTiles.MinBy(p => CatTargets.SquaredDistance(p, target));

    public Direction? ChooseDirection(GridPosition from, GridPosition target, Direction? facing, bool returning = false)
    {
        target = ResolveTarget(target);
        Dictionary<GridPosition, int> distances = new() { [target] = 0 };
        Queue<GridPosition> queue = new();
        queue.Enqueue(target);
        while (queue.TryDequeue(out GridPosition tile))
            foreach (Direction direction in Priority)
                if (topology.TryStep(tile, direction, out GridPosition next) && distances.TryAdd(next, distances[tile] + 1))
                    queue.Enqueue(next);

        var exits = Priority.Where(d => topology.TryStep(from, d, out var next) && next != from).ToArray();
        if (!returning && facing is { } current && exits.Any(d => d != current.Opposite()))
            exits = exits.Where(d => d != current.Opposite()).ToArray();
        Direction? best = null;
        int bestDistance = int.MaxValue;
        foreach (Direction exit in exits)
        {
            topology.TryStep(from, exit, out var next);
            if (distances.TryGetValue(next, out int distance) && distance < bestDistance)
            {
                best = exit;
                bestDistance = distance;
            }
        }
        return best;
    }
}
