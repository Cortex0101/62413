using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RatMan.App.Animation;
using RatMan.App.ViewModels;
using RatMan.App.Views;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class FoodViewTests
{
    [TestMethod]
    public void Views_RenderFoodScoreAndPreview_AndReleaseRenderingClocks()
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            Application? app = null;
            try
            {
                app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                foreach (string resource in new[] { "Theme", "Controls" })
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"/RatMan;component/Resources/{resource}.xaml", UriKind.Relative)
                    });
                MazeDefinition maze = new("Cheese garden", new(8, 10),
                    [new(new(2, 2), new(2, 3)), new(new(3, 2), new(3, 3)), new(new(5, 6), new(6, 6))],
                    new(4, 2), new(1, 6))
                {
                    EmptyFoodTiles = [new(3, 1), new(3, 2)],
                    SteroidCheeseTiles = [new(4, 3), new(1, 6), new(6, 8)]
                };
                var game = new GameViewModel(() => { }, new GameSession(maze));
                var gameView = new GameView { DataContext = game };
                Render(gameView, "game-before");
                var layer = Descendants(gameView).OfType<ItemsControl>().Single(control => ReferenceEquals(control.ItemsSource, game.Food.Items));
                Assert.AreEqual(77, Descendants(layer).OfType<Image>().Count(image => image.Source is not null));
                Assert.AreEqual(3, Descendants(layer).OfType<Image>().Count(image =>
                    image.DataContext is FoodItemViewModel { Kind: FoodKind.Steroid } && ReferenceEquals(image.Source, game.Food.Animator.CurrentImage)));
                game.MoveCommand.Execute(Direction.Right);
                game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
                Render(gameView, "game-halfway");
                Assert.IsTrue(Descendants(gameView).OfType<TextBlock>().Any(text => text.Text == "0"));
                game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
                Render(gameView, "game-collected");
                Assert.AreEqual(76, Descendants(layer).OfType<Image>().Count(image => image.Source is not null));
                Assert.IsTrue(Descendants(gameView).OfType<TextBlock>().Any(text => text.Text == "5"));
                VerifyClockLifecycle(gameView);

                var editor = new BoardBuilderViewModel(new MemoryMazeRepository(), new TestEditorDialogs(), () => { }, () => { });
                editor.Load(maze, "preview.maze");
                var editorView = new BoardBuilderView { DataContext = editor };
                editor.SelectedTool = BuilderTool.SteroidCheese;
                editor.Preview(162, 234);
                editor.Advance(TimeSpan.FromMilliseconds(300));
                Render(editorView, "editor-steroid-preview");
                Assert.IsTrue(Descendants(editorView).OfType<Image>().Any(image =>
                    ReferenceEquals(image.DataContext, editor) && image.Visibility == Visibility.Visible && image.Source is not null));
                VerifyClockLifecycle(editorView);
            }
            catch (Exception exception) { failure = exception; }
            finally { app?.Shutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "WPF rendering timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void VerifyClockLifecycle(UserControl view)
    {
        var field = view.GetType().GetField("renderingClock", BindingFlags.Instance | BindingFlags.NonPublic)!;
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        var first = (RenderingClock)field.GetValue(view)!;
        Assert.IsTrue(first.IsRunning);
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        var second = (RenderingClock)field.GetValue(view)!;
        Assert.IsFalse(first.IsRunning);
        Assert.IsTrue(second.IsRunning);
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Assert.IsFalse(second.IsRunning);
        Assert.IsNull(field.GetValue(view));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Render(FrameworkElement view, string name)
    {
        // Match the main window's inherited colors in an offscreen render.
        view.SetResourceReference(Control.ForegroundProperty, "PrimaryTextBrush");
        view.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundBrush");
        view.Measure(new Size(720, 880));
        view.Arrange(new Rect(0, 0, 720, 880));
        view.UpdateLayout();
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
        var bitmap = new RenderTargetBitmap(720, 880, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string directory = Path.Combine(AppContext.BaseDirectory, "visual-checks");
        Directory.CreateDirectory(directory);
        using var output = File.Create(Path.Combine(directory, name + ".png"));
        encoder.Save(output);
    }
}
