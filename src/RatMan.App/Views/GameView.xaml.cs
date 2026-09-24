using System.Windows.Controls;
using System.Windows.Input;
using RatMan.App.Animation;
using RatMan.App.ViewModels;

namespace RatMan.App.Views;

public partial class GameView : UserControl
{
    private RenderingClock? renderingClock;

    public GameView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        renderingClock?.Dispose();
        renderingClock = DataContext is GameViewModel viewModel
            ? new RenderingClock(viewModel.Advance)
            : null;
        renderingClock?.Start();

        Focus();
        Keyboard.Focus(this);
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        renderingClock?.Dispose();
        renderingClock = null;
    }
}
