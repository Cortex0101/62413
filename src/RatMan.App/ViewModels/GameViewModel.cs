using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media;
using RatMan.App.Animation;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class GameViewModel : ScreenViewModel
{
    public const double CellSize = BoardProjection.CellSize;
    public const double WallThickness = BoardProjection.WallThickness;

    private readonly GameSession gameSession;

    public GameViewModel(Action returnToMenu, GameSession session)
        : base(session.Name)
    {
        gameSession = session;
        Player = new PlayerPresentationViewModel(
            gameSession.Player.Position,
            CellSize,
            RatManSpriteSheet.CreateAnimator());
        Player.PropertyChanged += OnPlayerPropertyChanged;
        BoardCells = BoardProjection.Cells(gameSession.BoardSize);
        WallSegments = BoardProjection.Segments(gameSession.Walls);
        RenderedWalls = WallSegments.Concat(BoardProjection.Borders(gameSession.BoardSize,
            gameSession.OpenBorderRows, gameSession.OpenBorderColumns)).ToArray();
        BoardProjection.UpdateBlocked(BoardCells, gameSession.BoardSize, gameSession.Walls,
            gameSession.Player.Position, gameSession.OpenBorderRows, gameSession.OpenBorderColumns);
        MoveCommand = new RelayCommand(Move);
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu());
    }

    public IReadOnlyList<BoardCellViewModel> BoardCells { get; }

    public string MapWarnings { get; init; } = "";

    public IReadOnlyList<WallSegmentViewModel> WallSegments { get; }
    public IReadOnlyList<WallSegmentViewModel> RenderedWalls { get; }

    public bool HasMonsterSpawn => gameSession.MonsterStartingPosition.HasValue;
    public double MonsterLeft => (gameSession.MonsterStartingPosition?.Column ?? 0) * CellSize;
    public double MonsterTop => (gameSession.MonsterStartingPosition?.Row ?? 0) * CellSize;

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

        Player.SetIdle();

        if (TryStartQueuedMove())
        {
            return;
        }

        if (wasWalking && !TryStartMove(Player.FacingDirection))
        {
            Player.ShowBlocked(Player.FacingDirection);
        }
    }

    private void Move(object? parameter)
    {
        if (parameter is not Direction direction)
        {
            return;
        }

        Player.QueueDirection(direction);

        if (!Player.IsBusy)
        {
            TryStartQueuedMove();
        }
    }

    private bool TryStartQueuedMove()
    {
        if (!Player.TryTakeQueuedDirection(out Direction direction))
        {
            return false;
        }

        if (TryStartMove(direction))
        {
            return true;
        }

        // Keep a blocked turn pending until the player reaches an opening.
        Player.QueueDirection(direction);
        return false;
    }

    private bool TryStartMove(Direction direction)
    {
        bool wraps = !gameSession.BoardSize.Contains(gameSession.Player.Position.AdjacentTo(direction));
        if (!gameSession.TryMovePlayer(direction))
        {
            return false;
        }

        Player.BeginMove(gameSession.Player.Position, direction, wraps);
        OnPropertyChanged(nameof(PlayerPosition));
        return true;
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
