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

        Assert.AreEqual(128, game.BoardCells.Count);
        Assert.AreEqual(288d, game.BoardWidth);
        Assert.AreEqual(576d, game.BoardHeight);
        Assert.AreEqual("Row 2, Column 2", game.PlayerPosition);
    }

    [TestMethod]
    public void Constructor_ProjectsWallsOntoTileBoundaries()
    {
        GameViewModel game = new(() => { });

        Assert.AreEqual(7, game.WallSegments.Count);
        CollectionAssert.Contains(
            game.WallSegments.ToArray(),
            new WallSegmentViewModel(108d, 34d, 36d, 4d));
        CollectionAssert.Contains(
            game.WallSegments.ToArray(),
            new WallSegmentViewModel(214d, 36d, 4d, 36d));
    }

    [TestMethod]
    public void MoveCommand_WhenMoveSucceeds_RaisesProjectionChanges()
    {
        GameViewModel game = new(() => { });
        HashSet<string?> changedProperties = [];
        game.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        game.MoveCommand.Execute(Direction.Right);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);

        Assert.AreEqual("Row 2, Column 3", game.PlayerPosition);
        Assert.AreEqual(54d, game.PlayerLeft);
        CollectionAssert.IsSubsetOf(
            new[] { nameof(game.PlayerLeft), nameof(game.PlayerPosition) },
            changedProperties.ToArray());
    }

    [TestMethod]
    public void MoveCommand_WhenMoveIsBlocked_PlaysReactionWithoutChangingPosition()
    {
        GameViewModel game = new(() => { });
        game.MoveCommand.Execute(Direction.Up);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual("Row 1, Column 2", game.PlayerPosition);
        Assert.AreEqual(PlayerAnimationState.Blocked, game.Player.AnimationState);
        Assert.AreEqual(Direction.Up, game.Player.FacingDirection);

        game.Advance(TimeSpan.FromMilliseconds(210));

        Assert.AreEqual(PlayerAnimationState.Idle, game.Player.AnimationState);
        Assert.AreEqual(0d, game.PlayerTop);
    }

    [TestMethod]
    public void MoveCommand_WhileMoving_ContinuesInLatestDirectionUntilBlocked()
    {
        GameViewModel game = new(() => { });

        game.MoveCommand.Execute(Direction.Right);
        game.MoveCommand.Execute(Direction.Down);
        game.MoveCommand.Execute(Direction.Left);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual("Row 2, Column 2", game.PlayerPosition);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
        Assert.AreEqual(Direction.Left, game.Player.FacingDirection);

        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(36d, game.PlayerLeft);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
        Assert.AreEqual(Direction.Left, game.Player.FacingDirection);

        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(0d, game.PlayerLeft);
        Assert.AreEqual(PlayerAnimationState.Blocked, game.Player.AnimationState);
    }
}
