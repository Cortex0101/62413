using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RatMan.App.ViewModels;
using RatMan.App.Views;

namespace RatMan.App.Services;

public interface IHighScoreDialogs
{
    Task ShowAsync(HighScoreEntryViewModel entry);
}

public sealed class HighScoreDialogs : IHighScoreDialogs
{
    public async Task ShowAsync(HighScoreEntryViewModel entry)
    {
        var dispatcher = Application.Current.Dispatcher;
        // A modal window must never open inside CompositionTarget.Rendering.
        await dispatcher.InvokeAsync(() =>
        {
            var focus = Keyboard.FocusedElement;
            var window = new HighScoreEntryWindow(entry) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
            if (focus is not null) Keyboard.Focus(focus);
        }, DispatcherPriority.Background);
    }
}
