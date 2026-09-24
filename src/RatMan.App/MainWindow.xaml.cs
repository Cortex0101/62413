using System.Windows;
using RatMan.App.ViewModels;

namespace RatMan.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
