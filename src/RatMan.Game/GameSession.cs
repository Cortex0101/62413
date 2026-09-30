namespace RatMan.Game;

public sealed class GameSession
{
    private readonly HashSet<Wall> walls;

    public GameSession(MazeDefinition maze)
        : this(Validated(maze).Size, maze.PlayerSpawn!.Value, maze.Walls)
    {
        Name = maze.Name;
        MonsterStartingPosition = maze.MonsterSpawn;
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
    }

    public BoardSize BoardSize { get; }

    public Player Player { get; }

    public IReadOnlySet<Wall> Walls => walls;

    public bool TryMovePlayer(Direction direction)
    {
        GridPosition destination = Player.Position.AdjacentTo(direction);

        if (!BoardSize.Contains(destination))
        {
            return false;
        }

        if (walls.Contains(new Wall(Player.Position, destination)))
        {
            return false;
        }

        Player.MoveTo(destination);
        return true;
    }
}
