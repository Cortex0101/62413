using RatMan.App.Mvvm;

namespace RatMan.App.ViewModels;

public sealed class BoardCellViewModel(double left, double top) : ObservableObject
{
    private bool isBlocked;
    public double Left { get; } = left;
    public double Top { get; } = top;
    public bool IsBlocked { get => isBlocked; set => SetProperty(ref isBlocked, value); }
}
