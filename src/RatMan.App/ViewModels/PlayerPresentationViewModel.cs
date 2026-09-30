using System.ComponentModel;
using System.Windows.Media;
using RatMan.App.Animation;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class PlayerPresentationViewModel : ObservableObject
{
    public static readonly TimeSpan DefaultMoveDuration = TimeSpan.FromMilliseconds(350);

    private readonly SpriteAnimator animator;
    private readonly double cellSize;
    private readonly TimeSpan moveDuration;
    private Direction? queuedDirection;
    private double moveElapsedMilliseconds;
    private double originLeft;
    private double originTop;
    private double targetLeft;
    private double targetTop;
    private double left;
    private double top;
    private Direction facingDirection = Direction.Down;
    private PlayerAnimationState animationState = PlayerAnimationState.Idle;

    public PlayerPresentationViewModel(
        GridPosition initialPosition,
        double cellSize,
        SpriteAnimator animator,
        TimeSpan? moveDuration = null)
    {
        if (cellSize <= 0 || double.IsNaN(cellSize) || double.IsInfinity(cellSize))
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Cell size must be finite and positive.");
        }

        this.animator = animator ?? throw new ArgumentNullException(nameof(animator));
        this.cellSize = cellSize;
        this.moveDuration = moveDuration ?? DefaultMoveDuration;
        if (this.moveDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(moveDuration), moveDuration, "Movement duration must be positive.");
        }

        left = initialPosition.Column * cellSize;
        top = initialPosition.Row * cellSize;
        targetLeft = left;
        targetTop = top;
        animator.PropertyChanged += OnAnimatorPropertyChanged;
    }

    public double Left
    {
        get => left;
        private set => SetProperty(ref left, value);
    }

    public double Top
    {
        get => top;
        private set => SetProperty(ref top, value);
    }

    public Direction FacingDirection
    {
        get => facingDirection;
        private set => SetProperty(ref facingDirection, value);
    }

    public PlayerAnimationState AnimationState
    {
        get => animationState;
        private set
        {
            if (SetProperty(ref animationState, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public bool IsBusy => AnimationState != PlayerAnimationState.Idle;

    public ImageSource CurrentImage => animator.CurrentImage;

    public void BeginMove(GridPosition targetPosition, Direction direction)
    {
        if (IsBusy)
        {
            throw new InvalidOperationException("Cannot start a player move while another presentation action is active.");
        }

        FacingDirection = direction;
        originLeft = Left;
        originTop = Top;
        targetLeft = targetPosition.Column * cellSize;
        targetTop = targetPosition.Row * cellSize;
        moveElapsedMilliseconds = 0;
        SetAnimation(PlayerAnimationState.Walking, restart: true);
    }

    public void ShowBlocked(Direction direction)
    {
        if (AnimationState == PlayerAnimationState.Walking)
        {
            throw new InvalidOperationException("Cannot show a blocked reaction during movement.");
        }

        FacingDirection = direction;
        SetAnimation(PlayerAnimationState.Blocked, restart: true);
    }

    public void SetIdle()
    {
        SetAnimation(PlayerAnimationState.Idle);
    }

    public void QueueDirection(Direction direction)
    {
        queuedDirection = direction;
    }

    public bool TryTakeQueuedDirection(out Direction direction)
    {
        if (queuedDirection is Direction queued)
        {
            direction = queued;
            queuedDirection = null;
            return true;
        }

        direction = default;
        return false;
    }

    public bool Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Elapsed presentation time cannot be negative.");
        }

        animator.Advance(elapsed);

        if (AnimationState == PlayerAnimationState.Blocked && animator.IsComplete)
        {
            SetIdle();
            return true;
        }

        if (AnimationState != PlayerAnimationState.Walking || elapsed == TimeSpan.Zero)
        {
            return false;
        }

        moveElapsedMilliseconds = Math.Min(
            moveElapsedMilliseconds + elapsed.TotalMilliseconds,
            moveDuration.TotalMilliseconds);
        double progress = moveElapsedMilliseconds / moveDuration.TotalMilliseconds;
        Left = Lerp(originLeft, targetLeft, progress);
        Top = Lerp(originTop, targetTop, progress);

        return progress >= 1;
    }

    private static double Lerp(double start, double end, double progress) =>
        start + ((end - start) * progress);

    private void SetAnimation(PlayerAnimationState state, bool restart = false)
    {
        AnimationState = state;
        animator.Play(RatManSpriteSheet.GetClipName(state, FacingDirection), restart);
    }

    private void OnAnimatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SpriteAnimator.CurrentImage))
        {
            OnPropertyChanged(nameof(CurrentImage));
        }
    }
}
