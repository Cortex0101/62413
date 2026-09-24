using System.Windows.Controls;
using System.Windows.Input;

namespace RatMan.App.Views;

public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Focus();
        Keyboard.Focus(this);
    }
}
