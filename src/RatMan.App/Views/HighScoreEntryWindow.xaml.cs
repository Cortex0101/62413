using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using RatMan.App.ViewModels;

namespace RatMan.App.Views;

public partial class HighScoreEntryWindow : Window
{
    private readonly HighScoreEntryViewModel entry;

    public HighScoreEntryWindow(HighScoreEntryViewModel entry)
    {
        InitializeComponent();
        this.entry = entry;
        DataContext = entry;
        entry.CloseRequested += Close;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NameInput.Focus();
        Keyboard.Focus(NameInput);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // A completed atomic write determines whether a submission was saved before dismissal is allowed.
        if (entry.IsSaving && !entry.Saved) e.Cancel = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        entry.CloseRequested -= Close;
        entry.Dismiss();
    }
}
