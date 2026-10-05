using System.Windows.Media;
using RatMan.App.Animation;
using RatMan.App.Mvvm;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class CatPresentationViewModel : ObservableObject
{
    private readonly Cat cat;
    private readonly SpriteAnimator normal, scared;
    private double left, top, opacity;
    private bool visible;
    private string status = "";
    private ImageSource currentImage;

    public CatPresentationViewModel(Cat cat)
    {
        this.cat = cat;
        normal = CatSpriteSheet.CreateAnimator(cat.Kind, false);
        scared = CatSpriteSheet.CreateAnimator(cat.Kind, true);
        currentImage = normal.CurrentImage;
        Portrait = normal.CurrentImage;
        Synchronize(TimeSpan.Zero, 0);
    }

    public string Name => cat.Kind.ToString();
    public ImageSource Portrait { get; }
    public double Left { get => left; private set => SetProperty(ref left, value); }
    public double Top { get => top; private set => SetProperty(ref top, value); }
    public double Opacity { get => opacity; private set => SetProperty(ref opacity, value); }
    public bool IsVisible { get => visible; private set => SetProperty(ref visible, value); }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    public ImageSource CurrentImage { get => currentImage; private set => SetProperty(ref currentImage, value); }

    public void ResetAnimation()
    {
        normal.Play("idle-down", restart: true);
        scared.Play("idle-down", restart: true);
    }

    public void Synchronize(TimeSpan elapsed, int collected)
    {
        Left = cat.Motion.Column * BoardProjection.CellSize;
        Top = cat.Motion.Row * BoardProjection.CellSize;
        IsVisible = cat.IsVisible;
        Opacity = cat.State == CatState.ReturningHome ? .45 : 1;
        Status = cat.State switch
        {
            CatState.Waiting => $"In home · {Math.Max(0, cat.ReleaseCount - collected)} cheese to go",
            CatState.Chase => "Chasing",
            CatState.Scatter => "Scared · edible",
            CatState.ReturningHome => "Returning home",
            _ => "Recovering in home"
        };
        SpriteAnimator animator = cat.State is CatState.Scatter or CatState.ReturningHome ? scared : normal;
        animator.Play(RatManSpriteSheet.GetClipName(cat.Motion.IsMoving ? PlayerAnimationState.Walking : PlayerAnimationState.Idle, cat.Motion.FacingDirection));
        animator.Advance(elapsed);
        CurrentImage = animator.CurrentImage;
    }
}
