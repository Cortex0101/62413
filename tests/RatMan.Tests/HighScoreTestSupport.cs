using System.IO;
using RatMan.App.Persistence;
using RatMan.App.Services;
using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

internal static class HighScoreTestSupport
{
    public static MazeDefinition WinMaze => new("Sprint", new(1, 3), [], new(0, 0), new(0, 2)) { EmptyFoodTiles = [new(0, 2)] };
    public static GameViewModel Start(MainViewModel app)
    {
        ((MainMenuViewModel)app.ActiveScreen).StartGameCommand.Execute(null);
        return (GameViewModel)app.ActiveScreen;
    }
    public static void Win(GameViewModel game)
    {
        game.MoveCommand.Execute(Direction.Right);
        game.Advance(TimeSpan.FromMilliseconds(350));
        Assert.IsTrue(game.IsFinished);
    }
}

internal sealed class MemoryHighScoreRepository : IHighScoreRepository
{
    public List<HighScoreBoard> Boards { get; } = [];
    public bool FailLoad { get; set; }
    public bool FailSave { get; set; }
    public TaskCompletionSource? LoadGate { get; set; }
    public TaskCompletionSource? SaveGate { get; set; }
    public int Submissions { get; private set; }
    public async Task<IReadOnlyList<HighScoreBoard>> LoadAsync()
    {
        if (LoadGate is not null) await LoadGate.Task;
        if (FailLoad) throw new IOException("Read failed");
        return Boards.ToArray();
    }
    public async Task<HighScoreSubmission> SubmitAsync(HighScoreLayout layout, HighScoreEntry entry)
    {
        Submissions++;
        if (SaveGate is not null) await SaveGate.Task;
        if (FailSave) throw new IOException("Write failed");
        int index = Boards.FindIndex(b => b.Layout.Id == layout.Id);
        var entries = index < 0 ? Array.Empty<HighScoreEntry>() : Boards[index].Entries;
        int? rank = HighScoreRules.QualifyingRank(entries, entry.Score);
        if (rank is null) return new(false, null);
        var board = new HighScoreBoard(layout, HighScoreRules.Top(entries.Append(entry)));
        if (index < 0) Boards.Add(board); else Boards[index] = board;
        return new(true, rank);
    }
}

internal sealed class TestHighScoreDialogs : IHighScoreDialogs
{
    public int Prompts { get; private set; }
    public Func<HighScoreEntryViewModel, Task> OnShow { get; set; } = entry => { entry.SkipCommand.Execute(null); return Task.CompletedTask; };
    public Task ShowAsync(HighScoreEntryViewModel entry) { Prompts++; return OnShow(entry); }
}
