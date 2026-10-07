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
    public async Task MenuCommands_NavigateToHighScoresAndBack()
    {
        MainViewModel viewModel = new(repository: new MemoryMazeRepository(TestMazes.Sample), highScores: new MemoryHighScoreRepository());
        MainMenuViewModel menu = (MainMenuViewModel)viewModel.ActiveScreen;

        menu.HighScoresCommand.Execute(null);
        HighScoresViewModel placeholder = (HighScoresViewModel)viewModel.ActiveScreen;
        await placeholder.Loading;

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

        Assert.AreEqual("Row 9, Column 5", game.PlayerPosition);
        Assert.AreEqual(144d + 36d * 170 / 350, game.PlayerLeft, .000001);
        Assert.IsFalse(changedProperties.Contains(nameof(game.PlayerPosition)));
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        Assert.AreEqual("Row 9, Column 6", game.PlayerPosition);
        CollectionAssert.IsSubsetOf(
            new[] { nameof(game.PlayerLeft), nameof(game.PlayerPosition) },
            changedProperties.ToArray());
    }

    [TestMethod]
    public void MoveCommand_WhenIdleAndTurnIsBlocked_KeepsPositionAndFacingDirection()
    {
        GameViewModel game = new(() => { }, new GameSession(TestMazes.Sample));
        game.MoveCommand.Execute(Direction.Up);

        Assert.AreEqual("Row 9, Column 5", game.PlayerPosition);
        Assert.AreEqual(PlayerAnimationState.Idle, game.Player.AnimationState);
        Assert.AreEqual(Direction.Down, game.Player.FacingDirection);

        game.Advance(TimeSpan.FromMilliseconds(210));

        Assert.AreEqual(PlayerAnimationState.Idle, game.Player.AnimationState);
        Assert.AreEqual(288d, game.PlayerTop);
    }

    [DataTestMethod]
    [DataRow(Direction.Right, Direction.Up)]
    [DataRow(Direction.Left, Direction.Down)]
    [DataRow(Direction.Down, Direction.Right)]
    [DataRow(Direction.Up, Direction.Left)]
    public void MoveCommand_WhenTurnIsBlocked_ContinuesUntilFirstOpening(
        Direction forward,
        Direction turn)
    {
        GridPosition start = new(4, 4);
        GridPosition first = start.AdjacentTo(forward);
        GridPosition second = first.AdjacentTo(forward);
        GridPosition opening = second.AdjacentTo(forward);
        GameSession session = new(new BoardSize(9, 9), start,
        [
            new Wall(first, first.AdjacentTo(turn)),
            new Wall(second, second.AdjacentTo(turn)),
            new Wall(opening, opening.AdjacentTo(forward))
        ]);
        GameViewModel game = new(() => { }, session);

        game.MoveCommand.Execute(forward);
        game.MoveCommand.Execute(turn);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);

        Assert.AreEqual(start, session.Player.Position);
        Assert.AreEqual(forward, game.Player.FacingDirection);

        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);

        Assert.AreEqual(first, session.Player.Position);
        Assert.AreEqual(forward, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);

        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(second, session.Player.Position);
        Assert.AreEqual(forward, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);

        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(opening, session.Player.Position);
        Assert.AreEqual(opening.AdjacentTo(turn), session.Player.Motion.Destination);
        Assert.AreEqual(opening.Column * GameViewModel.CellSize, game.PlayerLeft);
        Assert.AreEqual(opening.Row * GameViewModel.CellSize, game.PlayerTop);
        Assert.AreEqual(turn, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
    }

    [DataTestMethod]
    [DataRow(0, 1, Direction.Right, Direction.Up)]
    [DataRow(4, 1, Direction.Right, Direction.Down)]
    [DataRow(1, 0, Direction.Down, Direction.Left)]
    [DataRow(1, 4, Direction.Down, Direction.Right)]
    public void MoveCommand_WhenTurnCrossesBoardEdge_ContinuesForward(
        int row,
        int column,
        Direction forward,
        Direction turn)
    {
        GridPosition start = new(row, column);
        GameSession session = new(new BoardSize(5, 5), start);
        GameViewModel game = new(() => { }, session);

        game.MoveCommand.Execute(forward);
        game.MoveCommand.Execute(turn);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(start.AdjacentTo(forward), session.Player.Position);
        Assert.AreEqual(start.AdjacentTo(forward).AdjacentTo(forward), session.Player.Motion.Destination);
        Assert.AreEqual(forward, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
    }

    [DataTestMethod]
    [DataRow(Direction.Down)]
    [DataRow(Direction.Left)]
    [DataRow(Direction.Right)]
    public void MoveCommand_WhenPendingTurnIsReplaced_UsesLatestInput(Direction latest)
    {
        GameSession session = new(new BoardSize(7, 7), new GridPosition(3, 1),
        [
            new Wall(new GridPosition(3, 2), new GridPosition(2, 2))
        ]);
        GameViewModel game = new(() => { }, session);

        game.MoveCommand.Execute(Direction.Right);
        game.MoveCommand.Execute(Direction.Up);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);
        game.MoveCommand.Execute(latest);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(new GridPosition(3, 3).AdjacentTo(latest), session.Player.Position);
        Assert.AreEqual(new GridPosition(3, 3).AdjacentTo(latest).AdjacentTo(latest), session.Player.Motion.Destination);
        Assert.AreEqual(latest, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Advance_WhenForwardWallIsReached_StopsInCurrentDirection(bool hasBlockedTurn)
    {
        GridPosition stop = new(2, 2);
        GameSession session = new(new BoardSize(5, 5), new GridPosition(2, 1),
        [
            new Wall(stop, stop.AdjacentTo(Direction.Right)),
            new Wall(stop, stop.AdjacentTo(Direction.Up))
        ]);
        GameViewModel game = new(() => { }, session);

        game.MoveCommand.Execute(Direction.Right);
        if (hasBlockedTurn)
        {
            game.MoveCommand.Execute(Direction.Up);
        }
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(stop, session.Player.Position);
        Assert.AreEqual(stop.Column * GameViewModel.CellSize, game.PlayerLeft);
        Assert.AreEqual(stop.Row * GameViewModel.CellSize, game.PlayerTop);
        Assert.AreEqual(Direction.Right, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Blocked, game.Player.AnimationState);

        game.Advance(TimeSpan.FromMilliseconds(210));
        game.Advance(TimeSpan.FromSeconds(1));

        Assert.AreEqual(stop, session.Player.Position);
        Assert.AreEqual(Direction.Right, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Idle, game.Player.AnimationState);

        game.MoveCommand.Execute(Direction.Down);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual(new GridPosition(3, 2), session.Player.Position);
        Assert.AreEqual(Direction.Down, game.Player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, game.Player.AnimationState);
    }

    [TestMethod]
    public void MoveCommand_WhileMoving_ContinuesInLatestDirectionUntilBlocked()
    {
        GameViewModel game = new(() => { }, new GameSession(TestMazes.Sample.Size, TestMazes.Sample.PlayerSpawn!.Value, TestMazes.Sample.Walls));

        game.MoveCommand.Execute(Direction.Right);
        game.MoveCommand.Execute(Direction.Down);
        game.MoveCommand.Execute(Direction.Left);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);

        Assert.AreEqual("Row 9, Column 6", game.PlayerPosition);
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
