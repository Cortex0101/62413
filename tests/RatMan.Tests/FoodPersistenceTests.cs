using System.IO;
using System.Text.Json.Nodes;
using RatMan.App.Persistence;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class FoodPersistenceTests
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
    public void Cleanup() => Directory.Delete(directory, recursive: true);

    [TestMethod]
    public void VersionTwo_RoundTripsOverridesWithoutRuntimeState()
    {
        var maze = TestMazes.Sample with { EmptyFoodTiles = [new(0, 0)], SteroidCheeseTiles = [new(0, 1)] };
        string path = repository.GetNewPath("Food");
        repository.Save(path, maze);
        var loaded = repository.Load(path);
        CollectionAssert.AreEqual(maze.EmptyFoodTiles.ToArray(), loaded.EmptyFoodTiles.ToArray());
        CollectionAssert.AreEqual(maze.SteroidCheeseTiles.ToArray(), loaded.SteroidCheeseTiles.ToArray());
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.AreEqual(2, json["version"]!.GetValue<int>());
        Assert.IsNull(json["score"]);
        Assert.IsNull(json["remainingFood"]);
        Assert.IsNull(json["variants"]);
    }

    [TestMethod]
    public void VersionOne_LoadsDefaultFood_UpgradesOnlyOnSave()
    {
        string path = repository.List().Single().FilePath;
        string original = File.ReadAllText(path);
        Assert.AreEqual(1, JsonNode.Parse(original)!["version"]!.GetValue<int>());
        var maze = repository.Load(path);
        Assert.AreEqual(0, maze.EmptyFoodTiles.Count);
        Assert.AreEqual(0, maze.SteroidCheeseTiles.Count);
        Assert.IsTrue(new GameSession(maze).RemainingFood.Count > 0);
        Assert.AreEqual(original, File.ReadAllText(path));
        repository.Save(path, maze);
        Assert.AreEqual(2, JsonNode.Parse(File.ReadAllText(path))!["version"]!.GetValue<int>());
    }

    [DataTestMethod]
    [DataRow("[{\"row\":-1,\"column\":0}]", "[]")]
    [DataRow("[]", "[{\"row\":11,\"column\":0}]")]
    [DataRow("[{\"row\":0,\"column\":1}]", "[{\"row\":0,\"column\":1}]")]
    [DataRow("[null]", "[]")]
    [DataRow("[]", "[{\"row\":0}]")]
    public void InvalidFood_IsReportedWithoutBreakingCatalog(string empty, string steroid)
    {
        string path = repository.GetNewPath("Bad food");
        repository.Save(path, TestMazes.Sample);
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        json["emptyFoodTiles"] = JsonNode.Parse(empty);
        json["steroidCheeseTiles"] = JsonNode.Parse(steroid);
        File.WriteAllText(path, json.ToJsonString());
        Assert.ThrowsException<InvalidDataException>(() => repository.Load(path));
        Assert.IsFalse(repository.List().Single(entry => entry.FilePath == path).IsValid);
        Assert.AreEqual(1, repository.List().Count(entry => entry.IsValid));
    }
}
