namespace RatMan.Game;

public readonly record struct Wall
{
    public Wall(GridPosition firstTile, GridPosition secondTile)
    {
        int distance = Math.Abs(firstTile.Row - secondTile.Row) +
            Math.Abs(firstTile.Column - secondTile.Column);

        if (distance != 1)
        {
            throw new ArgumentException("A wall must separate two orthogonally adjacent tiles.", nameof(secondTile));
        }

        if (Compare(firstTile, secondTile) <= 0)
        {
            FirstTile = firstTile;
            SecondTile = secondTile;
        }
        else
        {
            FirstTile = secondTile;
            SecondTile = firstTile;
        }
    }

    public GridPosition FirstTile { get; }

    public GridPosition SecondTile { get; }

    public bool Separates(GridPosition firstTile, GridPosition secondTile) =>
        this == new Wall(firstTile, secondTile);

    private static int Compare(GridPosition firstTile, GridPosition secondTile)
    {
        int rowComparison = firstTile.Row.CompareTo(secondTile.Row);
        return rowComparison != 0
            ? rowComparison
            : firstTile.Column.CompareTo(secondTile.Column);
    }
}
