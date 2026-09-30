namespace RatMan.Game;

public static class ClassicMaze
{
    public static BoardSize Size { get; } = new(11, 12);

    public static GridPosition StartingPosition { get; } = new(8, 4);

    public static GameSession CreateSession() => new(Size, StartingPosition, CreateWalls());

    private static IReadOnlySet<Wall> CreateWalls()
    {
        HashSet<Wall> walls = [];

        // Coordinates describe grid lines, not occupied tiles. Mirror each
        // segment across the board to keep the arcade-inspired maze balanced.
        void AddMirrored(Wall wall)
        {
            walls.Add(wall);
            walls.Add(new Wall(
                new GridPosition(wall.FirstTile.Row, Size.Columns - 1 - wall.FirstTile.Column),
                new GridPosition(wall.SecondTile.Row, Size.Columns - 1 - wall.SecondTile.Column)));
        }

        void Horizontal(int boundaryRow, int startColumn, int endColumn)
        {
            for (int column = startColumn; column < endColumn; column++)
            {
                AddMirrored(new Wall(
                    new GridPosition(boundaryRow - 1, column),
                    new GridPosition(boundaryRow, column)));
            }
        }

        void Vertical(int boundaryColumn, int startRow, int endRow)
        {
            for (int row = startRow; row < endRow; row++)
            {
                AddMirrored(new Wall(
                    new GridPosition(row, boundaryColumn - 1),
                    new GridPosition(row, boundaryColumn)));
            }
        }

        // Current board below is incomplete and in progress before adding the board builder
        
        // Corner boxes
        Vertical(1, 1, 2);
        Vertical(2, 1, 2);
        Horizontal(1, 1, 2);
        Horizontal(2, 1, 2);

        //Vertical(1, 1, 2);
        //Vertical(2, 1, 2);
        //Horizontal(1, 1, 2);
        //Horizontal(2, 1, 2);

        Vertical(3, 1, 2);
        Vertical(5, 1, 2);
        Horizontal(1, 3, 5);
        Horizontal(2, 3, 5);

        Vertical(6, 0, 2);

        Horizontal(3, 1, 2);

        Horizontal(3, 5, 6);
        Vertical(6, 3, 4);

        Vertical(3, 3, 5);
        Horizontal(4, 3, 4);

        /*
        // Upper lanes and the central divider.
        Vertical(5, 1, 2);
        Horizontal(2, 1, 4);
        Horizontal(3, 0, 2);
        Vertical(2, 2, 4);

        // Central enclosure, with a two-cell entrance at the top.
        Horizontal(4, 3, 4);
        Vertical(3, 4, 6);
        Horizontal(6, 3, 5);
        Horizontal(5, 0, 2);

        // Lower T-junctions and winding routes around the player start.
        Vertical(5, 7, 8);
        Horizontal(8, 2, 5);
        Vertical(2, 9, 11);
        Horizontal(10, 3, 4);
        Horizontal(10, 0, 1);
        Vertical(5, 9, 11);
        */

        return walls;
    }
}
