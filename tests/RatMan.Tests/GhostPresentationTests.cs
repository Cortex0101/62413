using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RatMan.App.Animation;
using RatMan.App.ViewModels;
using RatMan.App.Views;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class GhostPresentationTests
{
    public static IEnumerable<object[]> Sheets => CatSpriteSheet.Names.Select(name => new object[] { name });

    [DataTestMethod]
    [DynamicData(nameof(Sheets))]
    public void SpriteSheets_LoadEveryDirectionalFrame_WithTransparentPadding(string name)
    {
        var sheet = CatSpriteSheet.Create(name);
        var raw = new SpriteFrameProvider(sheet);
        var padded = new SpriteFrameProvider(sheet, 336);
        foreach (var direction in Enum.GetValues<Direction>())
        {
            var clip = sheet.GetClip($"walking-{direction.ToString().ToLowerInvariant()}");
            Assert.AreEqual(4, clip.Frames.Count);
            foreach (var frame in clip.Frames)
            {
                var bitmap = (BitmapSource)raw.GetFrame(frame);
                var bgra = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                byte[] pixels = new byte[bgra.PixelWidth * bgra.PixelHeight * 4];
                bgra.CopyPixels(pixels, bgra.PixelWidth * 4, 0);
                int opaque = Enumerable.Range(0, pixels.Length / 4).Count(i => pixels[i * 4 + 3] > 64);
                Assert.IsTrue(opaque > 1000, $"Empty or incorrect frame in {name}: {frame.Source}");
                var image = padded.GetFrame(frame);
                Assert.IsTrue(image.IsFrozen);
                Assert.AreEqual(336d, image.Width);
                Assert.AreEqual(336d, image.Height);
            }
        }
    }

    [TestMethod]
    public void Hud_TracksReleasePowerAndReturnStates_AndDisconnectedWarning()
    {
        var session = new GameSession(new MazeDefinition("Separated", new(2, 8),
            Enumerable.Range(0, 8).Select(c => new Wall(new(0, c), new(1, c))).ToArray(), new(0, 0), new(1, 7))
        { SteroidCheeseTiles = [new(0, 1)] });
        var vm = new GameViewModel(() => { }, session);
        StringAssert.Contains(vm.MapWarnings, "disconnected");
        CollectionAssert.AreEqual(new[] { "Blinky", "Pinky", "Inky", "Clyde" }, vm.Cats.Select(c => c.Name).ToArray());
        Assert.IsTrue(vm.Cats[0].IsVisible);
        Assert.IsFalse(vm.Cats[1].IsVisible);
        var normal = vm.PlayerImage;
        vm.MoveCommand.Execute(Direction.Right);
        vm.Advance(TimeSpan.FromMilliseconds(350));
        Assert.AreEqual(5, vm.Score);
        Assert.AreEqual(3, vm.Lives);
        Assert.AreNotSame(normal, vm.PlayerImage);
        StringAssert.Contains(vm.PowerText, "10s");
        Assert.IsTrue(vm.Cats[1].IsVisible);
        Assert.AreEqual("Scared · edible", vm.Cats[1].Status);
        session.Cats[0].State = CatState.ReturningHome;
        vm.Advance(TimeSpan.Zero);
        Assert.AreEqual(.45, vm.Cats[0].Opacity);
        Assert.AreEqual("Returning home", vm.Cats[0].Status);
    }

    [TestMethod]
    public void VictoryOverlayAndReplay_ResetSameMapAndStopFinishedAnimations()
    {
        var maze = new MazeDefinition("Replay me", new(1, 3), [], new(0, 0), new(0, 2)) { EmptyFoodTiles = [new(0, 2)] };
        var session = new GameSession(maze);
        var vm = new GameViewModel(() => { }, session);
        Assert.IsFalse(vm.ReplayCommand.CanExecute(null));
        vm.MoveCommand.Execute(Direction.Right);
        vm.Advance(session.Rules.PlayerMoveDuration);
        Assert.IsTrue(vm.IsFinished);
        Assert.IsTrue(vm.HasOverlay);
        Assert.AreEqual("You win!", vm.OutcomeTitle);
        Assert.AreEqual("Final score: 1", vm.OutcomeMessage);
        var frame = vm.Cats[0].CurrentImage;
        double left = vm.Cats[0].Left;
        vm.Advance(TimeSpan.FromSeconds(1));
        Assert.AreSame(frame, vm.Cats[0].CurrentImage);
        Assert.AreEqual(left, vm.Cats[0].Left);
        Assert.IsTrue(vm.ReplayCommand.CanExecute(null));
        vm.ReplayCommand.Execute(null);
        Assert.AreEqual("Replay me", vm.Title);
        Assert.IsFalse(vm.IsFinished);
        Assert.IsFalse(vm.HasOverlay);
        Assert.AreEqual(0, vm.Score);
        Assert.AreEqual(3, vm.Lives);
        Assert.AreEqual(1, vm.Food.Items.Count);
        Assert.AreEqual(0d, vm.PlayerLeft);
        Assert.IsFalse(vm.ReplayCommand.CanExecute(null));
        vm.MoveCommand.Execute(Direction.Right);
        vm.Advance(session.Rules.PlayerMoveDuration);
        Assert.AreEqual(1, vm.Score);
    }

    // Called inside the existing WPF test's single Application/STA lifetime.
    internal static void VerifyAndRender()
    {
        foreach (bool scared in new[] { false, true })
        {
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = scared ? "Scared cats — Down / Left / Right / Up" : "Cats — Down / Left / Right / Up", FontSize = 22, Margin = new(0, 0, 0, 12) });
            foreach (CatKind kind in Enum.GetValues<CatKind>())
                AddSpriteRows(panel, kind.ToString(), CatSpriteSheet.Create(CatSpriteSheet.Name(kind, scared)));
            FoodViewTests.Render(panel, scared ? "cats-scared-frames" : "cats-normal-frames", 560, 980);
        }
        var powered = new StackPanel { Margin = new Thickness(16) };
        AddSpriteRows(powered, "Powered Ratman", CatSpriteSheet.Create("ratman-steroid-spritesheet"));
        FoodViewTests.Render(powered, "ratman-powered-frames", 560, 260);

        var session = new GameSession(new MazeDefinition("Cat chase", new(6, 8), [], new(4, 1), new(1, 6))
        { SteroidCheeseTiles = [new(4, 2)] });
        var vm = new GameViewModel(() => { }, session);
        var view = new GameView { DataContext = vm };
        foreach (var (cat, index) in session.Cats.Select((cat, index) => (cat, index)))
        {
            cat.Motion.Reset(new(1 + index, 5));
            cat.State = CatState.Chase;
        }
        vm.Advance(TimeSpan.Zero);
        FoodViewTests.Render(view, "ghosts-chasing");
        vm.MoveCommand.Execute(Direction.Right);
        vm.Advance(TimeSpan.FromMilliseconds(350));
        Assert.AreEqual(5, vm.Score);
        FoodViewTests.Render(view, "ghosts-scattering");
        session.Cats[0].State = CatState.ReturningHome;
        vm.Advance(TimeSpan.Zero);
        FoodViewTests.Render(view, "ghost-returning");

        var loss = new GameSession(new MazeDefinition("Cat chase", new(3, 8), [], new(1, 0), new(1, 7)));
        var lostVm = new GameViewModel(() => { }, loss);
        var lostView = new GameView { DataContext = lostVm };
        for (int i = 0; i < 3; i++)
        {
            loss.Cats[0].Motion.Reset(loss.Player.Position);
            lostVm.Advance(GameRules.SimulationStep);
            if (i == 0) FoodViewTests.Render(lostView, "ghost-life-lost");
            if (i < 2) lostVm.Advance(loss.Rules.RespawnDelay);
        }
        Assert.AreEqual("Game over", lostVm.OutcomeTitle);
        Assert.AreEqual(0, lostVm.Lives);
        FoodViewTests.Render(lostView, "ghost-game-over");
        lostVm.ReplayCommand.Execute(null);
        Assert.IsFalse(lostVm.HasOverlay);
        FoodViewTests.Render(lostView, "ghost-replay");

        var win = new GameViewModel(() => { }, new GameSession(new MazeDefinition("Cat chase", new(1, 3), [], new(0, 0), new(0, 2))
        { EmptyFoodTiles = [new(0, 2)] }));
        win.MoveCommand.Execute(Direction.Right);
        win.Advance(TimeSpan.FromMilliseconds(350));
        FoodViewTests.Render(new GameView { DataContext = win }, "ghost-victory");
    }

    private static void AddSpriteRows(Panel panel, string label, SpriteSheetDefinition sheet)
    {
        var provider = new SpriteFrameProvider(sheet, 336);
        foreach (Direction direction in new[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up })
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Height = 56 };
            row.Children.Add(new TextBlock { Text = $"{label} · {direction}", Width = 170, VerticalAlignment = VerticalAlignment.Center });
            foreach (var frame in sheet.GetClip($"walking-{direction.ToString().ToLowerInvariant()}").Frames)
            {
                var image = new Image { Source = provider.GetFrame(frame), Width = 80, Height = 56 };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                row.Children.Add(image);
            }
            panel.Children.Add(row);
        }
    }
}
