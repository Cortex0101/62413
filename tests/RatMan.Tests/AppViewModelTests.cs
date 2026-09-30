using RatMan.App.Animation;
using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class AppViewModelTests
{
    [TestMethod]
    public void Constructor_StartsAtMainMenu()
    {
        MainViewModel viewModel = new(repository: new MemoryMazeRepository(TestMazes.Sample));

        Assert.IsInstanceOfType(viewModel.ActiveScreen, typeof(MainMenuViewModel));
    }

    [TestMethod]
    public void MenuCommands_NavigateToPlaceholdersAndBack()
    {
        MainViewModel viewModel = new(repository: new MemoryMazeRepository(TestMazes.Sample));
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
        MainViewModel viewModel = new(repository: new MemoryMazeRepository(TestMazes.Sample));
        MainMenuViewModel menu = (MainMenuViewModel)viewModel.ActiveScreen;

        menu.StartGameCommand.Execute(null);
        GameViewModel game = (GameViewModel)viewModel.ActiveScreen;

        Assert.AreEqual(110, game.BoardCells.Count);
        Assert.AreEqual(360d, game.BoardWidth);
        Assert.AreEqual(396d, game.BoardHeight);
        Assert.AreEqual("Row 9, Column 5", game.PlayerPosition);
    }

    [TestMethod]
    public void Constructor_ProjectsThinWallsBetweenCells()
    {
        GameViewModel game = new(() => { }, new GameSession(TestMazes.Sample));

        Assert.AreEqual(TestMazes.Sample.Walls.Count, game.WallSegments.Count);
        CollectionAssert.Contains(
            game.WallSegments.ToArray(),
            new WallSegmentViewModel(178d, -2d, 4d, 40d));
        CollectionAssert.Contains(
            game.WallSegments.ToArray(),
            new WallSegmentViewModel(34d, 70d, 40d, 4d));
    }

    [TestMethod]
    public void MoveCommand_WhenMoveSucceeds_RaisesProjectionChanges()
    {
        GameViewModel game = new(() => { }, new GameSession(TestMazes.Sample));
        HashSet<string?> changedProperties = [];
        game.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        game.MoveCommand.Execute(Direction.Right);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);

        Assert.AreEqual("Row 9, Column 6", game.PlayerPosition);
        Assert.AreEqual(162d, game.PlayerLeft);
        CollectionAssert.IsSubsetOf(
            new[] { nameof(game.PlayerLeft), nameof(game.PlayerPosition) },
            changedProperties.ToArray());
    }

    [TestMethod]
    public void MoveCommand_WhenMoveIsBlocked_PlaysReactionWithoutChangingPosition()
    {
        GameViewModel game = new(() => { }, new GameSession(TestMazes.Sample));
        game.MoveCommand.Execute(Direction.Up);

        Assert.AreEqual("Row 9, Column 5", game.PlayerPosition);
        Assert.AreEqual(PlayerAnimationState.Blocked, game.Player.AnimationState);
        Assert.AreEqual(Direction.Up, game.Player.FacingDirection);

        game.Advance(TimeSpan.FromMilliseconds(210));

        Assert.AreEqual(PlayerAnimationState.Idle, game.Player.AnimationState);
        Assert.AreEqual(288d, game.PlayerTop);
    }

    [TestMethod]
    public void MoveCommand_WhileMoving_ContinuesInLatestDirectionUntilBlocked()
    {
        GameViewModel game = new(() => { }, new GameSession(TestMazes.Sample));

        game.MoveCommand.Execute(Direction.Right);
        game.MoveCommand.Execute(Direction.Down);
        game.MoveCommand.Execute(Direction.Left);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual("Row 9, Column 5", game.PlayerPosition);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
        Assert.AreEqual(Direction.Left, game.Player.FacingDirection);

        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(144d, game.PlayerLeft);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
        Assert.AreEqual(Direction.Left, game.Player.FacingDirection);

        for (int step = 0; step < 4; step++)
        {
            game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);
        }

        Assert.AreEqual(0d, game.PlayerLeft);
        Assert.AreEqual(PlayerAnimationState.Blocked, game.Player.AnimationState);
    }
}
