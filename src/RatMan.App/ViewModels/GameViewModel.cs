using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media;
using RatMan.App.Animation;
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
        Player = new PlayerPresentationViewModel(
            gameSession.Player.Position,
            CellSize,
            RatManSpriteSheet.CreateAnimator());
        Player.PropertyChanged += OnPlayerPropertyChanged;
        BoardCells = CreateBoardCells(boardSize);
        WallSegments = CreateWallSegments(gameSession.Walls);
        MoveCommand = new RelayCommand(Move);
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu());
    }

    public IReadOnlyList<BoardCellViewModel> BoardCells { get; }

    public IReadOnlyList<WallSegmentViewModel> WallSegments { get; }

    public PlayerPresentationViewModel Player { get; }

    public ICommand MoveCommand { get; }

    public ICommand ReturnToMenuCommand { get; }

    public double BoardWidth => gameSession.BoardSize.Columns * CellSize;

    public double BoardHeight => gameSession.BoardSize.Rows * CellSize;

    public double PlayerLeft => Player.Left;

    public double PlayerTop => Player.Top;

    public ImageSource PlayerImage => Player.CurrentImage;

    public string PlayerPosition =>
        $"Row {gameSession.Player.Position.Row + 1}, Column {gameSession.Player.Position.Column + 1}";

    public void Advance(TimeSpan elapsed)
    {
        bool wasWalking = Player.AnimationState == PlayerAnimationState.Walking;

        if (!Player.Advance(elapsed))
        {
            return;
        }

        if (Player.TryTakeQueuedDirection(out Direction queuedDirection))
        {
            Player.SetIdle();
            TryStartMove(queuedDirection);
        }
        else if (wasWalking)
        {
            Direction direction = Player.FacingDirection;
            Player.SetIdle();
            TryStartMove(direction);
        }
    }

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
        if (parameter is not Direction direction)
        {
            return;
        }

        if (Player.IsBusy)
        {
            Player.QueueDirection(direction);
            return;
        }

        TryStartMove(direction);
    }

    private void TryStartMove(Direction direction)
    {
        if (!gameSession.TryMovePlayer(direction))
        {
            Player.ShowBlocked(direction);
            return;
        }

        Player.BeginMove(gameSession.Player.Position, direction);
        OnPropertyChanged(nameof(PlayerPosition));
    }

    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerPresentationViewModel.Left))
        {
            OnPropertyChanged(nameof(PlayerLeft));
        }
        else if (e.PropertyName == nameof(PlayerPresentationViewModel.Top))
        {
            OnPropertyChanged(nameof(PlayerTop));
        }
        else if (e.PropertyName == nameof(PlayerPresentationViewModel.CurrentImage))
        {
            OnPropertyChanged(nameof(PlayerImage));
        }
    }
}
