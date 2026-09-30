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
        MoveCommand = new RelayCommand(Move);
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu());
    }

    public IReadOnlyList<BoardCellViewModel> BoardCells { get; }

    public string MapWarnings { get; init; } = "";

    public IReadOnlyList<WallSegmentViewModel> WallSegments { get; }

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
