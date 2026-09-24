using System.Windows.Input;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class GameViewModel : ScreenViewModel
{
    public const double CellSize = 36;

    private readonly GameSession gameSession;

    public GameViewModel(Action returnToMenu)
        : base("RatMan")
    {
        BoardSize boardSize = new(10, 14);
        gameSession = new GameSession(boardSize, new GridPosition(5, 7));
        BoardCells = CreateBoardCells(boardSize);
        MoveCommand = new RelayCommand(Move);
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu());
    }

    public IReadOnlyList<BoardCellViewModel> BoardCells { get; }

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
