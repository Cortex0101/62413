using RatMan.Game;

namespace RatMan.App.Animation;

public static class RatManSpriteSheet
{
    private static readonly Lazy<SpriteSheetDefinition> SharedSheet = new(Create);
    private static readonly Lazy<SpriteFrameProvider> SharedFrameProvider =
        new(() => new SpriteFrameProvider(SharedSheet.Value));

    private static readonly TimeSpan IdleFrameDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WalkFrameDuration = TimeSpan.FromMilliseconds(95);
    private static readonly TimeSpan BlockedFrameDuration = TimeSpan.FromMilliseconds(70);

    private static readonly IReadOnlyDictionary<Direction, SpriteRectangle[]> FramesByDirection =
        new Dictionary<Direction, SpriteRectangle[]>
        {
            [Direction.Down] =
            [
                new(25, 34, 300, 300),
                new(325, 34, 300, 300),
                new(629, 34, 300, 300),
                new(932, 34, 300, 300)
            ],
            [Direction.Left] =
            [
                new(25, 324, 300, 300),
                new(325, 324, 300, 300),
                new(629, 324, 300, 300),
                new(932, 324, 300, 300)
            ],
            [Direction.Right] =
            [
                new(25, 618, 300, 300),
                new(325, 618, 300, 300),
                new(629, 618, 300, 300),
                new(932, 618, 300, 300)
            ],
            [Direction.Up] =
            [
                new(25, 899, 300, 300),
                new(325, 899, 300, 300),
                new(629, 899, 300, 300),
                new(932, 899, 300, 300)
            ]
        };

    public static SpriteSheetDefinition Create()
    {
        List<SpriteAnimationClip> clips = [];
        foreach ((Direction direction, SpriteRectangle[] frames) in FramesByDirection)
        {
            clips.Add(CreateIdleClip(direction, frames));
            clips.Add(CreateWalkClip(direction, frames));
            clips.Add(CreateBlockedClip(direction, frames));
        }

        return new SpriteSheetDefinition(
            new Uri(
                "/RatMan;component/Assets/Images/ratman-spritesheet.png",
                UriKind.Relative),
            clips);
    }

    public static SpriteAnimator CreateAnimator() =>
        new(
            SharedSheet.Value,
            SharedFrameProvider.Value,
            GetClipName(PlayerAnimationState.Idle, Direction.Down));

    public static string GetClipName(PlayerAnimationState state, Direction direction) =>
        $"{state.ToString().ToLowerInvariant()}-{direction.ToString().ToLowerInvariant()}";

    private static SpriteAnimationClip CreateIdleClip(
        Direction direction,
        IReadOnlyList<SpriteRectangle> frames) =>
        new(
            GetClipName(PlayerAnimationState.Idle, direction),
            [new SpriteFrameDefinition(frames[0], IdleFrameDuration)],
            isLooping: true);

    private static SpriteAnimationClip CreateWalkClip(
        Direction direction,
        IReadOnlyList<SpriteRectangle> frames) =>
        new(
            GetClipName(PlayerAnimationState.Walking, direction),
            [
                new SpriteFrameDefinition(frames[1], WalkFrameDuration),
                new SpriteFrameDefinition(frames[2], WalkFrameDuration),
                new SpriteFrameDefinition(frames[3], WalkFrameDuration),
                new SpriteFrameDefinition(frames[2], WalkFrameDuration)
            ],
            isLooping: true);

    private static SpriteAnimationClip CreateBlockedClip(
        Direction direction,
        IReadOnlyList<SpriteRectangle> frames) =>
        new(
            GetClipName(PlayerAnimationState.Blocked, direction),
            [
                new SpriteFrameDefinition(frames[0], BlockedFrameDuration),
                new SpriteFrameDefinition(frames[1], BlockedFrameDuration),
                new SpriteFrameDefinition(frames[0], BlockedFrameDuration)
            ],
            isLooping: false);
}

public enum PlayerAnimationState
{
    Idle,
    Walking,
    Blocked
}
