using System.Windows.Media.Imaging;
using RatMan.App.Animation;
using RatMan.App.ViewModels;
using RatMan.Game;

namespace RatMan.Tests;

[TestClass]
public sealed class CheeseSpriteTests
{
    [DataTestMethod]
    [DataRow(FoodKind.Normal)]
    [DataRow(FoodKind.Steroid)]
    public void AllFourFrames_LoadWithinTextureBounds(FoodKind kind)
    {
        var sheet = CheeseSpriteSheet.Create(kind);
        var provider = new SpriteFrameProvider(sheet);
        var frames = sheet.GetClip("cheese").Frames;
        Assert.AreEqual(4, frames.Count);
        foreach (var frame in frames)
        {
            var image = (BitmapSource)provider.GetFrame(frame);
            Assert.AreEqual(543, image.PixelWidth);
            Assert.AreEqual(724, image.PixelHeight);
            Assert.IsTrue(image.IsFrozen);
        }
    }

    [TestMethod]
    public void VariantsStayStableAcrossRefresh_AndSteroidItemsShareAnimator()
    {
        var food = new FoodPresentationViewModel();
        Dictionary<GridPosition, FoodKind> layout = new()
        {
            [new(0, 0)] = FoodKind.Normal, [new(0, 1)] = FoodKind.Steroid, [new(0, 2)] = FoodKind.Steroid
        };
        food.Refresh(layout);
        int variant = food.Items[0].Variant;
        Assert.IsTrue(variant is >= 0 and < 4);
        var image = food.Items[0].NormalImage;
        food.Refresh(layout);
        Assert.AreEqual(variant, food.Items[0].Variant);
        Assert.AreSame(image, food.Items[0].NormalImage);
        Assert.AreSame(food.Items[1].Animator, food.Items[2].Animator);
    }

    [TestMethod]
    public void SteroidAnimation_AdvancesWhilePlayerIdle_AndLoopsAtSixHundredMilliseconds()
    {
        var game = new GameViewModel(() => { }, new GameSession(new BoardSize(1, 2), new(0, 0)));
        game.Advance(TimeSpan.FromMilliseconds(150));
        Assert.AreEqual(1, game.Food.Animator.CurrentFrameIndex);
        game.Advance(TimeSpan.FromMilliseconds(300));
        Assert.AreEqual(3, game.Food.Animator.CurrentFrameIndex);
        game.Advance(TimeSpan.FromMilliseconds(150));
        Assert.AreEqual(0, game.Food.Animator.CurrentFrameIndex);
        Assert.AreEqual(0, game.Score);
        Assert.AreEqual(PlayerAnimationState.Idle, game.Player.AnimationState);
    }
}
