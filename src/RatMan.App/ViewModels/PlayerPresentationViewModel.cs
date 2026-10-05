using System.Windows.Media;
using RatMan.App.Animation;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class PlayerPresentationViewModel : ObservableObject
{
    public static readonly TimeSpan DefaultMoveDuration = new GameRules().PlayerMoveDuration;
    private readonly Player player;
    private readonly double cellSize;
    private readonly SpriteAnimator normal = RatManSpriteSheet.CreateAnimator();
    private readonly SpriteAnimator steroid = CatSpriteSheet.CreateAnimator("ratman-steroid-spritesheet");
    private double left, top;
    private Direction facing;
    private PlayerAnimationState animationState;
    private ImageSource currentImage;

    public PlayerPresentationViewModel(Player player, double cellSize)
    {
        this.player = player;
        this.cellSize = cellSize;
        currentImage = normal.CurrentImage;
        Synchronize(TimeSpan.Zero, false);
    }

    public double Left { get => left; private set => SetProperty(ref left, value); }
    public double Top { get => top; private set => SetProperty(ref top, value); }
    public Direction FacingDirection { get => facing; private set => SetProperty(ref facing, value); }
    public PlayerAnimationState AnimationState { get => animationState; private set => SetProperty(ref animationState, value); }
    public ImageSource CurrentImage { get => currentImage; private set => SetProperty(ref currentImage, value); }

    public void ResetAnimation()
    {
        normal.Play("idle-down", restart: true);
        steroid.Play("idle-down", restart: true);
    }

    public void Synchronize(TimeSpan elapsed, bool powered)
    {
        Left = player.Motion.Column * cellSize;
        Top = player.Motion.Row * cellSize;
        FacingDirection = player.FacingDirection;
        AnimationState = player.Motion.IsMoving ? PlayerAnimationState.Walking
            : player.BlockedRemaining > TimeSpan.Zero ? PlayerAnimationState.Blocked : PlayerAnimationState.Idle;
        SpriteAnimator animator = powered ? steroid : normal;
        animator.Play(RatManSpriteSheet.GetClipName(AnimationState, FacingDirection), restart: AnimationState == PlayerAnimationState.Blocked);
        animator.Advance(AnimationState == PlayerAnimationState.Blocked
            ? TimeSpan.FromMilliseconds(210) - player.BlockedRemaining : elapsed);
        CurrentImage = animator.CurrentImage;
    }
}
