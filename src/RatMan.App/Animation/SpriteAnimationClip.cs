namespace RatMan.App.Animation;

public sealed class SpriteAnimationClip
{
    public SpriteAnimationClip(
        string name,
        IEnumerable<SpriteFrameDefinition> frames,
        bool isLooping)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(frames);

        SpriteFrameDefinition[] materializedFrames = frames.ToArray();
        if (materializedFrames.Length == 0)
        {
            throw new ArgumentException("A sprite animation clip must contain at least one frame.", nameof(frames));
        }

        if (materializedFrames.Any(frame => frame is null))
        {
            throw new ArgumentException("A sprite animation clip cannot contain null frames.", nameof(frames));
        }

        Name = name;
        Frames = materializedFrames;
        IsLooping = isLooping;
    }

    public string Name { get; }

    public IReadOnlyList<SpriteFrameDefinition> Frames { get; }

    public bool IsLooping { get; }
}
