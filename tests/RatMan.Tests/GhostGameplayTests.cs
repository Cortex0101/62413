using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class GhostGameplayTests
{
    private static readonly TimeSpan Step = GameRules.SimulationStep;

    // One cheese lane and a separate cat lane make timing tests independent of combat.
    private static MazeDefinition SafeMaze(int food = 20) => new("Timing lanes", new(2, food + 1),
        Enumerable.Range(0, food + 1).Select(c => new Wall(new(0, c), new(1, c))).ToArray(), new(0, 0), new(1, food));

    private static GameSession Arena(params GridPosition[] steroid) => new(new MazeDefinition("Arena", new(5, 12), [], new(2, 1), new(2, 10))
    { SteroidCheeseTiles = steroid });

    [DataTestMethod]
    [DataRow(20, 2, 5, 10)]
    [DataRow(7, 1, 2, 4)]
    [DataRow(1, 1, 1, 1)]
    public void Releases_CountCheeseAndRoundUp(int food, int pinky, int inky, int clyde)
    {
        var session = new GameSession(SafeMaze(food) with { SteroidCheeseTiles = [new(0, 1)] });
        CollectionAssert.AreEqual(new[] { 0, pinky, inky, clyde }, session.Cats.Select(c => c.ReleaseCount).ToArray());
        Assert.AreEqual(CatState.Chase, session.Cats[0].State);
        Assert.IsTrue(session.Cats.Skip(1).All(c => c.State == CatState.Waiting));
        for (int count = 1; count <= food; count++)
        {
            session.TryMovePlayer(Direction.Right);
            session.CollectFoodAtPlayer();
            Assert.AreEqual(count, session.CollectedCheeseCount);
            Assert.AreEqual(count + 4, session.Score);
            foreach (var cat in session.Cats)
                Assert.AreEqual(count >= cat.ReleaseCount ? CatState.Scatter : CatState.Waiting, cat.State);
        }
    }

    [TestMethod]
    public void Power_LastTenSeconds_RefreshesAndNewReleasesAreScared()
    {
        var session = new GameSession(SafeMaze() with { SteroidCheeseTiles = [new(0, 1), new(0, 2)] });
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        session.Advance(TimeSpan.FromMilliseconds(9990));
        Assert.AreEqual(Step, session.PowerRemaining);
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        Assert.AreEqual(TimeSpan.FromSeconds(10), session.PowerRemaining);
        Assert.AreEqual(CatState.Scatter, session.Cats[1].State);
        session.Advance(TimeSpan.FromSeconds(10));
        Assert.IsFalse(session.IsPowered);
        Assert.IsTrue(session.Cats.Take(2).All(c => c.State == CatState.Chase));
    }

    [TestMethod]
    public void SpeedChanges_ApplyAtNextTileBoundary()
    {
        var session = new GameSession(SafeMaze() with { SteroidCheeseTiles = [new(0, 1)] });
        var cat = session.Cats[0];
        session.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(.25, cat.Motion.Progress);
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        Assert.AreEqual(CatState.Scatter, cat.State);
        session.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(.5, cat.Motion.Progress); // Existing chase edge still takes 400 ms.
        session.Advance(TimeSpan.FromMilliseconds(260));
        Assert.AreEqual(.1, cat.Motion.Progress, .000001); // Next edge takes 600 ms.
        cat.State = CatState.ReturningHome;
        session.Advance(TimeSpan.FromMilliseconds(540));
        Assert.AreEqual(Direction.Right, cat.Motion.FacingDirection); // Return may reverse.
        session.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(.5, cat.Motion.Progress); // Next edge takes 200 ms.
    }

    [TestMethod]
    public void SeveralEdibleCats_AwardFlatPointsEach_Once()
    {
        var session = new GameSession(SafeMaze() with { SteroidCheeseTiles = [new(0, 1)] });
        for (int i = 0; i < 10; i++)
        {
            session.TryMovePlayer(Direction.Right);
            session.CollectFoodAtPlayer();
        }
        Assert.IsTrue(session.Cats.All(cat => cat.State == CatState.Scatter));
        foreach (var cat in session.Cats) cat.Motion.Reset(session.Player.Position);
        session.Advance(Step);
        Assert.AreEqual(14 + 4 * 20, session.Score);
        Assert.AreEqual(10, session.CollectedCheeseCount);
        Assert.IsTrue(session.Cats.All(cat => cat.State == CatState.ReturningHome));
        session.Advance(Step);
        Assert.AreEqual(94, session.Score);
    }

    [DataTestMethod]
    [DataRow(CatState.Waiting)]
    [DataRow(CatState.ReturningHome)]
    public void WaitingAndReturningCats_CannotCatchRatman(CatState state)
    {
        var session = Arena();
        var cat = session.Cats[3];
        cat.Motion.Reset(session.Player.Position);
        cat.State = state;
        session.Advance(Step);
        Assert.AreEqual(3, session.Lives);
        Assert.AreEqual(0, session.Score);
    }

    [TestMethod]
    public void EatingCat_AwardsTwentyOnce_AndDoesNotAffectReleases()
    {
        var session = Arena(new GridPosition(2, 2));
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        var cat = session.Cats[0];
        cat.Motion.Reset(session.Player.Position);
        session.Advance(Step);
        Assert.AreEqual(25, session.Score);
        Assert.AreEqual(1, session.CollectedCheeseCount);
        Assert.AreEqual(CatState.Waiting, session.Cats[1].State);
        Assert.AreEqual(CatState.ReturningHome, cat.State);
        Assert.AreEqual(3, session.Lives);
        session.Advance(Step);
        Assert.AreEqual(25, session.Score);
        Assert.AreEqual(.05, cat.Motion.Progress, .000001);
    }

    [TestMethod]
    public void EatenCats_WaitAtHomeUntilPowerEnds_AndRefreshExtendsRecovery()
    {
        var session = new GameSession(SafeMaze() with { SteroidCheeseTiles = [new(0, 1), new(0, 2)] });
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        var cat = session.Cats[0];
        cat.Motion.Reset(session.MonsterStartingPosition!.Value);
        cat.State = CatState.ReturningHome;
        session.Advance(TimeSpan.FromSeconds(9));
        Assert.AreEqual(CatState.Recovering, cat.State);
        Assert.IsFalse(cat.Motion.IsMoving);
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        session.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(CatState.Recovering, cat.State);
        session.Advance(TimeSpan.FromSeconds(9));
        Assert.AreEqual(CatState.Chase, cat.State);
        Assert.IsTrue(cat.Motion.IsMoving);
    }

    [TestMethod]
    public void ReturnTrip_RemainsHarmlessAfterPowerExpires_AndChasesOnArrival()
    {
        var session = Arena(new GridPosition(2, 2));
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        var cat = session.Cats[0];
        cat.State = CatState.ReturningHome;
        cat.Motion.Reset(new(2, 9));
        cat.Motion.Begin(new(2, 10), Direction.Right, false, TimeSpan.FromSeconds(11));
        session.Advance(TimeSpan.FromSeconds(10));
        Assert.AreEqual(CatState.ReturningHome, cat.State);
        Assert.IsFalse(session.IsPowered);
        session.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(CatState.Chase, cat.State);
        Assert.IsTrue(cat.Motion.IsMoving);
        Assert.AreEqual(3, session.Lives);
    }

    [TestMethod]
    public void ContactOnSteroidArrival_AppliesPowerBeforeContact()
    {
        var session = Arena(new GridPosition(2, 2));
        session.RequestDirection(Direction.Right);
        session.Advance(TimeSpan.FromMilliseconds(340));
        // Arrange simultaneous arrivals from perpendicular corridors.
        var cat = session.Cats[0];
        cat.Motion.Reset(new(1, 2));
        cat.Motion.Begin(new(2, 2), Direction.Down, false, Step);
        session.Advance(Step);
        Assert.AreEqual(3, session.Lives);
        Assert.AreEqual(25, session.Score);
        Assert.AreEqual(CatState.ReturningHome, cat.State);
        Assert.AreEqual(TimeSpan.FromSeconds(10), session.PowerRemaining);
    }

    [TestMethod]
    public void ContactBeforeSteroidArrival_DoesNotAwardUnreachedFood()
    {
        var session = Arena(new GridPosition(2, 2));
        session.RequestDirection(Direction.Right);
        var cat = session.Cats[0];
        cat.Motion.Reset(new(2, 2));
        cat.Motion.Begin(new(2, 1), Direction.Left, false, session.Rules.ChaseMoveDuration);
        session.Advance(TimeSpan.FromMilliseconds(190));
        Assert.AreEqual(RoundStatus.LifeLost, session.Status);
        Assert.AreEqual(0, session.Score);
        Assert.IsTrue(session.RemainingFood.ContainsKey(new(2, 2)));
    }

    [TestMethod]
    public void CollisionAtPowerExpiry_IsDangerous()
    {
        var session = new GameSession(SafeMaze() with { SteroidCheeseTiles = [new(0, 1)] });
        session.TryMovePlayer(Direction.Right);
        session.CollectFoodAtPlayer();
        session.Advance(TimeSpan.FromMilliseconds(9990));
        var cat = session.Cats[0];
        cat.Motion.Reset(new(0, 2));
        cat.Motion.Begin(new(0, 1), Direction.Left, false, Step);
        session.Advance(Step);
        Assert.AreEqual(2, session.Lives);
        Assert.AreEqual(RoundStatus.LifeLost, session.Status);
    }

    [TestMethod]
    public void CatchResetsActorsAfterDelay_PreservingFoodScoreAndReleaseEligibility()
    {
        var session = Arena();
        for (int i = 0; i < 6; i++)
        {
            session.TryMovePlayer(Direction.Right);
            session.CollectFoodAtPlayer();
        }
        int remaining = session.RemainingFood.Count;
        Assert.AreEqual(CatState.Chase, session.Cats[1].State);
        foreach (var cat in session.Cats.Take(2)) cat.Motion.Reset(session.Player.Position);
        session.RequestDirection(Direction.Up);
        session.Advance(Step);
        Assert.AreEqual(2, session.Lives); // Two cats still cost only one life.
        Assert.AreEqual(RoundStatus.LifeLost, session.Status);
        session.RequestDirection(Direction.Left); // Ignored during reset.
        session.Advance(TimeSpan.FromMilliseconds(990));
        Assert.AreEqual(RoundStatus.LifeLost, session.Status);
        session.Advance(Step);
        Assert.AreEqual(RoundStatus.Playing, session.Status);
        Assert.AreEqual(new GridPosition(2, 1), session.Player.Position);
        Assert.IsFalse(session.Player.Motion.IsMoving);
        Assert.AreEqual(6, session.Score);
        Assert.AreEqual(remaining, session.RemainingFood.Count);
        Assert.AreEqual(CatState.Chase, session.Cats[1].State);
        Assert.AreEqual(CatState.Waiting, session.Cats[2].State);
    }

    [TestMethod]
    public void ThreeCatches_EndRound_AndRestartRestoresEverything()
    {
        var session = Arena();
        for (int life = 3; life > 0; life--)
        {
            session.Cats[0].Motion.Reset(session.Player.Position);
            session.Advance(Step);
            Assert.AreEqual(life - 1, session.Lives);
            if (life > 1) session.Advance(session.Rules.RespawnDelay);
        }
        Assert.AreEqual(RoundStatus.GameOver, session.Status);
        var position = session.Player.Position;
        session.RequestDirection(Direction.Right);
        session.Advance(TimeSpan.FromSeconds(100));
        Assert.AreEqual(position, session.Player.Position);
        session.Restart();
        Assert.AreEqual(RoundStatus.Playing, session.Status);
        Assert.AreEqual(3, session.Lives);
        Assert.AreEqual(0, session.Score);
        Assert.AreEqual(session.InitialCheeseCount, session.RemainingFood.Count);
        Assert.AreEqual(CatState.Chase, session.Cats[0].State);
        Assert.IsTrue(session.Cats.Skip(1).All(c => c.State == CatState.Waiting));
    }

    [TestMethod]
    public void LastCheeseWinsOverSimultaneousCatch_AndEmptyMapWinsOnFirstTick()
    {
        var maze = new MazeDefinition("Last cheese", new(1, 3), [], new(0, 0), new(0, 2))
        { EmptyFoodTiles = [new(0, 2)] };
        var session = new GameSession(maze);
        session.RequestDirection(Direction.Right);
        session.Cats[0].Motion.Reset(new(0, 2));
        session.Cats[0].Motion.Begin(new(0, 1), Direction.Left, false, session.Rules.PlayerMoveDuration);
        session.Advance(session.Rules.PlayerMoveDuration);
        Assert.AreEqual(RoundStatus.Won, session.Status);
        Assert.AreEqual(3, session.Lives);
        Assert.AreEqual(1, session.Score);
        var empty = new GameSession(maze with { EmptyFoodTiles = [new(0, 1), new(0, 2)] });
        Assert.AreEqual(RoundStatus.Playing, empty.Status);
        empty.Advance(Step);
        Assert.AreEqual(RoundStatus.Won, empty.Status);
    }

    [TestMethod]
    public void TunnelCrossings_CatchPlayerWithoutSlidingAcrossBoard()
    {
        var session = new GameSession(new MazeDefinition("Tunnel", new(1, 5), [], new(0, 0), new(0, 4)) { OpenBorderRows = [0] });
        session.RequestDirection(Direction.Left);
        Assert.AreEqual(5d, session.Player.Motion.Column);
        session.Advance(TimeSpan.FromMilliseconds(190));
        Assert.AreEqual(RoundStatus.LifeLost, session.Status);
        Assert.AreEqual(0, session.CollectedCheeseCount);
    }

    [TestMethod]
    public void FixedStep_IsIndependentOfElapsedPartitions_IncludingPowerAndDeaths()
    {
        foreach (var maze in new[] { SafeMaze() with { SteroidCheeseTiles = [new(0, 1)] },
            new MazeDefinition("Danger", new(1, 8), [], new(0, 0), new(0, 7)) })
        {
            var whole = new GameSession(maze);
            var pieces = new GameSession(maze);
            whole.RequestDirection(Direction.Right);
            pieces.RequestDirection(Direction.Right);
            whole.Advance(TimeSpan.FromMilliseconds(11007));
            for (int i = 0; i < 1572; i++) pieces.Advance(TimeSpan.FromMilliseconds(7));
            pieces.Advance(TimeSpan.FromMilliseconds(3));
            Assert.AreEqual(whole.Status, pieces.Status);
            Assert.AreEqual(whole.Score, pieces.Score);
            Assert.AreEqual(whole.Lives, pieces.Lives);
            Assert.AreEqual(whole.PowerRemaining, pieces.PowerRemaining);
            Assert.AreEqual(whole.Player.Position, pieces.Player.Position);
            Assert.AreEqual(whole.Player.Motion.Progress, pieces.Player.Motion.Progress);
            foreach (var (a, b) in whole.Cats.Zip(pieces.Cats))
            {
                Assert.AreEqual(a.Position, b.Position);
                Assert.AreEqual(a.State, b.State);
                Assert.AreEqual(a.Motion.Progress, b.Motion.Progress);
            }
        }
    }

    [TestMethod]
    public void Advance_CarriesFractionalTime_CollectsOnlyOnArrival_AndHandlesSeveralMoves()
    {
        var session = new GameSession(SafeMaze());
        session.RequestDirection(Direction.Right);
        session.Advance(TimeSpan.FromMilliseconds(349));
        Assert.AreEqual(new GridPosition(0, 0), session.Player.Position);
        Assert.AreEqual(0, session.Score);
        session.Advance(TimeSpan.FromMilliseconds(1));
        Assert.AreEqual(new GridPosition(0, 1), session.Player.Position);
        Assert.AreEqual(1, session.Score);
        session.Advance(TimeSpan.FromMilliseconds(1050));
        Assert.AreEqual(new GridPosition(0, 4), session.Player.Position);
        Assert.AreEqual(4, session.Score);
        Assert.IsTrue(session.IsMonsterDisconnected);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => session.Advance(TimeSpan.FromMilliseconds(-1)));
    }
}
