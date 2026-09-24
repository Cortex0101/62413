using System.Windows.Input;
using RatMan.App.Mvvm;

namespace RatMan.App.ViewModels;

public sealed class PlaceholderViewModel : ScreenViewModel
{
    public PlaceholderViewModel(string title, string message, Action returnToMenu)
        : base(title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Message = message;
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu());
    }

    public string Message { get; }

    public ICommand ReturnToMenuCommand { get; }
}
