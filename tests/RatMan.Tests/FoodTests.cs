using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class FoodTests
{
    private static MazeDefinition Maze => new("Food test", new(1, 4), [], new(0, 0), new(0, 3));

    [TestMethod]
    public void Layout_DefaultsToNormalExceptSpawn_AndHonorsOverrides()
    {
        var maze = Maze with { EmptyFoodTiles = [new(0, 1)], SteroidCheeseTiles = [new(0, 2)] };
        var food = FoodLayout.Create(maze);
        Assert.AreEqual(2, food.Count);
        Assert.AreEqual(FoodKind.Steroid, food[new(0, 2)]);
        Assert.AreEqual(FoodKind.Normal, food[new(0, 3)]); // Monster spawn still has food.
        Assert.AreEqual(3, FoodLayout.Create(maze with { PlayerSpawn = null }).Count);
    }

    [TestMethod]
    public void Layout_UsesReachabilityIncludingTunnels_WithoutChangingOverrides()
    {
        var maze = Maze with
        {
            Walls = [new(new(0, 1), new(0, 2))],
            EmptyFoodTiles = [new(0, 2)], SteroidCheeseTiles = [new(0, 3)]
        };
        Assert.AreEqual(1, FoodLayout.Create(maze).Count);
        var reopened = FoodLayout.Create(maze with { OpenBorderRows = [0] });
        Assert.AreEqual(2, reopened.Count);
        Assert.AreEqual(FoodKind.Steroid, reopened[new(0, 3)]);
        Assert.IsFalse(reopened.ContainsKey(new(0, 2)));
    }

    [TestMethod]
    public void Session_CollectsEachItemOnce_AndFreshSessionsReset()
    {
        var maze = Maze with { SteroidCheeseTiles = [new(0, 2)], EmptyFoodTiles = [new(0, 3)] };
        var session = new GameSession(maze);
        Assert.AreEqual(0, session.Score);
        Assert.IsNull(session.CollectFoodAtPlayer());
        Assert.IsFalse(session.TryMovePlayer(Direction.Left));
        Assert.IsNull(session.CollectFoodAtPlayer());
        Assert.IsTrue(session.TryMovePlayer(Direction.Right));
        Assert.AreEqual(new FoodCollection(new(0, 1), FoodKind.Normal, 1), session.CollectFoodAtPlayer());
        Assert.IsNull(session.CollectFoodAtPlayer());
        session.TryMovePlayer(Direction.Right);
        Assert.AreEqual(5, session.CollectFoodAtPlayer()!.Points);
        session.TryMovePlayer(Direction.Right);
        Assert.IsNull(session.CollectFoodAtPlayer());
        Assert.AreEqual(6, session.Score);
        Assert.AreEqual(0, session.RemainingFood.Count);
        Assert.IsTrue(session.TryMovePlayer(Direction.Left));
        Assert.IsNull(session.CollectFoodAtPlayer());
        var fresh = new GameSession(maze);
        Assert.AreEqual(0, fresh.Score);
        Assert.AreEqual(2, fresh.RemainingFood.Count);
        Assert.AreEqual(1, maze.SteroidCheeseTiles.Count);
    }

    [TestMethod]
    public void SpawnSuppression_PreservesSteroidOverrideForRelocation()
    {
        var maze = Maze with { SteroidCheeseTiles = [new(0, 0)] };
        Assert.IsFalse(new GameSession(maze).RemainingFood.ContainsKey(new(0, 0)));
        Assert.AreEqual(FoodKind.Steroid, FoodLayout.Create(maze with { PlayerSpawn = new(0, 1) })[new(0, 0)]);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GameView_CollectsOnlyAtArrival_BeforeNextMove(bool wraps)
    {
        // Isolate the home so this test measures collection, independent of cat contact.
        var maze = Maze with
        {
            Size = new(2, 4), MonsterSpawn = new(1, 3),
            Walls = Enumerable.Range(0, 4).Select(c => new Wall(new(0, c), new(1, c))).ToArray(),
            SteroidCheeseTiles = [new(0, 3)], OpenBorderRows = wraps ? [0] : []
        };
        var session = new GameSession(maze);
        var game = new GameViewModel(() => { }, session);
        int notifications = 0;
        game.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(game.Score)) notifications++; };
        game.MoveCommand.Execute(wraps ? Direction.Left : Direction.Right);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        Assert.AreEqual(0, game.Score);
        Assert.AreEqual(3, game.Food.Items.Count);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        Assert.AreEqual(wraps ? 5 : 1, game.Score);
        Assert.AreEqual(2, game.Food.Items.Count);
        Assert.AreEqual(1, notifications);
        Assert.IsFalse(game.Food.Items.Any(item => item.Position == new GridPosition(0, wraps ? 3 : 1)));
        Assert.AreEqual(RatMan.App.Animation.PlayerAnimationState.Walking, game.Player.AnimationState);
    }

    [TestMethod]
    public void GameView_LastFoodWins_BlockedAndFinishedDoNotScore()
    {
        var game = new GameViewModel(() => { }, new GameSession(new BoardSize(1, 2), new(0, 0)));
        game.MoveCommand.Execute(Direction.Left);
        game.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0, game.Score);
        game.MoveCommand.Execute(Direction.Right);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);
        Assert.AreEqual(1, game.Score);
        Assert.AreEqual(0, game.Food.Items.Count);
        Assert.IsTrue(game.IsFinished);
        Assert.AreEqual("You win!", game.OutcomeTitle);
        game.Advance(TimeSpan.FromSeconds(1));
        game.Advance(TimeSpan.FromSeconds(1));
        game.MoveCommand.Execute(Direction.Left);
        game.Advance(PlayerPresentationViewModel.DefaultMoveDuration);
        Assert.AreEqual(36d, game.PlayerLeft);
        Assert.AreEqual(1, game.Score);
    }

    [TestMethod]
    public void InvalidFoodOverrides_AreRejected()
    {
        Assert.IsTrue((Maze with { EmptyFoodTiles = [new(-1, 0)] }).Validate().Count > 0);
        Assert.IsTrue((Maze with { SteroidCheeseTiles = [new(0, 4)] }).Validate().Count > 0);
        var conflict = Maze with { EmptyFoodTiles = [new(0, 1)], SteroidCheeseTiles = [new(0, 1)] };
        Assert.IsTrue(conflict.Validate().Count > 0);
        Assert.ThrowsException<ArgumentException>(() => new GameSession(conflict));
    }
}
