using RatMan.Game;

namespace RatMan.App.ViewModels;

public static class BoardProjection
{
    public const double CellSize = 36;
    public const double WallThickness = 4;

    public static IReadOnlyList<BoardCellViewModel> Cells(BoardSize size) =>
        Enumerable.Range(0, size.Rows).SelectMany(row => Enumerable.Range(0, size.Columns)
            .Select(column => new BoardCellViewModel(column * CellSize, row * CellSize))).ToArray();

    public static void UpdateBlocked(IReadOnlyList<BoardCellViewModel> cells, BoardSize size,
        IEnumerable<Wall> walls, GridPosition? player, IEnumerable<int> rows, IEnumerable<int> columns)
    {
        var reachable = player is { } start ? new BoardTopology(size, walls, rows, columns).ReachableFrom(start) : null;
        for (int i = 0; i < cells.Count; i++)
            cells[i].IsBlocked = reachable is not null && !reachable.Contains(new(i / size.Columns, i % size.Columns));
    }

    public static IReadOnlyList<WallSegmentViewModel> Borders(BoardSize size,
        IReadOnlyCollection<int> rows, IReadOnlyCollection<int> columns)
    {
        List<WallSegmentViewModel> result = [];
        for (int row = 0; row < size.Rows; row++)
            if (!rows.Contains(row))
            {
                result.Add(Segment(new(new(row, -1), new(row, 0))));
                result.Add(Segment(new(new(row, size.Columns - 1), new(row, size.Columns))));
            }
        for (int column = 0; column < size.Columns; column++)
            if (!columns.Contains(column))
            {
                result.Add(Segment(new(new(-1, column), new(0, column))));
                result.Add(Segment(new(new(size.Rows - 1, column), new(size.Rows, column))));
            }
        return result;
    }

    public static WallSegmentViewModel Segment(Wall wall)
    {
        double offset = WallThickness / 2;
        return wall.FirstTile.Row == wall.SecondTile.Row
            ? new(wall.SecondTile.Column * CellSize - offset, wall.FirstTile.Row * CellSize - offset,
                WallThickness, CellSize + WallThickness)
            : new(wall.FirstTile.Column * CellSize - offset, wall.SecondTile.Row * CellSize - offset,
                CellSize + WallThickness, WallThickness);
    }

    public static IReadOnlyList<WallSegmentViewModel> Segments(IEnumerable<Wall> walls) => walls.Select(Segment).ToArray();
}
