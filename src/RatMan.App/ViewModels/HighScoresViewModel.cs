using RatMan.App.Mvvm;
using RatMan.App.Persistence;

namespace RatMan.App.ViewModels;

public sealed record HighScoreRow(int Rank, string Name, int Score);
public sealed record HighScoreLayoutOption(HighScoreLayout Layout, IReadOnlyList<HighScoreEntry> Entries)
{
    public DateTimeOffset? LastPlayed => Entries.Count == 0 ? null : Entries.Max(e => e.CompletedAt);
    public string Label => $"{Layout.Name} · {Layout.Width} × {Layout.Height} · " +
        (LastPlayed is { } date ? $"Last played {date.ToLocalTime():g}" : "No scores yet");
}

public sealed class HighScoresViewModel : ScreenViewModel
{
    private readonly IHighScoreRepository scores;
    private readonly IMazeRepository mazes;
    private readonly HighScoreLayout? preferredLayout;
    private IReadOnlyList<HighScoreLayoutOption> layouts = [];
    private HighScoreLayoutOption? selectedLayout;
    private string status = "";
    private bool loading;

    public HighScoresViewModel(IHighScoreRepository scores, IMazeRepository mazes, Action back,
        HighScoreLayout? preferredLayout = null) : base("High Scores")
    {
        this.scores = scores;
        this.mazes = mazes;
        this.preferredLayout = preferredLayout;
        ReturnToMenuCommand = new RelayCommand(_ => back());
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        Loading = RefreshCommand.ExecuteAsync();
    }

    public Task Loading { get; }
    public RelayCommand ReturnToMenuCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public IReadOnlyList<HighScoreLayoutOption> Layouts { get => layouts; private set => SetProperty(ref layouts, value); }
    public HighScoreLayoutOption? SelectedLayout
    {
        get => selectedLayout;
        set
        {
            if (!SetProperty(ref selectedLayout, value)) return;
            OnPropertyChanged(nameof(Rows));
            OnPropertyChanged(nameof(EmptyMessage));
        }
    }
    public IReadOnlyList<HighScoreRow> Rows => SelectedLayout is null ? [] : HighScoreRules.Top(SelectedLayout.Entries)
        .Select((e, index) => new HighScoreRow(index + 1, e.Name, e.Score)).ToArray();
    public string Status { get => status; private set { SetProperty(ref status, value); OnPropertyChanged(nameof(EmptyMessage)); } }
    public bool IsLoading { get => loading; private set { SetProperty(ref loading, value); OnPropertyChanged(nameof(EmptyMessage)); } }
    public string EmptyMessage => IsLoading || Status.Length > 0 || Rows.Count > 0 ? "" : SelectedLayout is null
        ? "No maps or high scores yet. Create a map in Board Builder to get started."
        : "No scores for this layout yet. Finish a game to set the first high score!";

    private async Task LoadAsync()
    {
        IsLoading = true;
        Status = "";
        try
        {
            var boards = (await scores.LoadAsync()).ToDictionary(b => b.Layout.Id,
                b => new HighScoreLayoutOption(b.Layout, b.Entries));
            List<string> warnings = [];
            try
            {
                foreach (var map in mazes.List())
                {
                    if (!map.IsValid) { warnings.Add($"Could not load map: {map.Name}."); continue; }
                    try
                    {
                        var layout = HighScoreLayout.FromMaze(mazes.Load(map.FilePath));
                        boards[layout.Id] = new(layout, boards.GetValueOrDefault(layout.Id)?.Entries ?? []);
                    }
                    catch (Exception exception) when (FileMazeRepository.IsFileError(exception))
                    { warnings.Add($"Could not load map {map.Name}: {exception.Message}"); }
                }
            }
            catch (Exception exception) when (FileMazeRepository.IsFileError(exception))
            { warnings.Add("Could not read map library: " + exception.Message); }
            if (preferredLayout is not null && !boards.ContainsKey(preferredLayout.Id))
                boards.Add(preferredLayout.Id, new(preferredLayout, []));
            string? selection = SelectedLayout?.Layout.Id ?? preferredLayout?.Id;
            Layouts = boards.Values.OrderBy(b => b.Layout.Name, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(b => b.LastPlayed).ToArray();
            SelectedLayout = Layouts.FirstOrDefault(b => b.Layout.Id == selection)
                ?? Layouts.OrderByDescending(b => b.LastPlayed).FirstOrDefault();
            Status = string.Join("\n", warnings);
        }
        catch (Exception exception) when (FileHighScoreRepository.IsStorageError(exception))
        { Status = "Could not load high scores. Your saved file has been left unchanged. " + exception.Message + " Use Refresh to retry."; }
        finally { IsLoading = false; }
    }
}
