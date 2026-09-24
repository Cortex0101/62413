using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class AppViewModelTests
{
    [TestMethod]
    public void Constructor_StartsAtMainMenu()
    {
        MainViewModel viewModel = new();

        Assert.IsInstanceOfType(viewModel.ActiveScreen, typeof(MainMenuViewModel));
    }

    [TestMethod]
    public void MenuCommands_NavigateToPlaceholdersAndBack()
    {
        MainViewModel viewModel = new();
        MainMenuViewModel menu = (MainMenuViewModel)viewModel.ActiveScreen;

        menu.HighScoresCommand.Execute(null);
        PlaceholderViewModel placeholder = (PlaceholderViewModel)viewModel.ActiveScreen;

        Assert.AreEqual("High Scores", placeholder.Title);

        placeholder.ReturnToMenuCommand.Execute(null);

        Assert.AreSame(menu, viewModel.ActiveScreen);
    }

    [TestMethod]
    public void StartGame_NavigatesToProjectedBoard()
    {
        MainViewModel viewModel = new();
        MainMenuViewModel menu = (MainMenuViewModel)viewModel.ActiveScreen;

        menu.StartGameCommand.Execute(null);
        GameViewModel game = (GameViewModel)viewModel.ActiveScreen;

        Assert.AreEqual(140, game.BoardCells.Count);
        Assert.AreEqual(504d, game.BoardWidth);
        Assert.AreEqual(360d, game.BoardHeight);
        Assert.AreEqual("Row 6, Column 8", game.PlayerPosition);
    }

    [TestMethod]
    public void MoveCommand_WhenMoveSucceeds_RaisesProjectionChanges()
    {
        GameViewModel game = new(() => { });
        HashSet<string?> changedProperties = [];
        game.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        game.MoveCommand.Execute(Direction.Right);

        Assert.AreEqual("Row 6, Column 9", game.PlayerPosition);
        CollectionAssert.IsSubsetOf(
            new[] { nameof(game.PlayerLeft), nameof(game.PlayerTop), nameof(game.PlayerPosition) },
            changedProperties.ToArray());
    }

    [TestMethod]
    public void MoveCommand_WhenAtBoundary_DoesNotRaiseProjectionChanges()
    {
        GameViewModel game = new(() => { });
        int propertyChangeCount = 0;
        game.PropertyChanged += (_, _) => propertyChangeCount++;

        for (int move = 0; move < 10; move++)
        {
            game.MoveCommand.Execute(Direction.Up);
        }

        propertyChangeCount = 0;
        game.MoveCommand.Execute(Direction.Up);

        Assert.AreEqual(0, propertyChangeCount);
        Assert.AreEqual("Row 1, Column 8", game.PlayerPosition);
    }
}
