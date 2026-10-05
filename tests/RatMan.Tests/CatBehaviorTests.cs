using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class CatBehaviorTests
{
    [DataTestMethod]
    [DataRow(Direction.Up, 3, 3)]
    [DataRow(Direction.Down, 7, 5)]
    [DataRow(Direction.Left, 5, 3)]
    [DataRow(Direction.Right, 5, 7)]
    public void Pinky_TargetsTwoAhead_WithUpwardOffset(Direction direction, int row, int column)
    {
        var player = new Player(new(5, 5));
        player.Motion.Begin(new GridPosition(5, 5).AdjacentTo(direction), direction, false, TimeSpan.FromMilliseconds(350));
        Assert.AreEqual(new GridPosition(row, column),
            CatTargets.For(CatKind.Pinky).Target(new(CatKind.Pinky, new(0, 0), 0), player, new(1, 1), new(12, 12)));
    }

    [TestMethod]
    public void BlinkyAndInky_UseCommittedRatmanAndBlinkyPositions()
    {
        var player = new Player(new(5, 5));
        player.Motion.Begin(new(5, 6), Direction.Right, false, TimeSpan.FromMilliseconds(350));
        var cat = new Cat(CatKind.Inky, new(0, 0), 0);
        Assert.AreEqual(new GridPosition(5, 5), CatTargets.For(CatKind.Blinky).Target(cat, player, new(1, 2), new(12, 12)));
        Assert.AreEqual(new GridPosition(9, 12), CatTargets.For(CatKind.Inky).Target(cat, player, new(1, 2), new(12, 12)));
    }

    [DataTestMethod]
    [DataRow(0, 7, 19, 0)]
    [DataRow(0, 8, 0, 0)]
    [DataRow(5, 6, 19, 0)]
    [DataRow(6, 6, 0, 0)]
    public void Clyde_UsesEuclideanEightTileBoundary(int row, int column, int targetRow, int targetColumn)
    {
        var cat = new Cat(CatKind.Clyde, new(row, column), 0);
        Assert.AreEqual(new GridPosition(targetRow, targetColumn),
            CatTargets.For(CatKind.Clyde).Target(cat, new(new(0, 0)), new(1, 1), new(20, 20)));
    }

    [DataTestMethod]
    [DataRow(CatKind.Blinky, 0, 9)]
    [DataRow(CatKind.Pinky, 0, 0)]
    [DataRow(CatKind.Inky, 7, 9)]
    [DataRow(CatKind.Clyde, 7, 0)]
    public void ScatterCorners_ScaleWithBoard(CatKind kind, int row, int column) =>
        Assert.AreEqual(new GridPosition(row, column), CatTargets.Corner(kind, new(8, 10)));

    [TestMethod]
    public void Navigation_RoutesAroundWalls_AndBreaksTiesUpThenLeft()
    {
        var open = new CatNavigator(new(new(5, 5), [], [], []), new(2, 2));
        Assert.AreEqual(Direction.Up, open.ChooseDirection(new(2, 2), new(0, 0), null));
        var blocked = new CatNavigator(new(new(5, 5), [new(new(2, 2), new(1, 2))], [], []), new(2, 2));
        Assert.AreEqual(Direction.Left, blocked.ChooseDirection(new(2, 2), new(0, 2), null));
    }

    [TestMethod]
    public void Navigation_UsesTunnels_WithoutCrossingClosedBorders()
    {
        var open = new CatNavigator(new(new(1, 5), [], [0], []), new(0, 0));
        var closed = new CatNavigator(new(new(1, 5), [], [], []), new(0, 0));
        Assert.AreEqual(Direction.Left, open.ChooseDirection(new(0, 0), new(0, 4), null));
        Assert.AreEqual(Direction.Right, closed.ChooseDirection(new(0, 0), new(0, 4), null));
    }

    [TestMethod]
    public void Navigation_AvoidsReversal_ExceptDeadEndsAndReturnHome()
    {
        var navigator = new CatNavigator(new(new(1, 5), [], [], []), new(0, 0));
        Assert.AreEqual(Direction.Right, navigator.ChooseDirection(new(0, 2), new(0, 0), Direction.Right));
        Assert.AreEqual(Direction.Left, navigator.ChooseDirection(new(0, 2), new(0, 0), Direction.Right, returning: true));
        Assert.AreEqual(Direction.Left, navigator.ChooseDirection(new(0, 4), new(0, 0), Direction.Right));
        Assert.AreEqual(Direction.Right, navigator.ChooseDirection(new(0, 0), new(0, 0), null));
    }

    [TestMethod]
    public void Navigation_ResolvesUnreachableAndOutsideTargets_Deterministically()
    {
        Wall[] walls = Enumerable.Range(0, 3).Select(row => new Wall(new(row, 0), new(row, 1))).ToArray();
        var navigator = new CatNavigator(new(new(3, 3), walls, [], []), new(0, 0));
        Assert.IsFalse(navigator.CanReach(new(1, 2)));
        Assert.AreEqual(new GridPosition(1, 0), navigator.ResolveTarget(new(1, 2)));
        Assert.AreEqual(new GridPosition(0, 0), navigator.ResolveTarget(new(-10, 10)));
        var isolated = new CatNavigator(new(new(1, 1), [], [0], [0]), new(0, 0));
        Assert.IsNull(isolated.ChooseDirection(new(0, 0), new(5, 5), null));
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Collision_RecognizesInteriorCrossingsAndOvertakes(bool tunnel)
    {
        var a = new MovementSegment(new(0, 0), new(0, 1), true, tunnel, .4, .6);
        var opposite = new MovementSegment(new(0, 1), new(0, 0), true, tunnel, .4, .6);
        var ahead = a with { Start = .5, End = .55 };
        Assert.AreEqual(.5, a.ContactTime(opposite)!.Value, .000001);
        Assert.IsTrue(a.ContactTime(ahead).HasValue);
        Assert.IsNull(a.ContactTime(opposite with { Wraps = !tunnel }));
    }

    [TestMethod]
    public void Collision_UsesActualCenters_NotReservedDestinationsOrAdjacentCorridors()
    {
        var moving = new MovementSegment(new(0, 0), new(0, 1), true, false, .2, .4);
        var standing = new MovementSegment(new(0, 1), new(0, 1), false, false, 0, 0);
        Assert.IsNull(moving.ContactTime(standing));
        Assert.AreEqual(1d, (moving with { End = 1 }).ContactTime(standing));
        Assert.IsNull(moving.ContactTime(moving with { From = new(1, 0), To = new(1, 1) }));
        Assert.AreEqual(0d, standing.ContactTime(standing));
    }
}
