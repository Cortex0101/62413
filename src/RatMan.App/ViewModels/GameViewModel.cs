using System.Windows.Input;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class GameViewModel : ScreenViewModel
{
    public const double CellSize = 36;
    public const double WallThickness = 4;

    private readonly GameSession gameSession;

    public GameViewModel(Action returnToMenu)
        : base("RatMan")
    {
        BoardSize boardSize = new(16, 8);
        gameSession = new GameSession(boardSize, new GridPosition(1, 1), CreateWalls());
        BoardCells = CreateBoardCells(boardSize);
        WallSegments = CreateWallSegments(gameSession.Walls);
        MoveCommand = new RelayCommand(Move);
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu());
    }

    public IReadOnlyList<BoardCellViewModel> BoardCells { get; }

    public IReadOnlyList<WallSegmentViewModel> WallSegments { get; }

    public ICommand MoveCommand { get; }

    public ICommand ReturnToMenuCommand { get; }

    public double BoardWidth => gameSession.BoardSize.Columns * CellSize;

    public double BoardHeight => gameSession.BoardSize.Rows * CellSize;

    public double PlayerLeft => gameSession.Player.Position.Column * CellSize;

    public double PlayerTop => gameSession.Player.Position.Row * CellSize;

    public string PlayerPosition =>
        $"Row {gameSession.Player.Position.Row + 1}, Column {gameSession.Player.Position.Column + 1}";

    private static IReadOnlyList<BoardCellViewModel> CreateBoardCells(BoardSize boardSize)
    {
        List<BoardCellViewModel> cells = new(boardSize.Rows * boardSize.Columns);

        for (int row = 0; row < boardSize.Rows; row++)
        {
            for (int column = 0; column < boardSize.Columns; column++)
            {
                cells.Add(new BoardCellViewModel(column * CellSize, row * CellSize));
            }
        }

        return cells;
    }

    private static IReadOnlyList<Wall> CreateWalls() =>
    [
        new(new GridPosition(0, 3), new GridPosition(1, 3)),
        new(new GridPosition(1, 5), new GridPosition(1, 6)),
        new(new GridPosition(3, 1), new GridPosition(3, 2)),
        new(new GridPosition(3, 2), new GridPosition(4, 2)),
        new(new GridPosition(5, 4), new GridPosition(5, 5)),
        new(new GridPosition(5, 5), new GridPosition(6, 5)),
        new(new GridPosition(6, 6), new GridPosition(6, 7))
    ];

    private static IReadOnlyList<WallSegmentViewModel> CreateWallSegments(IEnumerable<Wall> walls)
    {
        double offset = WallThickness / 2;

        return walls.Select(wall => wall.FirstTile.Row == wall.SecondTile.Row
            ? new WallSegmentViewModel(
                wall.SecondTile.Column * CellSize - offset,
                wall.FirstTile.Row * CellSize,
                WallThickness,
                CellSize)
            : new WallSegmentViewModel(
                wall.FirstTile.Column * CellSize,
                wall.SecondTile.Row * CellSize - offset,
                CellSize,
                WallThickness))
            .ToArray();
    }

    private void Move(object? parameter)
    {
        if (parameter is not Direction direction || !gameSession.TryMovePlayer(direction))
        {
            return;
        }

        OnPropertyChanged(nameof(PlayerLeft));
        OnPropertyChanged(nameof(PlayerTop));
        OnPropertyChanged(nameof(PlayerPosition));
    }
}
