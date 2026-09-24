using RatMan.App.Mvvm;

namespace RatMan.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private object activeScreen;
    private readonly MainMenuViewModel mainMenu;
    private readonly GameViewModel game;
    private readonly PlaceholderViewModel highScores;
    private readonly PlaceholderViewModel settings;
    private readonly PlaceholderViewModel about;

    public MainViewModel(Action? exit = null)
    {
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
        game = new GameViewModel(ShowMainMenu);
        mainMenu = new MainMenuViewModel(
            ShowGame,
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

    private void ShowGame() => ActiveScreen = game;

    private void ShowMainMenu() => ActiveScreen = mainMenu;
}
