using System.Collections.ObjectModel;
using System.Windows.Media;
using RatMan.App.Animation;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class FoodItemViewModel(GridPosition position, FoodKind kind, int variant, SpriteAnimator animator)
{
    public GridPosition Position { get; } = position;
    public FoodKind Kind { get; } = kind;
    public int Variant { get; } = variant;
    public double Left => Position.Column * BoardProjection.CellSize;
    public double Top => Position.Row * BoardProjection.CellSize;
    public ImageSource NormalImage => CheeseSpriteSheet.NormalImage(Variant);
    public SpriteAnimator Animator { get; } = animator;
}

// One animator per board; only large cheese bindings listen to its frame changes.
public sealed class FoodPresentationViewModel
{
    private readonly Dictionary<GridPosition, int> variants = [];
    private readonly ObservableCollection<FoodItemViewModel> items = [];
    public FoodPresentationViewModel() => Items = new(items);
    public ReadOnlyObservableCollection<FoodItemViewModel> Items { get; }
    public SpriteAnimator Animator { get; } = CheeseSpriteSheet.CreateSteroidAnimator();

    public int VariantAt(GridPosition position)
    {
        if (!variants.TryGetValue(position, out int variant))
            variants.Add(position, variant = Random.Shared.Next(4));
        return variant;
    }

    public void Refresh(IReadOnlyDictionary<GridPosition, FoodKind> food)
    {
        items.Clear();
        foreach (var (position, kind) in food)
            items.Add(new(position, kind, VariantAt(position), Animator));
    }

    public void Remove(GridPosition position)
    {
        var item = items.FirstOrDefault(item => item.Position == position);
        if (item is not null) items.Remove(item);
    }

    public void Reset()
    {
        variants.Clear();
        items.Clear();
        Animator.Play("cheese", restart: true);
    }

    public void Advance(TimeSpan elapsed) => Animator.Advance(elapsed);
}
