namespace RatMan.Game;

public sealed class GameSession
{
    private readonly HashSet<Wall> walls;
    private readonly BoardTopology topology;
    private readonly Dictionary<GridPosition, FoodKind> remainingFood;
    public IReadOnlyDictionary<GridPosition, FoodKind> RemainingFood { get; }
    public int Score { get; private set; }
    public IReadOnlyCollection<int> OpenBorderRows { get; } = [];
    public IReadOnlyCollection<int> OpenBorderColumns { get; } = [];

    public GameSession(MazeDefinition maze)
        : this(Validated(maze).Size, maze.PlayerSpawn!.Value, maze.Walls)
    {
        Name = maze.Name;
        MonsterStartingPosition = maze.MonsterSpawn;
        OpenBorderRows = maze.OpenBorderRows.ToArray();
        OpenBorderColumns = maze.OpenBorderColumns.ToArray();
        topology = new(BoardSize, walls, OpenBorderRows, OpenBorderColumns);
        remainingFood.Clear();
        foreach (var item in FoodLayout.Create(maze)) remainingFood.Add(item.Key, item.Value);
    }

    private static MazeDefinition Validated(MazeDefinition maze)
    {
        ArgumentNullException.ThrowIfNull(maze);
        IReadOnlyList<string> errors = maze.Validate();
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(maze));
        return maze;
    }

    public string Name { get; } = "RatMan";

    public GridPosition? MonsterStartingPosition { get; }

    public GameSession(
        BoardSize boardSize,
        GridPosition playerStartingPosition,
        IEnumerable<Wall>? walls = null)
    {
        ArgumentNullException.ThrowIfNull(boardSize);

        if (!boardSize.Contains(playerStartingPosition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerStartingPosition),
                playerStartingPosition,
                "The player must start inside the board.");
        }

        this.walls = walls?.ToHashSet() ?? [];

        foreach (Wall wall in this.walls)
        {
            if (!boardSize.Contains(wall.FirstTile) || !boardSize.Contains(wall.SecondTile))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(walls),
                    wall,
                    "Every wall must be inside the board.");
            }
        }

        BoardSize = boardSize;
        Player = new Player(playerStartingPosition);
        topology = new(BoardSize, this.walls, [], []);
        remainingFood = FoodLayout.Create(new("", boardSize, this.walls, playerStartingPosition, null));
        RemainingFood = new System.Collections.ObjectModel.ReadOnlyDictionary<GridPosition, FoodKind>(remainingFood);
    }

    public BoardSize BoardSize { get; }

    public Player Player { get; }

    public IReadOnlySet<Wall> Walls => walls;

    // Called at tile arrival; repeat calls on an empty tile cannot award points.
    public FoodCollection? CollectFoodAtPlayer()
    {
        if (!remainingFood.TryGetValue(Player.Position, out FoodKind kind)) return null;
        int points = FoodPoints.For(kind);
        int nextScore = checked(Score + points);
        remainingFood.Remove(Player.Position);
        Score = nextScore;
        return new(Player.Position, kind, points);
    }

    public bool TryMovePlayer(Direction direction)
    {
        if (!topology.TryStep(Player.Position, direction, out GridPosition destination))
        {
            return false;
        }

        Player.MoveTo(destination);
        return true;
    }
}
