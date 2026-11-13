using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RatMan.App;
using RatMan.App.Persistence;
using RatMan.App.ViewModels;
using RatMan.App.Views;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class SettingsTests
{
    [TestMethod]
    public async Task SettingsFile_UsesDefaultsWhenMissing_AndRestoresCustomKeys()
    {
        string path = Path.Combine(Path.GetTempPath(), $"RatMan-settings-{Guid.NewGuid():N}.json");
        FileSettingsRepository repository = new(path);
        try
        {
            MovementBinding[] defaults = (await repository.LoadAsync()).ToArray();
            CollectionAssert.AreEqual(new[] { Key.W, Key.S, Key.A, Key.D }, defaults.Select(row => row.FirstKey).ToArray());
            CollectionAssert.AreEqual(new[] { Key.Up, Key.Down, Key.Left, Key.Right }, defaults.Select(row => row.SecondKey).ToArray());
            Assert.IsFalse(File.Exists(path));

            defaults[0] = defaults[0] with { FirstKey = Key.I };
            await repository.SaveAsync(defaults);
            CollectionAssert.AreEqual(defaults, (await new FileSettingsRepository(path).LoadAsync()).ToArray());

            await File.WriteAllTextAsync(path, "{invalid json");
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => repository.LoadAsync());
        }
        finally { File.Delete(path); }
    }

    // Shares FoodViewTests' single Application/STA lifetime.
    internal static void VerifyAndRender()
    {
        var repository = new MemorySettingsRepository();
        var app = new MainViewModel(repository: new MemoryMazeRepository(TestMazes.Sample), settingsRepository: repository);
        var menu = (MainMenuViewModel)app.ActiveScreen;
        menu.SettingsCommand.Execute(null);
        var settings = (SettingsViewModel)app.ActiveScreen;
        var view = new SettingsView { DataContext = settings };
        FoodViewTests.Render(view, "settings", 620, 460);

        var window = new Window { Content = view, Width = 620, Height = 500, Opacity = 0, ShowInTaskbar = false };
        try
        {
            window.Show();
            var buttons = Descendants(view).OfType<Button>().Where(button => button.Tag is Key).ToArray();
            Assert.AreEqual(8, buttons.Length);
            var down = buttons.Single(button => Equals(button.Tag, Key.S));
            var right = buttons.Single(button => Equals(button.Tag, Key.D));
            var hint = (TextBlock)view.FindName("BindingHint");

            Click(right);
            Assert.AreEqual("Press a key. Esc cancels.", hint.Text);
            Assert.IsTrue(Press(right, Key.Escape));
            Assert.AreEqual("Select a binding to change its key.", hint.Text);
            Assert.AreEqual("D", right.Content);
            Assert.AreEqual(0, repository.Saves);

            Click(right);
            Assert.IsTrue(Press(right, Key.S));
            view.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            Assert.AreEqual("S", right.Content);
            Assert.AreEqual("D", down.Content);
            Click(right);
            Assert.IsTrue(Press(right, Key.L));
            view.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            Assert.AreEqual("L", right.Content);
            Assert.AreEqual(2, repository.Saves);

            settings.ReturnToMenuCommand.Execute(null);
            menu.StartGameCommand.Execute(null);
            var game = (GameViewModel)app.ActiveScreen;
            var gameView = new GameView { DataContext = game };
            window.Content = gameView;
            window.UpdateLayout();
            Assert.IsFalse(Press(gameView, Key.S));
            Assert.IsTrue(Press(gameView, Key.L));
            Assert.AreEqual(Direction.Right, game.Player.FacingDirection);
            Assert.IsTrue(Press(gameView, Key.Right));
        }
        finally { window.Close(); }
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static bool Press(FrameworkElement target, Key key)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), 0, key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent
        };
        target.RaiseEvent(args);
        return args.Handled;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class MemorySettingsRepository : ISettingsRepository
    {
        public int Saves { get; private set; }

        public Task<IReadOnlyList<MovementBinding>> LoadAsync() => Task.FromResult(MovementBinding.Defaults);

        public Task SaveAsync(IReadOnlyList<MovementBinding> bindings)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }
}
