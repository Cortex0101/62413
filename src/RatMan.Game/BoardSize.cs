namespace RatMan.Game;

public sealed record BoardSize
{
    public BoardSize(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);

        Rows = rows;
        Columns = columns;
    }

    public int Rows { get; }

    public int Columns { get; }

    public bool Contains(GridPosition position) =>
        position.Row >= 0 &&
        position.Row < Rows &&
        position.Column >= 0 &&
        position.Column < Columns;
}
