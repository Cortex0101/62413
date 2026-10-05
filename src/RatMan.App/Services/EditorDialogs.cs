using System.Windows;

namespace RatMan.App.Services;

public enum UnsavedChoice { Save, Discard, Cancel }

public interface IEditorDialogs
{
    bool ConfirmCrop();
    bool ConfirmOverwrite(string path);
    UnsavedChoice ConfirmUnsaved();
}

public sealed class EditorDialogs : IEditorDialogs
{
    public bool ConfirmCrop() => MessageBox.Show(
        "Reducing the board will remove walls, spawn points, or food overrides outside its new size. Continue?",
        "Resize board", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

    public bool ConfirmOverwrite(string path) => MessageBox.Show(
        $"Replace the existing board file {System.IO.Path.GetFileName(path)}?",
        "Save board", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

    public UnsavedChoice ConfirmUnsaved() => MessageBox.Show(
        "Save changes before leaving this board?\nYes: Save · No: Discard · Cancel: Keep editing",
        "Unsaved board", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) switch
    {
        MessageBoxResult.Yes => UnsavedChoice.Save,
        MessageBoxResult.No => UnsavedChoice.Discard,
        _ => UnsavedChoice.Cancel
    };
}
