using RatMan.App.Mvvm;
using RatMan.App.Persistence;
using RatMan.App.Services;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private object activeScreen;
    private readonly MainMenuViewModel mainMenu;
    private readonly IMazeRepository repository;
    private readonly IEditorDialogs dialogs;
    private readonly IHighScoreRepository highScores;
    private readonly IHighScoreDialogs highScoreDialogs;
    private HighScoreLayout? lastPlayedLayout;
    private readonly SettingsViewModel settings;
    private readonly PlaceholderViewModel about;

    public MainViewModel(Action? exit = null, IMazeRepository? repository = null, IEditorDialogs? dialogs = null,
        IHighScoreRepository? highScores = null, IHighScoreDialogs? highScoreDialogs = null,
        ISettingsRepository? settingsRepository = null)
    {
        this.repository = repository ?? new FileMazeRepository();
        this.dialogs = dialogs ?? new EditorDialogs();
        this.highScores = highScores ?? new FileHighScoreRepository();
        this.highScoreDialogs = highScoreDialogs ?? new HighScoreDialogs();
        settings = new SettingsViewModel(settingsRepository ?? new FileSettingsRepository(), ShowMainMenu);
        about = new PlaceholderViewModel(
            "About RatMan",
            "A maze-chase course project built with C#, .NET 10, WPF, and MVVM.",
            ShowMainMenu);
        mainMenu = new MainMenuViewModel(
            ShowGame,
            ShowBuilder,
            () => ShowHighScores(lastPlayedLayout),
            () => ActiveScreen = settings,
            () => ActiveScreen = about,
            exit ?? (() => { }));
        activeScreen = mainMenu;
    }

    public object ActiveScreen
    {
        get => activeScreen;
        private set => SetProperty(ref activeScreen, value);
    }

    internal Task SettingsLoaded => settings.Loading;

    private void ShowGame()
    {
        string warnings = "";
        MapSelectionViewModel selection = new("Choose a Map", repository,
            (maze, _) => StartGame(maze, warnings),
            ShowMainMenu, ShowBuilder);
        ActiveScreen = selection;
        MazeEntry[] playable = selection.Maps.Where(map => map.IsValid).ToArray();
        warnings = string.Join("\n", selection.Maps.Where(map => !map.IsValid).Select(map => "Skipped map: " + map.Description));
        if (playable.Length == 1)
            selection.SelectCommand.Execute(playable[0]);
    }

    private void StartGame(MazeDefinition maze, string warnings)
    {
        var layout = HighScoreLayout.FromMaze(maze);
        lastPlayedLayout = layout;
        CompletedRound? completed = null;
        GameViewModel? game = null;
        game = new GameViewModel(ShowMainMenu, new GameSession(maze), () => ShowHighScores(layout),
            () => completed is null ? Task.CompletedTask : CheckHighScoreAsync(game!, layout, completed),
            settings.Rows.Select(row => row.Binding).ToArray()) { MapWarnings = warnings };
        game.RoundCompleted += result =>
        {
            completed = result;
            game.HighScoreCompletion = CheckHighScoreAsync(game, layout, result);
        };
        ActiveScreen = game;
    }

    private async Task CheckHighScoreAsync(GameViewModel game, HighScoreLayout layout, CompletedRound result)
    {
        if (game.IsHighScoreBusy || !ReferenceEquals(ActiveScreen, game)) return;
        game.IsHighScoreBusy = true;
        game.HasHighScoreError = false;
        game.HighScoreStatus = "Checking high scores…";
        try
        {
            var boards = await highScores.LoadAsync();
            var entries = boards.FirstOrDefault(b => b.Layout.Id == layout.Id)?.Entries ?? [];
            int? rank = HighScoreRules.QualifyingRank(entries, result.Score);
            if (rank is null)
            {
                game.HighScoreStatus = "This score did not reach the top 10.";
                return;
            }
            if (!ReferenceEquals(ActiveScreen, game)) return;
            var entry = new HighScoreEntryViewModel(highScores, layout,
                new(result.Id, "", result.Score, result.CompletedAt), rank.Value);
            await highScoreDialogs.ShowAsync(entry);
            game.HighScoreStatus = entry.Saved ? "Your high score was saved!" : "Score not saved.";
        }
        catch (Exception exception) when (FileHighScoreRepository.IsStorageError(exception))
        {
            game.HasHighScoreError = true;
            game.HighScoreStatus = "Could not check high scores. Retry or continue without saving. " + exception.Message;
        }
        finally { game.IsHighScoreBusy = false; }
    }

    private void ShowHighScores(HighScoreLayout? layout) =>
        ActiveScreen = new HighScoresViewModel(highScores, repository, ShowMainMenu, layout);

    private void ShowBuilder()
    {
        BoardBuilderViewModel? builder = null;
        builder = new BoardBuilderViewModel(repository, dialogs, ShowMainMenu, () =>
        {
            ActiveScreen = new MapSelectionViewModel("Load Board", repository,
                (maze, path) => { builder!.Load(maze, path); ActiveScreen = builder; },
                () => ActiveScreen = builder!, () => ActiveScreen = builder!);
        });
        ActiveScreen = builder;
    }

    private void ShowMainMenu() => ActiveScreen = mainMenu;
}
