using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RatMan.App.Persistence;
using RatMan.App.Services;
using RatMan.App.ViewModels;
using RatMan.App.Views;

namespace RatMan.Tests;

// Shares FoodViewTests' single Application/STA lifetime.
internal static class HighScoreViewTests
{
    public static void VerifyAndRender()
    {
        var layout = HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze);
        var scores = new MemoryHighScoreRepository();
        scores.Boards.Add(new(layout, Enumerable.Range(1, 10).Select(i =>
            new HighScoreEntry(Guid.NewGuid(), i == 1 ? "Alexandria The Great Rat" : i == 2 ? "Anonymous" : $"Player {i}",
                1234567890 - i * 100, DateTimeOffset.Now.AddMinutes(-i))).ToArray()));
        var screen = new HighScoresViewModel(scores, new MemoryMazeRepository(HighScoreTestSupport.WinMaze), () => { });
        screen.Loading.GetAwaiter().GetResult();
        var view = new HighScoresView { DataContext = screen };
        FoodViewTests.Render(view, "highscores-full");
        Assert.AreEqual(10, Descendants(view).OfType<TextBlock>().Count(t => t.DataContext is HighScoreRow &&
            t.Text == ((HighScoreRow)t.DataContext).Name));
        FoodViewTests.Render(view, "highscores-minimum", 620, 460);
        var picker = Descendants(view).OfType<ComboBox>().Single();
        Assert.AreSame(screen.SelectedLayout, picker.SelectedItem);
        Assert.IsTrue(Descendants(picker).OfType<TextBlock>().Any(t => t.Text == screen.SelectedLayout!.Label));
        Assert.IsTrue(Descendants(view).OfType<ScrollViewer>().Any(s => s.ScrollableHeight > 0));
        Assert.IsTrue(Descendants(view).OfType<Button>().All(b => b.ActualWidth > 0 && b.ActualHeight > 0));

        var empty = new HighScoresViewModel(new MemoryHighScoreRepository(), new MemoryMazeRepository(HighScoreTestSupport.WinMaze), () => { });
        empty.Loading.GetAwaiter().GetResult();
        var emptyView = new HighScoresView { DataContext = empty };
        FoodViewTests.Render(emptyView, "highscores-empty", 620, 460);
        Assert.IsTrue(Descendants(emptyView).OfType<TextBlock>().Any(t => t.Text.Contains("No scores for this layout")));

        var entry = new HighScoreEntryViewModel(new MemoryHighScoreRepository(), layout,
            new(Guid.NewGuid(), "", 1234, DateTimeOffset.Now), 2) { Name = "Alexandria The Great Rat" };
        var dialog = new HighScoreEntryWindow(entry);
        var content = (FrameworkElement)dialog.Content;
        FoodViewTests.Render(content, "highscore-dialog", 464, 390);
        var name = Descendants(content).OfType<TextBox>().Single();
        Assert.AreEqual(24, name.MaxLength);
        Assert.AreEqual(entry.Name, name.Text);
        Assert.IsTrue(Descendants(content).OfType<Button>().Single(b => ReferenceEquals(b.Command, entry.SaveCommand)).IsDefault);
        Assert.AreSame(entry.SkipCommand, dialog.InputBindings.OfType<KeyBinding>().Single(b => b.Key == Key.Escape).Command);
        dialog.Close();
        Assert.IsFalse(entry.SaveCommand.CanExecute(null));

        VerifyModalActions(layout);
        VerifyDeferredDialogAndFocus(layout);
    }

    private static void VerifyDeferredDialogAndFocus(HighScoreLayout layout)
    {
        var application = Application.Current;
        var previousWindow = application.MainWindow;
        var gameView = new GameView { DataContext = new GameViewModel(() => { }, new RatMan.Game.GameSession(HighScoreTestSupport.WinMaze)) };
        var owner = new Window { Content = gameView, Width = 720, Height = 920, Opacity = 0, ShowInTaskbar = false };
        application.MainWindow = owner;
        try
        {
            owner.Show();
            Keyboard.Focus(gameView);
            var entry = new HighScoreEntryViewModel(new MemoryHighScoreRepository(), layout,
                new(Guid.NewGuid(), "", 1, DateTimeOffset.Now), 1);
            Task showing = new HighScoreDialogs().ShowAsync(entry);
            Assert.IsFalse(showing.IsCompleted, "The dialog must be deferred outside the rendering callback.");
            Exception? failure = null;
            application.Dispatcher.Invoke(() =>
            {
                var dialogs = application.Windows.OfType<HighScoreEntryWindow>().ToArray();
                try
                {
                    var dialog = dialogs.Single();
                    Assert.AreSame(owner, dialog.Owner);
                    Assert.AreSame(Descendants(dialog).OfType<TextBox>().Single(), Keyboard.FocusedElement);
                    entry.SkipCommand.Execute(null);
                }
                catch (Exception exception) { failure = exception; }
                finally { foreach (var dialog in dialogs) if (dialog.IsVisible) dialog.Close(); }
            }, DispatcherPriority.ApplicationIdle);
            application.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Assert.IsTrue(showing.IsCompletedSuccessfully);
            Assert.AreSame(gameView, Keyboard.FocusedElement, "Closing the dialog must restore game keyboard input.");
        }
        finally { owner.Close(); application.MainWindow = previousWindow; }
    }

    private static void VerifyModalActions(HighScoreLayout layout)
    {
        foreach (string action in new[] { "save", "skip", "close" })
        {
            var repository = new MemoryHighScoreRepository();
            var entry = new HighScoreEntryViewModel(repository, layout, new(Guid.NewGuid(), "", 1, DateTimeOffset.Now), 1);
            // Hidden from the taskbar and rendered transparently; still exercises real WPF modal focus/lifecycle.
            var dialog = new HighScoreEntryWindow(entry) { Opacity = 0, ShowInTaskbar = false };
            Exception? failure = null;
            dialog.ContentRendered += (_, _) =>
            {
                try
                {
                    var input = Descendants(dialog).OfType<TextBox>().Single();
                    Assert.AreSame(input, Keyboard.FocusedElement);
                    if (action == "save") entry.SaveCommand.Execute(null);
                    else if (action == "skip") dialog.InputBindings.OfType<KeyBinding>().Single(b => b.Key == Key.Escape).Command.Execute(null);
                    else dialog.Close();
                }
                catch (Exception exception) { failure = exception; dialog.Close(); }
            };
            dialog.ShowDialog();
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Assert.AreEqual(action == "save", entry.Saved);
            Assert.AreEqual(action == "save" ? 1 : 0, repository.Submissions);
            Assert.IsFalse(entry.SaveCommand.CanExecute(null));
        }
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
}
