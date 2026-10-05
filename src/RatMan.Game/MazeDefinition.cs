namespace RatMan.Game;

public sealed record MazeDefinition(
    string Name,
    BoardSize Size,
    IReadOnlyCollection<Wall> Walls,
    GridPosition? PlayerSpawn,
    GridPosition? MonsterSpawn)
{
    public const int MaximumDimension = 100;

    public IReadOnlyCollection<GridPosition> EmptyFoodTiles { get; init; } = [];
    public IReadOnlyCollection<GridPosition> SteroidCheeseTiles { get; init; } = [];

    // Each index opens both opposing borders, so an unpaired opening is impossible.
    public IReadOnlyCollection<int> OpenBorderRows { get; init; } = [];
    public IReadOnlyCollection<int> OpenBorderColumns { get; init; } = [];

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];
        if (EmptyFoodTiles.Concat(SteroidCheeseTiles).Any(position => !Size.Contains(position)))
            errors.Add("Food coordinates must be inside the board.");
        if (EmptyFoodTiles.Intersect(SteroidCheeseTiles).Any())
            errors.Add("A tile cannot be both empty and contain steroid cheese.");
        if (OpenBorderRows.Any(row => row < 0 || row >= Size.Rows) ||
            OpenBorderColumns.Any(column => column < 0 || column >= Size.Columns))
            errors.Add("Border openings must be inside the board dimensions.");
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Enter a board name.");
        if (Size.Rows > MaximumDimension || Size.Columns > MaximumDimension)
            errors.Add("Width and height must be between 1 and 100.");
        if (PlayerSpawn is not { } player || !Size.Contains(player))
            errors.Add("Place exactly one player spawn inside the board.");
        if (MonsterSpawn is not { } monster || !Size.Contains(monster))
            errors.Add("Place exactly one monster spawn inside the board.");
        if (PlayerSpawn.HasValue && PlayerSpawn == MonsterSpawn)
            errors.Add("Player and monster spawns must use separate cells.");
        if (Walls.Any(wall => !Size.Contains(wall.FirstTile) || !Size.Contains(wall.SecondTile) ||
            Math.Abs(wall.FirstTile.Row - wall.SecondTile.Row) + Math.Abs(wall.FirstTile.Column - wall.SecondTile.Column) != 1))
            errors.Add("Walls must separate adjacent cells inside the board.");
        return errors;
    }
}
