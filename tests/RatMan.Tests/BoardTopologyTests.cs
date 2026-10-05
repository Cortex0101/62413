using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class BoardTopologyTests
{
    [DataTestMethod]
    [DataRow(Direction.Left, 1, 0, 1, 3)]
    [DataRow(Direction.Right, 1, 3, 1, 0)]
    [DataRow(Direction.Up, 0, 2, 2, 2)]
    [DataRow(Direction.Down, 2, 2, 0, 2)]
    public void OpenBorders_WrapInBothDirections(Direction direction, int row, int column, int endRow, int endColumn)
    {
        MazeDefinition maze = new("Wrap", new(3, 4), [], new(row, column), new(1, 1))
        { OpenBorderRows = [1], OpenBorderColumns = [2] };
        GameSession session = new(maze);
        Assert.IsTrue(session.TryMovePlayer(direction));
        Assert.AreEqual(new GridPosition(endRow, endColumn), session.Player.Position);
    }

    [TestMethod]
    public void Builder_FillsAndClearsWholeDisconnectedRegionLive()
    {
        BoardBuilderViewModel editor = new(new MemoryMazeRepository(), new TestEditorDialogs(), () => { }, () => { });
        editor.WidthInput = "3"; editor.HeightInput = "2"; editor.ApplyDimensions();
        editor.SelectedTool = BuilderTool.Player; editor.Place(18, 18);
        editor.SelectedTool = BuilderTool.Wall; editor.Rotate();
        editor.Place(36, 18);
        Assert.IsFalse(editor.BoardCells.Any(c => c.IsBlocked));
        editor.Place(36, 54);
        Assert.AreEqual(4, editor.BoardCells.Count(c => c.IsBlocked));
        // A border tunnel makes the region accessible even though the divider remains.
        editor.Remove(0, 18);
        CollectionAssert.AreEquivalent(new[] { 0 }, editor.Definition.OpenBorderRows.ToArray());
        Assert.IsFalse(editor.BoardCells.Any(c => c.IsBlocked));
        editor.Place(editor.BoardWidth, 18);
        Assert.AreEqual(0, editor.Definition.OpenBorderRows.Count);
        Assert.AreEqual(4, editor.BoardCells.Count(c => c.IsBlocked));
        editor.Remove(36, 54);
        Assert.IsFalse(editor.BoardCells.Any(c => c.IsBlocked));
    }

    [TestMethod]
    public void Builder_IsolatedCellAndPlayerRelocationUpdateFill()
    {
        BoardBuilderViewModel editor = new(new MemoryMazeRepository(), new TestEditorDialogs(), () => { }, () => { });
        editor.WidthInput = "3"; editor.HeightInput = "3"; editor.ApplyDimensions();
        editor.Place(54, 36); editor.Place(54, 72);
        editor.Rotate(); editor.Place(36, 54); editor.Place(72, 54);
        Assert.IsFalse(editor.BoardCells.Any(c => c.IsBlocked));
        editor.SelectedTool = BuilderTool.Player; editor.Place(18, 18);
        Assert.AreEqual(1, editor.BoardCells.Count(c => c.IsBlocked));
        Assert.IsTrue(editor.BoardCells[4].IsBlocked);
        editor.Place(54, 54);
        Assert.AreEqual(8, editor.BoardCells.Count(c => c.IsBlocked));
        Assert.IsFalse(editor.BoardCells[4].IsBlocked);
        editor.Remove(54, 54);
        Assert.IsFalse(editor.BoardCells.Any(c => c.IsBlocked));
    }

    [TestMethod]
    public void Builder_TopBottomBordersAndResizePreservePairs()
    {
        BoardBuilderViewModel editor = new(new MemoryMazeRepository(), new TestEditorDialogs(), () => { }, () => { });
        Assert.AreEqual(46, editor.RenderedWalls.Count);
        editor.Remove(54, editor.BoardHeight);
        Assert.AreEqual(44, editor.RenderedWalls.Count);
        editor.WidthInput = "20"; editor.HeightInput = "20"; editor.ApplyDimensions();
        CollectionAssert.AreEquivalent(new[] { 1 }, editor.Definition.OpenBorderColumns.ToArray());
        editor.Place(54, 0);
        Assert.AreEqual(0, editor.Definition.OpenBorderColumns.Count);
        Assert.AreEqual(80, editor.RenderedWalls.Count);
    }

    [TestMethod]
    public void SingleCellAxis_WrapsWithoutInvalidWallOrInfiniteFloodFill()
    {
        BoardTopology topology = new(new(1, 2), [new(new(0, 0), new(0, 1))], [0], [0]);
        Assert.IsTrue(topology.TryStep(new(0, 0), Direction.Up, out var destination));
        Assert.AreEqual(new GridPosition(0, 0), destination);
        Assert.AreEqual(2, topology.ReachableFrom(new(0, 0)).Count);
    }
}
