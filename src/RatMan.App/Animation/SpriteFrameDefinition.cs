namespace RatMan.App.Animation;

public sealed record SpriteFrameDefinition
{
    public SpriteFrameDefinition(SpriteRectangle source, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A sprite frame duration must be positive.");
        }

        Source = source;
        Duration = duration;
    }

    public SpriteRectangle Source { get; }

    public TimeSpan Duration { get; }
}
