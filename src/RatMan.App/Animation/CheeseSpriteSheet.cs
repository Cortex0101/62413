using System.Windows.Media;
using RatMan.Game;

namespace RatMan.App.Animation;

public static class CheeseSpriteSheet
{
    private static readonly Lazy<SpriteSheetDefinition> Normal = new(() => Create(FoodKind.Normal));
    private static readonly Lazy<SpriteSheetDefinition> Steroid = new(() => Create(FoodKind.Steroid));
    private static readonly Lazy<SpriteFrameProvider> NormalFrames = new(() => new(Normal.Value));
    private static readonly Lazy<SpriteFrameProvider> SteroidFrames = new(() => new(Steroid.Value));

    public static SpriteSheetDefinition Create(FoodKind kind) => new(
        new Uri($"/RatMan;component/Assets/Images/{(kind == FoodKind.Normal ? "tiny" : "huge")}-cheese.png", UriKind.Relative),
        [new SpriteAnimationClip("cheese", Enumerable.Range(0, 4).Select(index =>
            new SpriteFrameDefinition(new(index * 543, 0, 543, 724), TimeSpan.FromMilliseconds(150))), true)]);

    public static ImageSource NormalImage(int variant) => NormalFrames.Value.GetFrame(Normal.Value.GetClip("cheese").Frames[variant]);

    public static SpriteAnimator CreateSteroidAnimator() => new(Steroid.Value, SteroidFrames.Value, "cheese");
}
