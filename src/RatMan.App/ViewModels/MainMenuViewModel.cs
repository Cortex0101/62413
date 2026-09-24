using System.Windows.Input;
using RatMan.App.Mvvm;

namespace RatMan.App.ViewModels;

public sealed class MainMenuViewModel : ScreenViewModel
{
    public MainMenuViewModel(
        Action startGame,
        Action showHighScores,
        Action showSettings,
        Action showAbout,
        Action exit)
        : base("RatMan")
    {
        StartGameCommand = new RelayCommand(_ => startGame());
        HighScoresCommand = new RelayCommand(_ => showHighScores());
        SettingsCommand = new RelayCommand(_ => showSettings());
        AboutCommand = new RelayCommand(_ => showAbout());
        ExitCommand = new RelayCommand(_ => exit());
    }

    public ICommand StartGameCommand { get; }

    public ICommand HighScoresCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand AboutCommand { get; }

    public ICommand ExitCommand { get; }
}
