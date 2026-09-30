using System.Windows.Input;
using RatMan.App.Mvvm;
using RatMan.App.Persistence;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public sealed class MapSelectionViewModel : ScreenViewModel
{
    private string status = "";
    public MapSelectionViewModel(string title, IMazeRepository repository,
        Action<MazeDefinition, string> select, Action back, Action builder) : base(title)
    {
        try { Maps = repository.List(); }
        catch (Exception exception) when (FileMazeRepository.IsFileError(exception))
        { Maps = []; Status = "Could not read map library: " + exception.Message; }
        if (Maps.Count == 0 && Status.Length == 0) Status = "No maps yet. Create one in Board Builder.";
        else if (Maps.Any(entry => !entry.IsValid)) Status = "Some maps could not be loaded. Their errors are shown below.";
        SelectCommand = new RelayCommand(value =>
        {
            if (value is not MazeEntry { IsValid: true } entry) return;
            try { select(repository.Load(entry.FilePath), entry.FilePath); }
            catch (Exception exception) when (FileMazeRepository.IsFileError(exception))
            { Status = "Could not load board: " + exception.Message; }
        }, value => value is MazeEntry { IsValid: true });
        BackCommand = new RelayCommand(_ => back());
        BuilderCommand = new RelayCommand(_ => builder());
    }

    public IReadOnlyList<MazeEntry> Maps { get; }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    public ICommand SelectCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand BuilderCommand { get; }
}
