using System.Windows.Media;
using RatMan.App.Mvvm;

namespace RatMan.App.Animation;

public sealed class SpriteAnimator : ObservableObject
{
    private readonly SpriteSheetDefinition sheet;
    private readonly ISpriteFrameProvider frameProvider;
    private SpriteAnimationClip currentClip;
    private TimeSpan frameElapsed;
    private int currentFrameIndex;
    private bool isComplete;

    public SpriteAnimator(
        SpriteSheetDefinition sheet,
        ISpriteFrameProvider frameProvider,
        string initialClipName)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(frameProvider);

        this.sheet = sheet;
        this.frameProvider = frameProvider;
        currentClip = sheet.GetClip(initialClipName);
    }

    public event EventHandler? Completed;

    public string CurrentClipName => currentClip.Name;

    public int CurrentFrameIndex => currentFrameIndex;

    public SpriteFrameDefinition CurrentFrame => currentClip.Frames[currentFrameIndex];

    public ImageSource CurrentImage => frameProvider.GetFrame(CurrentFrame);

    public bool IsComplete
    {
        get => isComplete;
        private set
        {
            if (SetProperty(ref isComplete, value) && value)
            {
                Completed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Play(string clipName, bool restart = false)
    {
        SpriteAnimationClip clip = sheet.GetClip(clipName);
        if (!restart && ReferenceEquals(clip, currentClip) && !IsComplete)
        {
            return;
        }

        bool clipChanged = !ReferenceEquals(clip, currentClip);
        currentClip = clip;
        currentFrameIndex = 0;
        frameElapsed = TimeSpan.Zero;
        IsComplete = false;

        if (clipChanged)
        {
            OnPropertyChanged(nameof(CurrentClipName));
        }

        OnFrameChanged();
    }

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Elapsed animation time cannot be negative.");
        }

        if (elapsed == TimeSpan.Zero || IsComplete)
        {
            return;
        }

        frameElapsed += elapsed;
        bool frameChanged = false;

        while (frameElapsed >= CurrentFrame.Duration && !IsComplete)
        {
            frameElapsed -= CurrentFrame.Duration;

            if (currentFrameIndex + 1 < currentClip.Frames.Count)
            {
                currentFrameIndex++;
                frameChanged = true;
            }
            else if (currentClip.IsLooping)
            {
                currentFrameIndex = 0;
                frameChanged = true;
            }
            else
            {
                frameElapsed = TimeSpan.Zero;
                IsComplete = true;
            }
        }

        if (frameChanged)
        {
            OnFrameChanged();
        }
    }

    private void OnFrameChanged()
    {
        OnPropertyChanged(nameof(CurrentFrameIndex));
        OnPropertyChanged(nameof(CurrentFrame));
        OnPropertyChanged(nameof(CurrentImage));
    }
}
