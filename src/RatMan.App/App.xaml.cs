using System.Windows;
using RatMan.App.ViewModels;

namespace RatMan.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainViewModel mainViewModel = new(Shutdown);
        MainWindow mainWindow = new(mainViewModel);
        mainWindow.Show();
    }
}
