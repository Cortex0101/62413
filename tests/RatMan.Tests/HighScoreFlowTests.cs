using RatMan.App.Persistence;
using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class HighScoreFlowTests
{
    [TestMethod]
    public async Task Victory_PromptsOnce_SavesFinalScore_AndReplayCanSubmitAgain()
    {
        var scores = new MemoryHighScoreRepository();
        var dialogs = new TestHighScoreDialogs
        {
            OnShow = async entry => { Assert.AreEqual(1, entry.Score); entry.Name = "  Ada  "; await entry.SaveCommand.ExecuteAsync(); }
        };
        var app = new MainViewModel(repository: new MemoryMazeRepository(HighScoreTestSupport.WinMaze), highScores: scores, highScoreDialogs: dialogs);
        var game = HighScoreTestSupport.Start(app);
        HighScoreTestSupport.Win(game);
        await game.HighScoreCompletion;
        for (int i = 0; i < 4; i++) { game.Advance(TimeSpan.FromSeconds(1)); game.MoveCommand.Execute(Direction.Right); }
        Assert.AreEqual(1, dialogs.Prompts);
        Assert.AreEqual(1, scores.Submissions);
        Assert.AreEqual("Ada", scores.Boards.Single().Entries.Single().Name);
        Assert.AreSame(game, app.ActiveScreen);
        Assert.IsTrue(game.ReplayCommand.CanExecute(null));
        game.ReplayCommand.Execute(null);
        HighScoreTestSupport.Win(game);
        await game.HighScoreCompletion;
        Assert.AreEqual(2, dialogs.Prompts);
        Assert.AreEqual(2, scores.Boards.Single().Entries.Select(e => e.Id).Distinct().Count());
        game.ShowHighScoresCommand.Execute(null);
        var screen = (HighScoresViewModel)app.ActiveScreen;
        await screen.Loading;
        Assert.AreEqual(HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze).Id, screen.SelectedLayout!.Layout.Id);
        Assert.AreEqual(2, screen.Rows.Count);
    }

    [TestMethod]
    public async Task LifeLost_DoesNotPrompt_ButGameOverDoes_EvenWithZeroPoints()
    {
        var scores = new MemoryHighScoreRepository();
        var dialogs = new TestHighScoreDialogs { OnShow = entry => entry.SaveCommand.ExecuteAsync() };
        var maze = new MazeDefinition("Catch", new(1, 3), [], new(0, 0), new(0, 1));
        var app = new MainViewModel(repository: new MemoryMazeRepository(maze), highScores: scores, highScoreDialogs: dialogs);
        var game = HighScoreTestSupport.Start(app);
        game.Advance(TimeSpan.FromMilliseconds(400));
        Assert.IsFalse(game.IsFinished);
        Assert.AreEqual(0, dialogs.Prompts);
        game.Advance(TimeSpan.FromSeconds(10));
        await game.HighScoreCompletion;
        Assert.AreEqual("Game over", game.OutcomeTitle);
        Assert.AreEqual(1, dialogs.Prompts);
        var entry = scores.Boards.Single().Entries.Single();
        Assert.AreEqual(0, entry.Score);
        Assert.AreEqual("Anonymous", entry.Name);
    }

    [TestMethod]
    public async Task NonqualifyingScore_DoesNotPromptOrSubmit()
    {
        var scores = new MemoryHighScoreRepository();
        scores.Boards.Add(new(HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze), Enumerable.Range(0, 10)
            .Select(_ => new HighScoreEntry(Guid.NewGuid(), "Player", 1, DateTimeOffset.UtcNow)).ToArray()));
        var dialogs = new TestHighScoreDialogs();
        var app = new MainViewModel(repository: new MemoryMazeRepository(HighScoreTestSupport.WinMaze), highScores: scores, highScoreDialogs: dialogs);
        var game = HighScoreTestSupport.Start(app);
        HighScoreTestSupport.Win(game);
        await game.HighScoreCompletion;
        Assert.AreEqual(0, dialogs.Prompts);
        Assert.AreEqual(0, scores.Submissions);
        Assert.IsFalse(game.IsHighScoreBusy);
    }

    [TestMethod]
    public async Task LeavingMidGameOrSkipping_DoesNotSaveOrPromptAgain()
    {
        var scores = new MemoryHighScoreRepository();
        var dialogs = new TestHighScoreDialogs();
        var app = new MainViewModel(repository: new MemoryMazeRepository(HighScoreTestSupport.WinMaze), highScores: scores, highScoreDialogs: dialogs);
        var game = HighScoreTestSupport.Start(app);
        game.ReturnToMenuCommand.Execute(null);
        Assert.AreEqual(0, dialogs.Prompts);
        game = HighScoreTestSupport.Start(app);
        HighScoreTestSupport.Win(game);
        await game.HighScoreCompletion;
        game.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, dialogs.Prompts);
        Assert.AreEqual(0, scores.Submissions);
        Assert.AreEqual(0, scores.Boards.Count);
    }

    [TestMethod]
    public async Task PendingCheck_BlocksNavigationAndReplay_UntilDialogFinishes()
    {
        var scores = new MemoryHighScoreRepository { LoadGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var dialogs = new TestHighScoreDialogs();
        var app = new MainViewModel(repository: new MemoryMazeRepository(HighScoreTestSupport.WinMaze), highScores: scores, highScoreDialogs: dialogs);
        var game = HighScoreTestSupport.Start(app);
        HighScoreTestSupport.Win(game);
        Assert.IsTrue(game.IsHighScoreBusy);
        Assert.IsFalse(game.ReplayCommand.CanExecute(null));
        Assert.IsFalse(game.ReturnToMenuCommand.CanExecute(null));
        Assert.IsFalse(game.ShowHighScoresCommand.CanExecute(null));
        game.ReplayCommand.Execute(null);
        game.ReturnToMenuCommand.Execute(null);
        game.ShowHighScoresCommand.Execute(null);
        game.MoveCommand.Execute(Direction.Right);
        Assert.AreSame(game, app.ActiveScreen);
        Assert.AreEqual(0, dialogs.Prompts);
        scores.LoadGate.SetResult();
        await game.HighScoreCompletion;
        Assert.AreEqual(1, dialogs.Prompts);
        Assert.IsFalse(game.IsHighScoreBusy);
        Assert.IsTrue(game.IsFinished);
    }

    [TestMethod]
    public async Task FailedCheck_CanRetry_WithoutLosingFinalScore()
    {
        var scores = new MemoryHighScoreRepository { FailLoad = true };
        var dialogs = new TestHighScoreDialogs { OnShow = entry => entry.SaveCommand.ExecuteAsync() };
        var app = new MainViewModel(repository: new MemoryMazeRepository(HighScoreTestSupport.WinMaze), highScores: scores, highScoreDialogs: dialogs);
        var game = HighScoreTestSupport.Start(app);
        HighScoreTestSupport.Win(game);
        await game.HighScoreCompletion;
        Assert.IsTrue(game.HasHighScoreError);
        Assert.IsTrue(game.RetryHighScoreCommand.CanExecute(null));
        Assert.IsTrue(game.ReturnToMenuCommand.CanExecute(null));
        Assert.AreEqual(0, dialogs.Prompts);
        scores.FailLoad = false;
        await game.RetryHighScoreCommand.ExecuteAsync();
        Assert.IsFalse(game.HasHighScoreError);
        Assert.AreEqual(1, scores.Boards.Single().Entries.Single().Score);
        Assert.IsFalse(game.RetryHighScoreCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Entry_SaveFailureRetainsName_AndSuccessfulRetryClosesOnce()
    {
        var scores = new MemoryHighScoreRepository { FailSave = true };
        var entry = CreateEntry(scores);
        entry.Name = "Ada";
        int closes = 0;
        entry.CloseRequested += () => closes++;
        await entry.SaveCommand.ExecuteAsync();
        Assert.AreEqual("Ada", entry.Name);
        Assert.IsFalse(entry.Saved);
        Assert.AreEqual(0, closes);
        Assert.IsTrue(entry.CanClose);
        StringAssert.Contains(entry.Status, "Could not save");
        scores.FailSave = false;
        await entry.SaveCommand.ExecuteAsync();
        await entry.SaveCommand.ExecuteAsync();
        Assert.AreEqual(1, closes);
        Assert.AreEqual(2, scores.Submissions);
        Assert.AreEqual(1, scores.Boards.Single().Entries.Count);
    }

    [TestMethod]
    public async Task Entry_PendingSaveCannotBeSubmittedTwiceOrDismissed()
    {
        var scores = new MemoryHighScoreRepository { SaveGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var entry = CreateEntry(scores);
        Task first = entry.SaveCommand.ExecuteAsync();
        Assert.IsFalse(entry.CanClose);
        Assert.IsFalse(entry.CanEdit);
        entry.SkipCommand.Execute(null);
        entry.Dismiss();
        await entry.SaveCommand.ExecuteAsync();
        Assert.AreEqual(1, scores.Submissions);
        scores.SaveGate.SetResult();
        await first;
        Assert.IsTrue(entry.Saved);
    }

    [TestMethod]
    public async Task Entry_ChangedLeaderboardAndDismissalDoNotSave()
    {
        var scores = new MemoryHighScoreRepository();
        var entry = CreateEntry(scores);
        scores.Boards.Add(new(HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze), Enumerable.Range(0, 10)
            .Select(_ => new HighScoreEntry(Guid.NewGuid(), "Player", 100, DateTimeOffset.UtcNow)).ToArray()));
        await entry.SaveCommand.ExecuteAsync();
        Assert.IsFalse(entry.Saved);
        Assert.IsFalse(entry.SaveCommand.CanExecute(null));
        Assert.IsTrue(entry.CanClose);
        StringAssert.Contains(entry.Status, "no longer");
        var dismissed = CreateEntry(scores);
        dismissed.Dismiss();
        await dismissed.SaveCommand.ExecuteAsync();
        Assert.AreEqual(1, scores.Submissions);
    }

    [TestMethod]
    public async Task Leaderboard_MergesCurrentAndHistoricalLayouts_SelectsLatest_AndRefreshes()
    {
        var old = HighScoreTestSupport.WinMaze;
        var current = old with { SteroidCheeseTiles = [new(0, 1)] };
        var scores = new MemoryHighScoreRepository();
        var oldLayout = HighScoreLayout.FromMaze(old);
        scores.Boards.Add(new(oldLayout, [new(Guid.NewGuid(), "Old player", 1, DateTimeOffset.UtcNow)]));
        var maps = new MemoryMazeRepository(current);
        var screen = new HighScoresViewModel(scores, maps, () => { });
        await screen.Loading;
        Assert.AreEqual(2, screen.Layouts.Count);
        Assert.AreEqual(oldLayout.Id, screen.SelectedLayout!.Layout.Id);
        Assert.AreEqual("Old player", screen.Rows.Single().Name);
        screen.SelectedLayout = screen.Layouts.Single(b => b.Layout.Id != oldLayout.Id);
        Assert.AreEqual(0, screen.Rows.Count);
        StringAssert.Contains(screen.EmptyMessage, "No scores for this layout");
        var newest = new HighScoreEntry(Guid.NewGuid(), "New player", 5, DateTimeOffset.UtcNow);
        await scores.SubmitAsync(HighScoreLayout.FromMaze(current), newest);
        await screen.RefreshCommand.ExecuteAsync();
        Assert.AreEqual("New player", screen.Rows.Single().Name);
        maps.Files.Clear();
        maps.Files.Add("renamed.maze", old with { Name = "Renamed" });
        await screen.RefreshCommand.ExecuteAsync();
        Assert.AreEqual(2, screen.Layouts.Count);
        Assert.AreEqual("Renamed", screen.Layouts.Single(b => b.Layout.Id == oldLayout.Id).Layout.Name);
    }

    [TestMethod]
    public async Task Leaderboard_LoadErrorsAndEmptyLibrariesRemainNavigableAndRecoverable()
    {
        var scores = new MemoryHighScoreRepository { FailLoad = true };
        bool back = false;
        var screen = new HighScoresViewModel(scores, new MemoryMazeRepository(), () => back = true);
        await screen.Loading;
        StringAssert.Contains(screen.Status, "Could not load high scores");
        screen.ReturnToMenuCommand.Execute(null);
        Assert.IsTrue(back);
        scores.FailLoad = false;
        await screen.RefreshCommand.ExecuteAsync();
        Assert.AreEqual("", screen.Status);
        StringAssert.Contains(screen.EmptyMessage, "No maps");
        var layout = HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze);
        scores.Boards.Add(new(layout, [new(Guid.NewGuid(), "Player", 10, DateTimeOffset.UtcNow)]));
        var unavailable = new HighScoresViewModel(scores, new MemoryMazeRepository { FailList = true }, () => { });
        await unavailable.Loading;
        StringAssert.Contains(unavailable.Status, "Could not read map library");
        Assert.AreEqual(1, unavailable.Rows.Count);
    }

    private static HighScoreEntryViewModel CreateEntry(MemoryHighScoreRepository repository) => new(repository,
        HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze), new(Guid.NewGuid(), "", 1, DateTimeOffset.UtcNow), 1);
}
