using System.Windows;
using RatMan.App.ViewModels;

namespace RatMan.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainViewModel mainViewModel = new(Shutdown);
        await mainViewModel.SettingsLoaded;
        MainWindow mainWindow = new(mainViewModel);
        mainWindow.Show();
    }
}
