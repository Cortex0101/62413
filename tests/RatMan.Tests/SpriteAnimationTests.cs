using System.Windows.Media;
using System.Windows.Media.Imaging;
using RatMan.App.Animation;
using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class SpriteAnimationTests
{
    [TestMethod]
    public void PlayerPresentation_WrapEntersFromOppositeEdge()
    {
        var model = new Player(new(1, 0));
        PlayerPresentationViewModel player = new(model, 36);
        model.Motion.Begin(new(1, 3), Direction.Left, true, PlayerPresentationViewModel.DefaultMoveDuration);
        player.Synchronize(TimeSpan.Zero, false);
        Assert.AreEqual(144d, player.Left);
        model.Motion.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        player.Synchronize(PlayerPresentationViewModel.DefaultMoveDuration / 2, false);
        Assert.AreEqual(126d, player.Left);
        Assert.AreEqual(36d, player.Top);
        model.Motion.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        player.Synchronize(PlayerPresentationViewModel.DefaultMoveDuration / 2, false);
        Assert.AreEqual(108d, player.Left);
    }

    [TestMethod]
    public void Metadata_WhenValuesAreInvalid_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new SpriteRectangle(0, 0, 0, 10));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new SpriteFrameDefinition(new SpriteRectangle(0, 0, 10, 10), TimeSpan.Zero));
        Assert.ThrowsException<ArgumentException>(
            () => new SpriteAnimationClip("empty", [], isLooping: true));

        SpriteAnimationClip duplicate = CreateClip("duplicate", isLooping: true, 100, 100);
        Assert.ThrowsException<ArgumentException>(
            () => new SpriteSheetDefinition(
                new Uri("sprite.png", UriKind.Relative),
                [duplicate, duplicate]));
    }

    [TestMethod]
    public void Advance_WhenElapsedCrossesSeveralFrames_CatchesUpAndLoops()
    {
        SpriteAnimationClip clip = CreateClip("walk", isLooping: true, 100, 100, 100);
        SpriteAnimator animator = CreateAnimator(clip);

        animator.Advance(TimeSpan.FromMilliseconds(250));

        Assert.AreEqual(2, animator.CurrentFrameIndex);
        Assert.IsFalse(animator.IsComplete);

        animator.Advance(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(0, animator.CurrentFrameIndex);
    }

    [TestMethod]
    public void Advance_WhenOneShotEnds_HoldsLastFrameAndSignalsOnce()
    {
        SpriteAnimationClip clip = CreateClip("blocked", isLooping: false, 50, 50);
        SpriteAnimator animator = CreateAnimator(clip);
        int completionCount = 0;
        animator.Completed += (_, _) => completionCount++;

        animator.Advance(TimeSpan.FromMilliseconds(500));
        animator.Advance(TimeSpan.FromMilliseconds(500));

        Assert.AreEqual(1, animator.CurrentFrameIndex);
        Assert.IsTrue(animator.IsComplete);
        Assert.AreEqual(1, completionCount);
    }

    [TestMethod]
    public void PlayerPresentation_AcceptedMove_InterpolatesAndSnapsExactly()
    {
        var model = new Player(new(1, 1));
        PlayerPresentationViewModel player = new(model, 36);
        model.Motion.Begin(new(1, 2), Direction.Right, false, PlayerPresentationViewModel.DefaultMoveDuration);
        model.Motion.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        player.Synchronize(PlayerPresentationViewModel.DefaultMoveDuration / 2, false);

        Assert.AreEqual(new GridPosition(1, 1), model.Position);
        Assert.AreEqual(54d, player.Left);
        Assert.AreEqual(36d, player.Top);
        Assert.AreEqual(Direction.Right, player.FacingDirection);
        Assert.AreEqual(PlayerAnimationState.Walking, player.AnimationState);

        model.Motion.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);
        player.Synchronize(PlayerPresentationViewModel.DefaultMoveDuration / 2, false);

        Assert.AreEqual(new GridPosition(1, 2), model.Position);
        Assert.AreEqual(72d, player.Left);
        Assert.AreEqual(36d, player.Top);
    }

    [TestMethod]
    public void Engine_QueueDirection_ReplacesEarlierInput()
    {
        GameSession session = new(new BoardSize(5, 5), new GridPosition(1, 1));
        session.RequestDirection(Direction.Right);
        session.RequestDirection(Direction.Down);
        session.RequestDirection(Direction.Left);
        session.Advance(PlayerPresentationViewModel.DefaultMoveDuration);
        Assert.AreEqual(new GridPosition(1, 2), session.Player.Position);
        Assert.AreEqual(Direction.Left, session.Player.FacingDirection);
        Assert.AreEqual(new GridPosition(1, 1), session.Player.Motion.Destination);
    }

    [TestMethod]
    public void PlayerPresentation_BlockedReaction_ReturnsToDirectionalIdle()
    {
        var model = new Player(new(1, 1));
        PlayerPresentationViewModel player = new(model, 36);
        model.Motion.Begin(new(0, 1), Direction.Up, false, TimeSpan.FromMilliseconds(350));
        model.Motion.Advance(TimeSpan.FromMilliseconds(350));
        model.BlockedRemaining = TimeSpan.FromMilliseconds(210);
        player.Synchronize(TimeSpan.Zero, false);
        Assert.AreEqual(PlayerAnimationState.Blocked, player.AnimationState);
        model.BlockedRemaining = TimeSpan.Zero;
        player.Synchronize(TimeSpan.FromMilliseconds(210), false);
        Assert.AreEqual(PlayerAnimationState.Idle, player.AnimationState);
        Assert.AreEqual(Direction.Up, player.FacingDirection);
        Assert.AreEqual(36d, player.Left);
        Assert.AreEqual(0d, player.Top);
    }

    [TestMethod]
    public void FrameProvider_LoadsMeasuredRatManFrameWithinTextureBounds()
    {
        SpriteSheetDefinition sheet = RatManSpriteSheet.Create();
        SpriteFrameProvider provider = new(sheet);
        SpriteFrameDefinition frame = sheet.GetClip("idle-down").Frames[0];

        BitmapSource image = (BitmapSource)provider.GetFrame(frame);

        Assert.AreEqual(300, image.PixelWidth);
        Assert.AreEqual(300, image.PixelHeight);
    }

    private static SpriteAnimator CreateRatManAnimator()
    {
        SpriteSheetDefinition sheet = RatManSpriteSheet.Create();
        return new SpriteAnimator(sheet, new FakeFrameProvider(), "idle-down");
    }

    private static SpriteAnimator CreateAnimator(SpriteAnimationClip clip)
    {
        SpriteSheetDefinition sheet = new(
            new Uri("sprite.png", UriKind.Relative),
            [clip]);
        return new SpriteAnimator(sheet, new FakeFrameProvider(), clip.Name);
    }

    private static SpriteAnimationClip CreateClip(
        string name,
        bool isLooping,
        params int[] durations) =>
        new(
            name,
            durations.Select((duration, index) =>
                new SpriteFrameDefinition(
                    new SpriteRectangle(index * 10, 0, 10, 10),
                    TimeSpan.FromMilliseconds(duration))),
            isLooping);

    private sealed class FakeFrameProvider : ISpriteFrameProvider
    {
        private static readonly ImageSource Frame = CreateFrame();

        public ImageSource GetFrame(SpriteFrameDefinition frame) => Frame;

        private static ImageSource CreateFrame()
        {
            DrawingImage image = new();
            image.Freeze();
            return image;
        }
    }
}
