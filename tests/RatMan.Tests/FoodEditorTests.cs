using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class FoodEditorTests
{
    private static MazeDefinition Maze => new("Food editor", new(2, 4), [], new(0, 0), new(1, 0));
    private static BoardBuilderViewModel Create(TestEditorDialogs? dialogs = null)
    {
        var editor = new BoardBuilderViewModel(new MemoryMazeRepository(), dialogs ?? new(), () => { }, () => { });
        editor.Load(Maze, "food.maze");
        return editor;
    }

    [TestMethod]
    public void FoodTools_ReplaceRemoveRestore_TrackDirty_AndPreserveWallsAndSpawns()
    {
        var editor = Create();
        editor.SelectedTool = BuilderTool.Wall;
        editor.Rotate(); editor.Place(36, 18);
        editor.Save();
        editor.SelectedTool = BuilderTool.SteroidCheese;
        editor.Place(54, 18);
        Assert.IsTrue(editor.IsDirty);
        Assert.AreEqual(FoodKind.Steroid, editor.Food.Items.Single(item => item.Position == new GridPosition(0, 1)).Kind);
        editor.Remove(37, 18); // Near a wall: a food tool must not remove that wall.
        Assert.AreEqual(1, editor.Definition.Walls.Count);
        Assert.AreEqual(1, editor.Definition.EmptyFoodTiles.Count);
        Assert.AreEqual(0, editor.Definition.SteroidCheeseTiles.Count);
        editor.SelectedTool = BuilderTool.NormalCheese; editor.Place(54, 18);
        Assert.AreEqual(0, editor.Definition.EmptyFoodTiles.Count);
        Assert.AreEqual(7, editor.Food.Items.Count);
        editor.Remove(18, 54);
        Assert.AreEqual(new GridPosition(1, 0), editor.MonsterSpawn);
        editor.Place(18, 18);
        Assert.AreEqual("The player starting tile stays empty.", editor.Status);
        Assert.IsFalse(editor.Food.Items.Any(item => item.Position == editor.PlayerSpawn));
    }

    [TestMethod]
    public void TopologyAndSpawnChanges_SuppressAndRestoreOverridesAndVariants()
    {
        var editor = Create();
        editor.SelectedTool = BuilderTool.SteroidCheese; editor.Place(126, 18);
        editor.Remove(126, 54);
        int variant = editor.Food.Items.Single(item => item.Position == new GridPosition(0, 2)).Variant;
        editor.SelectedTool = BuilderTool.Wall; editor.Rotate();
        editor.Place(72, 18); editor.Place(72, 54);
        Assert.AreEqual(3, editor.Food.Items.Count);
        editor.SelectedTool = BuilderTool.NormalCheese; editor.Place(126, 18);
        Assert.IsTrue(editor.Status.Contains("reachable"));
        Assert.AreEqual(1, editor.Definition.SteroidCheeseTiles.Count);
        editor.SelectedTool = BuilderTool.Wall; editor.Remove(72, 18);
        Assert.AreEqual(FoodKind.Steroid, editor.Food.Items.Single(item => item.Position == new GridPosition(0, 3)).Kind);
        Assert.AreEqual(variant, editor.Food.Items.Single(item => item.Position == new GridPosition(0, 2)).Variant);
        Assert.IsFalse(editor.Food.Items.Any(item => item.Position == new GridPosition(1, 3)));
        editor.SelectedTool = BuilderTool.Player; editor.Place(126, 18);
        Assert.IsFalse(editor.Food.Items.Any(item => item.Position == new GridPosition(0, 3)));
        Assert.IsTrue(editor.Food.Items.Any(item => item.Position == new GridPosition(0, 0)));
        editor.Place(18, 18);
        Assert.AreEqual(FoodKind.Steroid, editor.Food.Items.Single(item => item.Position == new GridPosition(0, 3)).Kind);
    }

    [TestMethod]
    public void Resize_ConfirmsFoodOnlyCrop_CancelPreserves_GrowthDefaultsToNormal()
    {
        var dialogs = new TestEditorDialogs { Crop = false };
        var editor = Create(dialogs);
        editor.SelectedTool = BuilderTool.SteroidCheese; editor.Place(126, 18); editor.Remove(126, 54);
        editor.WidthInput = "3";
        Assert.IsFalse(editor.ApplyDimensions());
        Assert.AreEqual(1, editor.Definition.SteroidCheeseTiles.Count);
        Assert.AreEqual(1, editor.Definition.EmptyFoodTiles.Count);
        dialogs.Crop = true; editor.WidthInput = "3";
        Assert.IsTrue(editor.ApplyDimensions());
        Assert.AreEqual(2, dialogs.CropPrompts);
        Assert.AreEqual(0, editor.Definition.SteroidCheeseTiles.Count);
        Assert.AreEqual(0, editor.Definition.EmptyFoodTiles.Count);
        editor.WidthInput = "4"; editor.ApplyDimensions();
        Assert.AreEqual(7, editor.Food.Items.Count);
        Assert.IsTrue(editor.Food.Items.All(item => item.Kind == FoodKind.Normal));
    }

    [TestMethod]
    public void SaveLoadAndNewBoard_PreserveOrResetFoodConfiguration()
    {
        var repository = new MemoryMazeRepository();
        var editor = new BoardBuilderViewModel(repository, new TestEditorDialogs(), () => { }, () => { });
        editor.Load(Maze, "food.maze");
        editor.SelectedTool = BuilderTool.SteroidCheese; editor.Place(54, 18); editor.Remove(90, 18);
        Assert.IsTrue(editor.Save());
        Assert.IsFalse(editor.IsDirty);
        var saved = repository.Load("food.maze");
        editor.Load(saved, "food.maze");
        Assert.AreEqual(1, editor.Definition.SteroidCheeseTiles.Count);
        Assert.AreEqual(1, editor.Definition.EmptyFoodTiles.Count);
        editor.NewBoardCommand.Execute(null);
        Assert.AreEqual(132, editor.Food.Items.Count);
        Assert.AreEqual(0, editor.Definition.SteroidCheeseTiles.Count);
        Assert.AreEqual(0, editor.Definition.EmptyFoodTiles.Count);
    }

    [TestMethod]
    public void FoodPreview_UsesCachedVariantAndAnimatedSteroid_RejectsSpawn()
    {
        var editor = Create();
        editor.SelectedTool = BuilderTool.NormalCheese; editor.Preview(54, 18);
        Assert.IsTrue(editor.HasFoodPreview);
        Assert.IsFalse(editor.HasPreviewCell);
        Assert.AreSame(editor.Food.Items.Single(item => item.Position == new GridPosition(0, 1)).NormalImage, editor.PreviewFoodImage);
        editor.SelectedTool = BuilderTool.SteroidCheese; editor.Preview(54, 18);
        var first = editor.PreviewFoodImage;
        editor.Advance(TimeSpan.FromMilliseconds(150));
        Assert.AreNotSame(first, editor.PreviewFoodImage);
        editor.Preview(18, 18);
        Assert.IsFalse(editor.HasFoodPreview);
        Assert.IsNull(editor.PreviewFoodImage);
    }

    [TestMethod]
    public void LoadingDifferentDimensions_ClearsActivePreviewBeforeAnimationReset()
    {
        var editor = Create();
        editor.SelectedTool = BuilderTool.SteroidCheese; editor.Preview(126, 54);
        // Bindings can read this property synchronously as the animator resets.
        editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(editor.PreviewFoodImage)) _ = editor.PreviewFoodImage;
        };
        editor.Load(Maze with { Size = new(20, 30) }, "large.maze");
        Assert.IsFalse(editor.HasFoodPreview);
        Assert.AreEqual(599, editor.Food.Items.Count);
    }
}
