using System.IO;
using System.Windows.Input;
using RatMan.App.Mvvm;
using RatMan.App.Persistence;

namespace RatMan.App.ViewModels;

public sealed class SettingsViewModel : ScreenViewModel
{
    private readonly ISettingsRepository repository;
    private string status = "";
    private bool isSaving;

    public SettingsViewModel(ISettingsRepository repository, Action returnToMenu) : base("Settings")
    {
        this.repository = repository;
        Rows = MovementBinding.Defaults.Select(binding => new MovementBindingRow(binding)).ToArray();
        ReturnToMenuCommand = new RelayCommand(_ => returnToMenu(), _ => !IsSaving);
        Loading = LoadAsync();
    }

    public IReadOnlyList<MovementBindingRow> Rows { get; }
    public RelayCommand ReturnToMenuCommand { get; }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    internal bool IsSaving
    {
        get => isSaving;
        private set
        {
            if (SetProperty(ref isSaving, value)) ReturnToMenuCommand.RaiseCanExecuteChanged();
        }
    }

    internal Task Loading { get; }

    internal async Task RebindAsync(Key previousKey, Key key)
    {
        if (IsSaving || key == previousKey || !MovementBinding.CanBind(key)) return;

        MovementBinding[] updated = Rows.Select(item => item.Binding with
        {
            FirstKey = ReplaceKey(item.Binding.FirstKey),
            SecondKey = ReplaceKey(item.Binding.SecondKey)
        }).ToArray();

        IsSaving = true;
        try
        {
            await repository.SaveAsync(updated);
            Apply(updated);
            Status = "";
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            Status = "Could not save the key binding. Choose a key to try again.";
        }
        finally { IsSaving = false; }

        Key ReplaceKey(Key existing) => existing == previousKey ? key : existing == key ? previousKey : existing;
    }

    private async Task LoadAsync()
    {
        try { Apply(await repository.LoadAsync()); }
        catch (Exception exception) when (IsStorageError(exception) || exception is InvalidDataException)
        {
            Status = "Could not load saved key bindings. Using the default keys.";
        }
    }

    private void Apply(IReadOnlyList<MovementBinding> bindings)
    {
        for (int i = 0; i < Rows.Count; i++) Rows[i].Binding = bindings[i];
    }

    private static bool IsStorageError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or System.Security.SecurityException;
}

public sealed class MovementBindingRow(MovementBinding binding) : ObservableObject
{
    private MovementBinding currentBinding = binding;

    public MovementBinding Binding { get => currentBinding; internal set => SetProperty(ref currentBinding, value); }
}
