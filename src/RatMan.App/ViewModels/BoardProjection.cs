using RatMan.Game;

namespace RatMan.App.ViewModels;

public static class BoardProjection
{
    public const double CellSize = 36;
    public const double WallThickness = 4;

    public static IReadOnlyList<BoardCellViewModel> Cells(BoardSize size) =>
        Enumerable.Range(0, size.Rows).SelectMany(row => Enumerable.Range(0, size.Columns)
            .Select(column => new BoardCellViewModel(column * CellSize, row * CellSize))).ToArray();

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
