using System.IO;
using System.Text.Json.Nodes;
using RatMan.App.Persistence;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class HighScoreRepositoryTests
{
    private string directory = null!;
    private string PathName => Path.Combine(directory, "highscores.json");
    private static readonly HighScoreLayout Layout = HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze);
    private static HighScoreEntry Entry(int score, string name = "Player") => new(Guid.NewGuid(), name, score, DateTimeOffset.UtcNow);

    [TestInitialize]
    public void Initialize() => directory = Path.Combine(Path.GetTempPath(), "RatMan-highscores-" + Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        string full = Path.GetFullPath(directory);
        Assert.AreEqual(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(full));
        Assert.IsTrue(Path.GetFileName(full).StartsWith("RatMan-highscores-", StringComparison.Ordinal));
        if (Directory.Exists(full)) Directory.Delete(full, true);
    }

    [TestMethod]
    public async Task MissingFile_IsEmpty_AndDoesNotCreateStorage()
    {
        Assert.AreEqual(0, (await new FileHighScoreRepository(PathName).LoadAsync()).Count);
        Assert.IsFalse(Directory.Exists(directory));
    }

    [TestMethod]
    public async Task RoundTrip_RetainsTopTen_NormalizesNames_AndKeepsEarlierTies()
    {
        var repository = new FileHighScoreRepository(PathName);
        var first = Entry(20, "  Ada  ");
        var second = Entry(20, "  ");
        await repository.SubmitAsync(Layout, first);
        await repository.SubmitAsync(Layout, second);
        for (int score = 0; score < 12; score++) await repository.SubmitAsync(Layout, Entry(score));
        var board = (await new FileHighScoreRepository(PathName).LoadAsync()).Single();
        Assert.AreEqual(Layout, board.Layout);
        Assert.AreEqual(10, board.Entries.Count);
        Assert.AreEqual(first.Id, board.Entries[0].Id);
        Assert.AreEqual("Ada", board.Entries[0].Name);
        Assert.AreEqual("Anonymous", board.Entries[1].Name);
        CollectionAssert.AreEqual(new[] { 20, 20, 11, 10, 9, 8, 7, 6, 5, 4 }, board.Entries.Select(e => e.Score).ToArray());
        Assert.IsFalse((await repository.SubmitAsync(Layout, Entry(4))).Saved);
        Assert.IsFalse((await repository.SubmitAsync(Layout, Entry(0))).Saved);
        Assert.AreEqual(3, (await repository.SubmitAsync(Layout, Entry(19, new string('x', 40)))).Rank);
        board = (await repository.LoadAsync()).Single();
        Assert.AreEqual(24, board.Entries[2].Name.Length);
        Assert.AreEqual(first.CompletedAt, board.Entries[0].CompletedAt);
    }

    [TestMethod]
    public async Task Submission_RechecksRank_IsIdempotent_AndSeparatesLayouts()
    {
        var repository = new FileHighScoreRepository(PathName);
        Assert.AreEqual(1, HighScoreRules.QualifyingRank([], 0));
        var entry = Entry(0);
        Assert.IsTrue((await repository.SubmitAsync(Layout, entry)).Saved);
        Assert.IsTrue((await repository.SubmitAsync(Layout, entry)).Saved);
        Assert.AreEqual(1, (await repository.LoadAsync()).Single().Entries.Count);
        for (int i = 0; i < 10; i++) await repository.SubmitAsync(Layout, Entry(100));
        Assert.IsFalse((await repository.SubmitAsync(Layout, Entry(99))).Saved);
        var other = HighScoreLayout.FromMaze(HighScoreTestSupport.WinMaze with { SteroidCheeseTiles = [new(0, 1)] });
        Assert.IsTrue((await repository.SubmitAsync(other, Entry(0))).Saved);
        Assert.AreEqual(2, (await repository.LoadAsync()).Count);
    }

    [TestMethod]
    public async Task ConcurrentRepositoryInstances_DoNotLoseEntries()
    {
        await Task.WhenAll(Enumerable.Range(1, 10).Select(i => new FileHighScoreRepository(PathName).SubmitAsync(Layout, Entry(i))));
        var board = (await new FileHighScoreRepository(PathName).LoadAsync()).Single();
        CollectionAssert.AreEqual(Enumerable.Range(1, 10).Reverse().ToArray(), board.Entries.Select(e => e.Score).ToArray());
    }

    [DataTestMethod]
    [DataRow("{")]
    [DataRow("null")]
    [DataRow("{\"version\":99,\"boards\":[]}")]
    [DataRow("{\"version\":1,\"boards\":[null]}")]
    public async Task InvalidStorage_IsReported_AndNeverOverwritten(string contents)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(PathName, contents);
        var repository = new FileHighScoreRepository(PathName);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => repository.LoadAsync());
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => repository.SubmitAsync(Layout, Entry(100)));
        Assert.AreEqual(contents, await File.ReadAllTextAsync(PathName));
    }

    [TestMethod]
    public async Task InvalidEntry_IsReported_AndOriginalFileIsPreserved()
    {
        var repository = new FileHighScoreRepository(PathName);
        await repository.SubmitAsync(Layout, Entry(10));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(PathName))!;
        json["boards"]![0]!["entries"]![0]!["score"] = -1;
        string contents = json.ToJsonString();
        await File.WriteAllTextAsync(PathName, contents);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => repository.SubmitAsync(Layout, Entry(100)));
        Assert.AreEqual(contents, await File.ReadAllTextAsync(PathName));
    }

    [TestMethod]
    public async Task FailedAtomicReplacement_PreservesOriginal_AndRemovesTemporaryFile()
    {
        var repository = new FileHighScoreRepository(PathName);
        await repository.SubmitAsync(Layout, Entry(10));
        string before = await File.ReadAllTextAsync(PathName);
        using (var locked = new FileStream(PathName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try
            {
                await repository.SubmitAsync(Layout, Entry(20));
                Assert.Fail("Replacing a locked file should fail.");
            }
            catch (Exception exception) when (FileHighScoreRepository.IsStorageError(exception)) { }
        }
        Assert.AreEqual(before, await File.ReadAllTextAsync(PathName));
        Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
        Assert.IsTrue((await repository.SubmitAsync(Layout, Entry(20))).Saved);
    }

    [TestMethod]
    public async Task UnreadableFile_IsNotTreatedAsAnEmptyLeaderboard()
    {
        var repository = new FileHighScoreRepository(PathName);
        await repository.SubmitAsync(Layout, Entry(10));
        using var locked = new FileStream(PathName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        await Assert.ThrowsExceptionAsync<IOException>(() => repository.LoadAsync());
    }

    [TestMethod]
    public void LayoutIdentity_IgnoresNamesOrderingAndHiddenFood_ButTracksGameplayChanges()
    {
        var maze = new MazeDefinition("Original", new(3, 4), [new(new(0, 0), new(1, 0)), new(new(1, 2), new(1, 3))],
            new(0, 0), new(2, 3)) { OpenBorderRows = [0, 2], SteroidCheeseTiles = [new(0, 1), new(1, 2)] };
        string original = HighScoreLayout.FromMaze(maze).Id;
        Assert.AreEqual(original, HighScoreLayout.FromMaze(maze with
        {
            Name = "Renamed copy", Walls = maze.Walls.Reverse().Concat(maze.Walls).ToArray(),
            OpenBorderRows = [2, 0, 2], SteroidCheeseTiles = maze.SteroidCheeseTiles.Reverse().ToArray()
        }).Id);
        foreach (var changed in new[]
        {
            maze with { Size = new(3, 5) }, maze with { Walls = [] }, maze with { PlayerSpawn = new(0, 2) },
            maze with { MonsterSpawn = new(2, 2) }, maze with { OpenBorderRows = [] },
            maze with { OpenBorderColumns = [1] }, maze with { EmptyFoodTiles = [new(2, 0)] },
            maze with { SteroidCheeseTiles = [] }
        }) Assert.AreNotEqual(original, HighScoreLayout.FromMaze(changed).Id);
        Assert.AreEqual(original, HighScoreLayout.FromMaze(maze with { EmptyFoodTiles = [maze.PlayerSpawn!.Value] }).Id);
        Assert.AreEqual(original, HighScoreLayout.FromMaze(maze with { Name = "Restored" }).Id);
    }
}
