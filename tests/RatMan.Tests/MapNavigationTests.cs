using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class MapNavigationTests
{
    [TestMethod]
    public void EmptyLibrary_OffersBuilderAndReturnToMenu()
    {
        var app = new MainViewModel(repository: new MemoryMazeRepository(), dialogs: new TestEditorDialogs());
        var menu = (MainMenuViewModel)app.ActiveScreen; menu.StartGameCommand.Execute(null);
        var picker = (MapSelectionViewModel)app.ActiveScreen;
        Assert.AreEqual(0, picker.Maps.Count);
        picker.BuilderCommand.Execute(null);
        var editor = (BoardBuilderViewModel)app.ActiveScreen;
        editor.ReturnToMenuCommand.Execute(null); Assert.AreSame(menu, app.ActiveScreen);
        menu.BoardBuilderCommand.Execute(null); Assert.IsInstanceOfType<BoardBuilderViewModel>(app.ActiveScreen);
    }

    [TestMethod]
    public void OneMap_StartsDirectlyAndReplayCreatesFreshSession()
    {
        var app = new MainViewModel(repository: new MemoryMazeRepository(TestMazes.Sample));
        var menu = (MainMenuViewModel)app.ActiveScreen; menu.StartGameCommand.Execute(null);
        var first = (GameViewModel)app.ActiveScreen; first.MoveCommand.Execute(Direction.Right);
        first.ReturnToMenuCommand.Execute(null); menu.StartGameCommand.Execute(null);
        var second = (GameViewModel)app.ActiveScreen;
        Assert.AreNotSame(first, second); Assert.AreEqual("Row 9, Column 5", second.PlayerPosition);
        Assert.IsTrue(second.HasMonsterSpawn);
    }

    [TestMethod]
    public void MultipleMaps_SelectsDimensionsWallsAndSpawnsAndRefreshesCatalog()
    {
        var repository = new MemoryMazeRepository(TestMazes.Sample, new("Small", new(2, 3), [], new(0, 0), new(1, 2)));
        var app = new MainViewModel(repository: repository);
        var menu = (MainMenuViewModel)app.ActiveScreen; menu.StartGameCommand.Execute(null);
        var picker = (MapSelectionViewModel)app.ActiveScreen;
        picker.SelectCommand.Execute(picker.Maps.Single(map => map.Name == "Small"));
        var game = (GameViewModel)app.ActiveScreen;
        Assert.AreEqual(108d, game.BoardWidth); Assert.AreEqual(72d, game.BoardHeight);
        Assert.AreEqual(0, game.WallSegments.Count); Assert.AreEqual("Row 1, Column 1", game.PlayerPosition);
        game.ReturnToMenuCommand.Execute(null); repository.Files.Remove("Small.maze");
        menu.StartGameCommand.Execute(null); Assert.IsInstanceOfType<GameViewModel>(app.ActiveScreen);
    }

    [TestMethod]
    public void Builder_LoadUsesSharedPickerAndPreservesDraftOnReadFailure()
    {
        var repository = new MemoryMazeRepository(TestMazes.Sample);
        var app = new MainViewModel(repository: repository, dialogs: new TestEditorDialogs());
        ((MainMenuViewModel)app.ActiveScreen).BoardBuilderCommand.Execute(null);
        var editor = (BoardBuilderViewModel)app.ActiveScreen; editor.LoadBoardCommand.Execute(null);
        var picker = (MapSelectionViewModel)app.ActiveScreen;
        repository.FailLoad = true; picker.SelectCommand.Execute(picker.Maps[0]);
        Assert.AreSame(picker, app.ActiveScreen); Assert.AreEqual("", editor.BoardName);
        repository.FailLoad = false; picker.SelectCommand.Execute(picker.Maps[0]);
        Assert.AreSame(editor, app.ActiveScreen); Assert.AreEqual(TestMazes.Sample.Name, editor.BoardName);
    }

    [TestMethod]
    public void UnreadableLibrary_ShowsRecoverableError()
    {
        var app = new MainViewModel(repository: new MemoryMazeRepository { FailList = true });
        ((MainMenuViewModel)app.ActiveScreen).StartGameCommand.Execute(null);
        var picker = (MapSelectionViewModel)app.ActiveScreen;
        StringAssert.Contains(picker.Status, "Library unavailable");
        picker.BackCommand.Execute(null); Assert.IsInstanceOfType<MainMenuViewModel>(app.ActiveScreen);
    }
}
