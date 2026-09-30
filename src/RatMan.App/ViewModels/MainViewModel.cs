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
    private readonly PlaceholderViewModel highScores;
    private readonly PlaceholderViewModel settings;
    private readonly PlaceholderViewModel about;

    public MainViewModel(Action? exit = null, IMazeRepository? repository = null, IEditorDialogs? dialogs = null)
    {
        this.repository = repository ?? new FileMazeRepository();
        this.dialogs = dialogs ?? new EditorDialogs();
        highScores = new PlaceholderViewModel(
            "High Scores",
            "Score persistence arrives in Week 10.",
            ShowMainMenu);
        settings = new PlaceholderViewModel(
            "Settings",
            "Configurable game options arrive in a later iteration.",
            ShowMainMenu);
        about = new PlaceholderViewModel(
            "About RatMan",
            "A maze-chase course project built with C#, .NET 10, WPF, and MVVM.",
            ShowMainMenu);
        mainMenu = new MainMenuViewModel(
            ShowGame,
            ShowBuilder,
            () => ActiveScreen = highScores,
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

    private void ShowGame()
    {
        string warnings = "";
        MapSelectionViewModel selection = new("Choose a Map", repository,
            (maze, _) => ActiveScreen = new GameViewModel(ShowMainMenu, new GameSession(maze)) { MapWarnings = warnings },
            ShowMainMenu, ShowBuilder);
        ActiveScreen = selection;
        MazeEntry[] playable = selection.Maps.Where(map => map.IsValid).ToArray();
        warnings = string.Join("\n", selection.Maps.Where(map => !map.IsValid).Select(map => "Skipped map: " + map.Description));
        if (playable.Length == 1)
            selection.SelectCommand.Execute(playable[0]);
    }

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
