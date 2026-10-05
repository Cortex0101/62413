namespace RatMan.Game;

public enum FoodKind { Normal, Steroid }

public static class FoodPoints
{
    public static int For(FoodKind kind) => kind switch
    {
        FoodKind.Normal => 1,
        FoodKind.Steroid => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}

public sealed record FoodCollection(GridPosition Position, FoodKind Kind, int Points);

public static class FoodLayout
{
    public static Dictionary<GridPosition, FoodKind> Create(MazeDefinition maze)
    {
        var reachable = maze.PlayerSpawn is { } start
            ? new BoardTopology(maze.Size, maze.Walls, maze.OpenBorderRows, maze.OpenBorderColumns).ReachableFrom(start)
            : null;
        var empty = maze.EmptyFoodTiles.ToHashSet();
        var steroid = maze.SteroidCheeseTiles.ToHashSet();
        Dictionary<GridPosition, FoodKind> food = [];
        for (int row = 0; row < maze.Size.Rows; row++)
            for (int column = 0; column < maze.Size.Columns; column++)
            {
                GridPosition position = new(row, column);
                if (position == maze.PlayerSpawn || empty.Contains(position) ||
                    reachable is not null && !reachable.Contains(position)) continue;
                food.Add(position, steroid.Contains(position) ? FoodKind.Steroid : FoodKind.Normal);
            }
        return food;
    }
}
