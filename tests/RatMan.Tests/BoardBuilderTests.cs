using RatMan.App.Services;
using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class BoardBuilderTests
{
    private static BoardBuilderViewModel Create(MemoryMazeRepository? repository = null, TestEditorDialogs? dialogs = null) =>
        new(repository ?? new(), dialogs ?? new(), () => { }, () => { });

    [TestMethod]
    public void NewBoard_HasExpectedSizeAndRequiresBothSpawnsAndName()
    {
        var editor = Create();
        Assert.AreEqual(new BoardSize(11, 12), editor.Size);
        Assert.AreEqual(3, editor.Definition.Validate().Count);
        Assert.IsFalse(editor.Save());
    }

    [TestMethod]
    public void Walls_SnapRotateDeduplicateAndRemoveRegardlessOfTool()
    {
        var editor = Create();
        editor.Place(18, 40); editor.Place(18, 40);
        editor.Place(18, 18); editor.Place(-1, 36); editor.Place(18, 0);
        Assert.AreEqual(1, editor.Definition.Walls.Count);
        editor.Rotate(); editor.Place(40, 18);
        Assert.AreEqual(2, editor.Definition.Walls.Count);
        editor.SelectedTool = BuilderTool.Monster;
        editor.Remove(40, 18); editor.Remove(18, 40);
        Assert.AreEqual(0, editor.Definition.Walls.Count);
    }

    [TestMethod]
    public void Preview_UsesOrientationAndRejectsOutsideBoardAndCellCenters()
    {
        var editor = Create();
        editor.Preview(18, 36);
        Assert.AreEqual(1, editor.PreviewSegments.Count);
        editor.Rotate(); editor.Preview(18, 36);
        Assert.AreEqual(0, editor.PreviewSegments.Count);
        editor.Preview(36, 18);
        Assert.AreEqual(1, editor.PreviewSegments.Count);
        editor.SelectedTool = BuilderTool.Player; editor.Preview(54, 90);
        Assert.IsTrue(editor.HasPreviewCell);
        Assert.AreEqual(36d, editor.PreviewLeft);
        Assert.AreEqual(72d, editor.PreviewTop);
        editor.Preview(-1, 0);
        Assert.IsFalse(editor.HasPreviewCell);
    }

    [TestMethod]
    public void Spawns_MoveAndMustOccupyDifferentCells()
    {
        var editor = Create(); editor.BoardName = "Board";
        editor.SelectedTool = BuilderTool.Player; editor.Place(18, 18); editor.Place(54, 18);
        Assert.AreEqual(new GridPosition(0, 1), editor.PlayerSpawn);
        editor.SelectedTool = BuilderTool.Monster; editor.Place(54, 18);
        Assert.IsFalse(editor.Save());
        editor.Place(18, 18);
        Assert.IsTrue(editor.Save());
        editor.Remove(18, 18);
        Assert.IsNull(editor.MonsterSpawn);
        Assert.IsFalse(editor.Save());
    }

    [TestMethod]
    public void Shrink_CanCancelOrCropAndClearLostSpawns()
    {
        var dialogs = new TestEditorDialogs { Crop = false };
        var editor = Create(dialogs: dialogs);
        editor.Load(TestMazes.Sample, "sample.maze");
        editor.WidthInput = "4";
        Assert.IsFalse(editor.ApplyDimensions());
        Assert.AreEqual("10", editor.WidthInput);
        Assert.AreEqual(TestMazes.Sample.PlayerSpawn, editor.PlayerSpawn);
        dialogs.Crop = true; editor.WidthInput = "4";
        Assert.IsTrue(editor.ApplyDimensions());
        Assert.IsNull(editor.PlayerSpawn); Assert.IsNull(editor.MonsterSpawn);
        Assert.AreEqual(1, editor.Definition.Walls.Count);
        Assert.AreEqual(2, dialogs.CropPrompts);
        Assert.IsFalse(editor.Save());
    }

    [TestMethod]
    public void Grow_PreservesContentsWithoutPrompt()
    {
        var dialogs = new TestEditorDialogs(); var editor = Create(dialogs: dialogs);
        editor.Load(TestMazes.Sample, "sample.maze");
        editor.WidthInput = "20"; editor.HeightInput = "30";
        Assert.IsTrue(editor.ApplyDimensions());
        Assert.AreEqual(0, dialogs.CropPrompts);
        Assert.AreEqual(TestMazes.Sample.PlayerSpawn, editor.PlayerSpawn);
        Assert.AreEqual(3, editor.Definition.Walls.Count);
    }

    [TestMethod]
    public void CropDialogFocusChange_DoesNotOpenSecondConfirmation()
    {
        var dialogs = new TestEditorDialogs();
        var editor = Create(dialogs: dialogs);
        editor.Load(TestMazes.Sample, "sample.maze");
        dialogs.OnCrop = () => Assert.IsFalse(editor.ApplyDimensions());
        editor.WidthInput = "4";
        Assert.IsTrue(editor.ApplyDimensions());
        Assert.AreEqual(1, dialogs.CropPrompts);
    }

    [TestMethod]
    public void EditingWalls_PreservesGridProjectionUntilDimensionsChange()
    {
        var editor = Create(); var cells = editor.BoardCells;
        editor.Place(18, 36);
        Assert.AreSame(cells, editor.BoardCells);
        editor.WidthInput = "20"; editor.ApplyDimensions();
        Assert.AreNotSame(cells, editor.BoardCells);
    }

    [DataTestMethod]
    [DataRow("0")][DataRow("-2")][DataRow("101")][DataRow("1.5")][DataRow("abc")][DataRow("")]
    public void InvalidDimensions_DoNotChangeBoardOrSave(string value)
    {
        var editor = Create(); editor.WidthInput = value;
        Assert.IsFalse(editor.ApplyDimensions()); Assert.IsFalse(editor.Save());
        Assert.AreEqual(new BoardSize(11, 12), editor.Size);
    }

    [TestMethod]
    public void Save_RenameRetainsFileAndFailureRetainsDirtyDraft()
    {
        var repository = new MemoryMazeRepository(TestMazes.Sample);
        var editor = Create(repository);
        string path = repository.Files.Keys.Single();
        editor.Load(TestMazes.Sample, path); editor.BoardName = "Renamed";
        Assert.IsTrue(editor.Save()); Assert.AreEqual(path, editor.FilePath);
        Assert.AreEqual("Renamed", repository.Files[path].Name);
        Assert.IsFalse(editor.IsDirty);
        repository.FailSave = true; editor.BoardName = "Latest draft";
        Assert.IsFalse(editor.Save()); Assert.IsTrue(editor.IsDirty);
        Assert.AreEqual("Latest draft", editor.BoardName);
        Assert.AreEqual("Renamed", repository.Files[path].Name);
    }

    [TestMethod]
    public void NewFileCollision_RequiresConfirmation()
    {
        var repository = new MemoryMazeRepository(TestMazes.Sample);
        var dialogs = new TestEditorDialogs { Overwrite = false };
        var editor = Create(repository, dialogs); editor.BoardName = TestMazes.Sample.Name;
        editor.SelectedTool = BuilderTool.Player; editor.Place(18, 18);
        editor.SelectedTool = BuilderTool.Monster; editor.Place(54, 18);
        Assert.IsFalse(editor.Save()); Assert.IsNull(editor.FilePath);
        Assert.AreEqual(1, dialogs.OverwritePrompts);
        dialogs.Overwrite = true;
        Assert.IsTrue(editor.Save()); Assert.AreEqual(1, repository.Files.Count);
    }

    [TestMethod]
    public void UnsavedNavigation_SaveDiscardCancelAndFailedSave()
    {
        var repository = new MemoryMazeRepository();
        var dialogs = new TestEditorDialogs(); int leaves = 0;
        var editor = new BoardBuilderViewModel(repository, dialogs, () => leaves++, () => leaves++);
        editor.BoardName = "Draft";
        editor.ReturnToMenuCommand.Execute(null); Assert.AreEqual(0, leaves);
        dialogs.Unsaved = UnsavedChoice.Save;
        editor.LoadBoardCommand.Execute(null); Assert.AreEqual(0, leaves);
        dialogs.Unsaved = UnsavedChoice.Discard;
        editor.ReturnToMenuCommand.Execute(null); Assert.AreEqual(1, leaves);
        editor.Load(TestMazes.Sample, "sample.maze"); editor.BoardName = "Saved";
        dialogs.Unsaved = UnsavedChoice.Save;
        editor.ReturnToMenuCommand.Execute(null); Assert.AreEqual(2, leaves);
        Assert.AreEqual("Saved", repository.Files["sample.maze"].Name);
    }
}
