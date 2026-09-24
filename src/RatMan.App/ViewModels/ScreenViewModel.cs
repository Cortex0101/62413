using RatMan.App.Mvvm;

namespace RatMan.App.ViewModels;

public abstract class ScreenViewModel : ObservableObject
{
    protected ScreenViewModel(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    public string Title { get; }
}
