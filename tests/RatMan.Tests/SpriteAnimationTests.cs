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
        SpriteAnimator animator = CreateRatManAnimator();
        PlayerPresentationViewModel player = new(
            new GridPosition(1, 1),
            36,
            animator);

        player.BeginMove(new GridPosition(1, 2), Direction.Right);
        bool completedHalfway = player.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);

        Assert.IsFalse(completedHalfway);
        Assert.AreEqual(54d, player.Left);
        Assert.AreEqual(36d, player.Top);
        Assert.AreEqual(Direction.Right, player.FacingDirection);
        Assert.AreEqual("walking-right", animator.CurrentClipName);

        bool completed = player.Advance(PlayerPresentationViewModel.DefaultMoveDuration / 2);

        Assert.IsTrue(completed);
        Assert.AreEqual(72d, player.Left);
        Assert.AreEqual(36d, player.Top);
    }

    [TestMethod]
    public void PlayerPresentation_QueueDirection_ReplacesEarlierInput()
    {
        PlayerPresentationViewModel player = new(
            new GridPosition(1, 1),
            36,
            CreateRatManAnimator());

        player.QueueDirection(Direction.Down);
        player.QueueDirection(Direction.Left);

        Assert.IsTrue(player.TryTakeQueuedDirection(out Direction direction));
        Assert.AreEqual(Direction.Left, direction);
        Assert.IsFalse(player.TryTakeQueuedDirection(out _));
    }

    [TestMethod]
    public void PlayerPresentation_BlockedReaction_ReturnsToDirectionalIdle()
    {
        SpriteAnimator animator = CreateRatManAnimator();
        PlayerPresentationViewModel player = new(
            new GridPosition(1, 1),
            36,
            animator);

        player.ShowBlocked(Direction.Up);
        bool completed = player.Advance(TimeSpan.FromMilliseconds(210));

        Assert.IsTrue(completed);
        Assert.AreEqual(PlayerAnimationState.Idle, player.AnimationState);
        Assert.AreEqual(Direction.Up, player.FacingDirection);
        Assert.AreEqual("idle-up", animator.CurrentClipName);
        Assert.AreEqual(36d, player.Left);
        Assert.AreEqual(36d, player.Top);
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
