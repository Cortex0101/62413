namespace RatMan.Game;

public sealed class GameSession
{
    private readonly HashSet<Wall> walls;
    private readonly BoardTopology topology;
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
    }

    public BoardSize BoardSize { get; }

    public Player Player { get; }

    public IReadOnlySet<Wall> Walls => walls;

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
