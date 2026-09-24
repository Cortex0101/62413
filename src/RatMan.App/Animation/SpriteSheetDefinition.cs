namespace RatMan.App.Animation;

public sealed class SpriteSheetDefinition
{
    private readonly IReadOnlyDictionary<string, SpriteAnimationClip> clips;

    public SpriteSheetDefinition(
        Uri resourceUri,
        IEnumerable<SpriteAnimationClip> clips)
    {
        ArgumentNullException.ThrowIfNull(resourceUri);
        ArgumentNullException.ThrowIfNull(clips);

        SpriteAnimationClip[] materializedClips = clips.ToArray();
        if (materializedClips.Length == 0)
        {
            throw new ArgumentException("A sprite sheet must define at least one animation clip.", nameof(clips));
        }

        if (materializedClips.Any(clip => clip is null))
        {
            throw new ArgumentException("A sprite sheet cannot contain null clips.", nameof(clips));
        }

        Dictionary<string, SpriteAnimationClip> clipsByName = new(StringComparer.Ordinal);
        foreach (SpriteAnimationClip clip in materializedClips)
        {
            if (!clipsByName.TryAdd(clip.Name, clip))
            {
                throw new ArgumentException($"The sprite sheet contains duplicate clip name '{clip.Name}'.", nameof(clips));
            }
        }

        ResourceUri = resourceUri;
        this.clips = clipsByName;
    }

    public Uri ResourceUri { get; }

    public IReadOnlyDictionary<string, SpriteAnimationClip> Clips => clips;

    public SpriteAnimationClip GetClip(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return clips.TryGetValue(name, out SpriteAnimationClip? clip)
            ? clip
            : throw new KeyNotFoundException($"Sprite animation clip '{name}' was not found.");
    }
}
