using System.IO;
using RatMan.App.Persistence;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class MazeRepositoryTests
{
    private string directory = null!;
    private FileMazeRepository repository = null!;
    [TestInitialize]
    public void Initialize()
    {
        directory = Path.Combine(Path.GetTempPath(), "RatManTests", Guid.NewGuid().ToString("N"));
        repository = new(directory);
    }
    [TestCleanup]
    public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [TestMethod]
    public void FirstUse_SeedsExactCurrentLayoutAndDoesNotRestoreDeletedMap()
    {
        MazeEntry entry = repository.List().Single(); MazeDefinition maze = repository.Load(entry.FilePath);
        Assert.AreEqual(ClassicMaze.Size, maze.Size);
        Assert.AreEqual(ClassicMaze.StartingPosition, maze.PlayerSpawn);
        Assert.AreEqual(new GridPosition(5, 5), maze.MonsterSpawn);
        Assert.IsTrue(ClassicMaze.CreateSession().Walls.SetEquals(maze.Walls));
        File.Delete(entry.FilePath);
        Assert.AreEqual(0, new FileMazeRepository(directory).List().Count);
    }

    [TestMethod]
    public void FirstUse_DoesNotOverwriteExistingClassic()
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "Classic.maze"); File.WriteAllText(path, "user data");
        Assert.IsFalse(repository.List().Single().IsValid);
        Assert.AreEqual("user data", File.ReadAllText(path));
    }

    [TestMethod]
    public void SaveLoad_RoundTripsAndReplacesAtomically()
    {
        string path = repository.GetNewPath("Test board"); repository.Save(path, TestMazes.Sample);
        MazeDefinition loaded = repository.Load(path);
        Assert.AreEqual(TestMazes.Sample.Name, loaded.Name);
        Assert.AreEqual(TestMazes.Sample.Size, loaded.Size);
        Assert.AreEqual(TestMazes.Sample.PlayerSpawn, loaded.PlayerSpawn);
        Assert.AreEqual(TestMazes.Sample.MonsterSpawn, loaded.MonsterSpawn);
        Assert.IsTrue(TestMazes.Sample.Walls.ToHashSet().SetEquals(loaded.Walls));
        repository.Save(path, loaded with { Name = "Updated" });
        Assert.AreEqual("Updated", repository.Load(path).Name);
        Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
    }

    [TestMethod]
    public void Exists_SeedsBeforeCheckingClassicCollision()
    {
        Assert.IsTrue(repository.Exists(repository.GetNewPath("Classic")));
    }

    [DataTestMethod]
    [DataRow("null")]
    [DataRow("not JSON")]
    [DataRow("{\"version\":2}")]
    [DataRow("{\"version\":1,\"name\":\"Bad\",\"width\":0,\"height\":3,\"walls\":[]}")]
    [DataRow("{\"version\":1,\"name\":\"Bad\",\"width\":3,\"height\":3,\"walls\":[]}")]
    [DataRow("{\"version\":1,\"name\":\"Bad\",\"width\":3,\"height\":3,\"walls\":[null]}")]
    [DataRow("{\"version\":1,\"name\":\"Bad\",\"width\":3,\"height\":3,\"walls\":[{\"firstTile\":{\"row\":-2147483648,\"column\":0},\"secondTile\":{\"row\":0,\"column\":0}}]}")]
    [DataRow("{\"version\":1,\"name\":\"Bad\",\"width\":3,\"height\":3,\"walls\":[{\"firstTile\":{\"row\":0,\"column\":0},\"secondTile\":{\"row\":2,\"column\":2}}]}")]
    [DataRow("{\"version\":1,\"name\":\"Bad\",\"width\":3,\"height\":3,\"walls\":[],\"playerSpawn\":{\"row\":0},\"monsterSpawn\":{\"row\":1,\"column\":1}}")]
    public void InvalidFile_IsReportedWithoutBreakingCatalog(string json)
    {
        repository.List(); string path = Path.Combine(directory, "Bad.maze"); File.WriteAllText(path, json);
        Assert.ThrowsException<InvalidDataException>(() => repository.Load(path));
        Assert.AreEqual(1, repository.List().Count(entry => entry.IsValid));
        Assert.IsNotNull(repository.List().Single(entry => !entry.IsValid).Error);
    }

    [TestMethod]
    public void FailedWrite_LeavesExistingFileAndRemovesTemporaryFile()
    {
        string path = repository.GetNewPath("Locked"); repository.Save(path, TestMazes.Sample);
        string original = File.ReadAllText(path);
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try { repository.Save(path, TestMazes.Sample with { Name = "Replacement" }); Assert.Fail("Expected locked-file failure."); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        }
        Assert.AreEqual(original, File.ReadAllText(path));
        Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
    }

    [TestMethod]
    public void SafeFileNames_StayInsideLibraryAndHandleReservedNames()
    {
        Assert.AreEqual(directory, Path.GetDirectoryName(repository.GetNewPath("../bad:name")));
        Assert.AreEqual("_CON.maze", Path.GetFileName(repository.GetNewPath("CON")));
    }

    [TestMethod]
    public void DisconnectedBoard_IsValidButSharedOrOutOfBoundsSpawnsAreRejected()
    {
        MazeDefinition maze = new("Disconnected", new(1, 2), [new(new(0, 0), new(0, 1))], new(0, 0), new(0, 1));
        Assert.AreEqual(0, maze.Validate().Count);
        var session = new GameSession(maze);
        Assert.IsFalse(session.TryMovePlayer(Direction.Right));
        Assert.AreEqual(maze.MonsterSpawn, session.MonsterStartingPosition);
        Assert.IsTrue((maze with { MonsterSpawn = maze.PlayerSpawn }).Validate().Count > 0);
        Assert.IsTrue((maze with { MonsterSpawn = new(8, 8) }).Validate().Count > 0);
    }
}
