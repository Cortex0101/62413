using System.Windows.Input;
using System.Windows.Media;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class GameViewModel : ScreenViewModel
{
    public const double CellSize = BoardProjection.CellSize;
    public const double WallThickness = BoardProjection.WallThickness;
    private readonly GameSession gameSession;
    private int score, lives;
    private string powerText = "", cheeseText = "", playerPosition = "";
    private RoundStatus status;
    private string mapWarnings = "";
    private bool completionNotified, highScoreBusy, highScoreError;
    private string highScoreStatus = "";

    public GameViewModel(Action returnToMenu, GameSession session, Action? showHighScores = null,
        Func<Task>? retryHighScore = null) : base(session.Name)
    {
        gameSession = session;
        Food.Refresh(session.RemainingFood);
        Player = new(session.Player, CellSize);
        Player.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Player.Left)) OnPropertyChanged(nameof(PlayerLeft));
            if (e.PropertyName == nameof(Player.Top)) OnPropertyChanged(nameof(PlayerTop));
            if (e.PropertyName == nameof(Player.CurrentImage)) OnPropertyChanged(nameof(PlayerImage));
        };
        Cats = session.Cats.Select(cat => new CatPresentationViewModel(cat)).ToArray();
        BoardCells = BoardProjection.Cells(session.BoardSize);
        WallSegments = BoardProjection.Segments(session.Walls);
        RenderedWalls = WallSegments.Concat(BoardProjection.Borders(session.BoardSize,
            session.OpenBorderRows, session.OpenBorderColumns)).ToArray();
        BoardProjection.UpdateBlocked(BoardCells, session.BoardSize, session.Walls,
            session.Player.Position, session.OpenBorderRows, session.OpenBorderColumns);
        MoveCommand = new RelayCommand(parameter =>
        {
            if (parameter is not Direction direction) return;
            session.RequestDirection(direction);
            Synchronize(TimeSpan.Zero);
        });
        ReplayCommand = new RelayCommand(_ =>
        {
            if (!session.IsFinished || IsHighScoreBusy) return;
            completionNotified = false;
            HighScoreStatus = "";
            HasHighScoreError = false;
            session.Restart();
            Player.ResetAnimation();
            foreach (var cat in Cats) cat.ResetAnimation();
            Food.Reset();
            Food.Refresh(session.RemainingFood);
            Synchronize(TimeSpan.Zero);
        }, _ => session.IsFinished && !IsHighScoreBusy);
        ReturnToMenuCommand = new RelayCommand(_ => { if (!IsHighScoreBusy) returnToMenu(); }, _ => !IsHighScoreBusy);
        ShowHighScoresCommand = new RelayCommand(_ =>
        {
            if (IsFinished && !IsHighScoreBusy) showHighScores?.Invoke();
        }, _ => IsFinished && !IsHighScoreBusy && showHighScores is not null);
        RetryHighScoreCommand = new AsyncRelayCommand(retryHighScore ?? (() => Task.CompletedTask),
            () => HasHighScoreError && !IsHighScoreBusy && retryHighScore is not null);
        Synchronize(TimeSpan.Zero);
    }

    public IReadOnlyList<BoardCellViewModel> BoardCells { get; }
    public IReadOnlyList<WallSegmentViewModel> WallSegments { get; }
    public IReadOnlyList<WallSegmentViewModel> RenderedWalls { get; }
    public FoodPresentationViewModel Food { get; } = new();
    public PlayerPresentationViewModel Player { get; }
    public IReadOnlyList<CatPresentationViewModel> Cats { get; }
    public int Score { get => score; private set => SetProperty(ref score, value); }
    public int Lives { get => lives; private set => SetProperty(ref lives, value); }
    public string PowerText { get => powerText; private set => SetProperty(ref powerText, value); }
    public string CheeseText { get => cheeseText; private set => SetProperty(ref cheeseText, value); }
    public string PlayerPosition { get => playerPosition; private set => SetProperty(ref playerPosition, value); }
    public bool IsFinished => status is RoundStatus.Won or RoundStatus.GameOver;
    public bool HasOverlay => status != RoundStatus.Playing;
    public string OutcomeTitle => status switch { RoundStatus.Won => "You win!", RoundStatus.GameOver => "Game over", _ => "Caught!" };
    public string OutcomeMessage => IsFinished ? $"Final score: {Score}" : $"{Lives} lives remaining. Get ready…";
    public string MapWarnings
    {
        get => string.Join("\n", new[] { mapWarnings, gameSession.IsMonsterDisconnected
            ? "The cat home is disconnected from Ratman. Cats cannot reach you on this map." : "" }.Where(s => s.Length > 0));
        init => mapWarnings = value;
    }
    public bool HasMonsterSpawn => gameSession.MonsterStartingPosition.HasValue;
    public double BoardWidth => gameSession.BoardSize.Columns * CellSize;
    public double BoardHeight => gameSession.BoardSize.Rows * CellSize;
    public double PlayerLeft => Player.Left;
    public double PlayerTop => Player.Top;
    public ImageSource PlayerImage => Player.CurrentImage;
    public ICommand MoveCommand { get; }
    public RelayCommand ReturnToMenuCommand { get; }
    public RelayCommand ReplayCommand { get; }
    public RelayCommand ShowHighScoresCommand { get; }
    public AsyncRelayCommand RetryHighScoreCommand { get; }
    public event Action<CompletedRound>? RoundCompleted;
    public Task HighScoreCompletion { get; internal set; } = Task.CompletedTask;
    public string HighScoreStatus { get => highScoreStatus; internal set => SetProperty(ref highScoreStatus, value); }
    public bool HasHighScoreError
    {
        get => highScoreError;
        internal set { SetProperty(ref highScoreError, value); RetryHighScoreCommand.RaiseCanExecuteChanged(); }
    }
    public bool IsHighScoreBusy
    {
        get => highScoreBusy;
        internal set
        {
            SetProperty(ref highScoreBusy, value);
            ReplayCommand.RaiseCanExecuteChanged();
            ReturnToMenuCommand.RaiseCanExecuteChanged();
            ShowHighScoresCommand.RaiseCanExecuteChanged();
            RetryHighScoreCommand.RaiseCanExecuteChanged();
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (gameSession.IsFinished) return;
        gameSession.Advance(elapsed);
        if (gameSession.Status == RoundStatus.Playing) Food.Advance(elapsed);
        Synchronize(gameSession.Status == RoundStatus.Playing ? elapsed : TimeSpan.Zero);
    }

    private void Synchronize(TimeSpan elapsed)
    {
        if (Food.Items.Count != gameSession.RemainingFood.Count)
            foreach (var item in Food.Items.Where(item => !gameSession.RemainingFood.ContainsKey(item.Position)).ToArray())
                Food.Remove(item.Position);
        Player.Synchronize(elapsed, gameSession.IsPowered);
        foreach (var cat in Cats) cat.Synchronize(elapsed, gameSession.CollectedCheeseCount);
        Score = gameSession.Score;
        Lives = gameSession.Lives;
        PowerText = gameSession.IsPowered ? $"Steroid power · {Math.Ceiling(gameSession.PowerRemaining.TotalSeconds):0}s" : "Cats are chasing";
        CheeseText = $"Cheese: {gameSession.CollectedCheeseCount} / {gameSession.InitialCheeseCount}";
        PlayerPosition = $"Row {gameSession.Player.Position.Row + 1}, Column {gameSession.Player.Position.Column + 1}";
        if (status != gameSession.Status)
        {
            status = gameSession.Status;
            OnPropertyChanged(nameof(IsFinished));
            OnPropertyChanged(nameof(HasOverlay));
            OnPropertyChanged(nameof(OutcomeTitle));
            OnPropertyChanged(nameof(OutcomeMessage));
            ReplayCommand.RaiseCanExecuteChanged();
            ShowHighScoresCommand.RaiseCanExecuteChanged();
        }
        if (IsFinished && !completionNotified)
        {
            completionNotified = true;
            RoundCompleted?.Invoke(new(Guid.NewGuid(), Score, DateTimeOffset.UtcNow, status));
        }
    }
}

public sealed record CompletedRound(Guid Id, int Score, DateTimeOffset CompletedAt, RoundStatus Outcome);
