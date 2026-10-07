using System.Windows.Controls;

namespace RatMan.App.Views;

public partial class HighScoresView : UserControl
{
    public HighScoresView()
    {
        InitializeComponent();
        Loaded += (_, _) => Focus();
    }
}
