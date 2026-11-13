using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RatMan.App.ViewModels;

namespace RatMan.App.Views;

public partial class SettingsView : UserControl
{
    private const string BindingInstructions = "Select a binding to change its key.";
    private Button? captureButton;

    public SettingsView()
    {
        InitializeComponent();
        BindingHint.Text = BindingInstructions;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Keyboard.Focus(this);

    private void OnBindingClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel { IsSaving: false } ||
            sender is not Button { Tag: Key } button || button == captureButton) return;

        EndCapture();
        if (!button.Focus()) return;
        captureButton = button;
        BindingHint.Text = "Press a key. Esc cancels.";
    }

    private void OnBindingLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender == captureButton) EndCapture();
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat && (e.Key == Key.Escape ||
            (e.Key is Key.Return or Key.Space && Keyboard.FocusedElement is Button { Tag: Key })))
        {
            e.Handled = true;
            return;
        }

        if (captureButton?.Tag is not Key previousKey || DataContext is not SettingsViewModel viewModel) return;

        // Let Tab leave the button so the user can continue navigating the settings.
        if (e.Key == Key.Tab) return;
        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            EndCapture();
            return;
        }

        if (e.IsRepeat || Keyboard.Modifiers != ModifierKeys.None || !MovementBinding.CanBind(e.Key)) return;
        EndCapture();
        await viewModel.RebindAsync(previousKey, e.Key);
    }

    private void EndCapture()
    {
        captureButton = null;
        BindingHint.Text = BindingInstructions;
    }
}
