using RatMan.App.Mvvm;
using RatMan.App.Persistence;

namespace RatMan.App.ViewModels;

public sealed class HighScoreEntryViewModel : ObservableObject
{
    private readonly IHighScoreRepository repository;
    private readonly HighScoreLayout layout;
    private readonly HighScoreEntry entry;
    private string name = "", status = "";
    private bool saving, closed, noLongerQualifies;

    public HighScoreEntryViewModel(IHighScoreRepository repository, HighScoreLayout layout, HighScoreEntry entry, int rank)
    {
        this.repository = repository;
        this.layout = layout;
        this.entry = entry;
        Rank = rank;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !closed && !noLongerQualifies);
        SkipCommand = new RelayCommand(_ => { if (CanClose) Close(false); }, _ => CanClose);
    }

    public string MapName => layout.Name;
    public int Score => entry.Score;
    public int Rank { get; }
    public string ResultText => $"{Score} points · Rank #{Rank}";
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value.Length <= HighScoreRules.MaximumNameLength ? value : value[..HighScoreRules.MaximumNameLength]);
    }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    public bool IsSaving
    {
        get => saving;
        private set
        {
            SetProperty(ref saving, value);
            OnPropertyChanged(nameof(CanClose));
            OnPropertyChanged(nameof(CanEdit));
            SkipCommand.RaiseCanExecuteChanged();
        }
    }
    public bool CanClose => !IsSaving && !closed;
    public bool CanEdit => !IsSaving && !closed && !noLongerQualifies;
    public bool Saved { get; private set; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand SkipCommand { get; }
    public event Action? CloseRequested;

    public void Dismiss()
    {
        if (CanClose) closed = true;
    }

    private async Task SaveAsync()
    {
        IsSaving = true;
        Status = "Saving…";
        try
        {
            var result = await repository.SubmitAsync(layout, entry with { Name = HighScoreRules.NormalizeName(Name) });
            if (result.Saved) Close(true);
            else
            {
                noLongerQualifies = true;
                Status = "The leaderboard changed and this score no longer reaches the top 10. Choose Skip to continue.";
            }
        }
        catch (Exception exception) when (FileHighScoreRepository.IsStorageError(exception))
        { Status = "Could not save your score. Try Save Score again or choose Skip. " + exception.Message; }
        finally { IsSaving = false; }
    }

    private void Close(bool saved)
    {
        closed = true;
        Saved = saved;
        CloseRequested?.Invoke();
    }
}
