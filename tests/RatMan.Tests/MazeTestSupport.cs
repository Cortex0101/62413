using System.IO;
using RatMan.App.Persistence;
using RatMan.App.Services;
using RatMan.Game;

namespace RatMan.Tests;

internal static class TestMazes
{
    public static MazeDefinition Sample => new("Test board", new(11, 10),
        [new(new(0, 4), new(0, 5)), new(new(1, 1), new(2, 1)), new(new(7, 4), new(8, 4))],
        new(8, 4), new(5, 5));
}

internal sealed class MemoryMazeRepository(params MazeDefinition[] mazes) : IMazeRepository
{
    public Dictionary<string, MazeDefinition> Files { get; } = mazes.ToDictionary(maze => maze.Name + ".maze");
    public bool FailSave { get; set; }
    public bool FailLoad { get; set; }
    public bool FailList { get; set; }
    public IReadOnlyList<MazeEntry> List()
    {
        if (FailList) throw new IOException("Library unavailable");
        return Files.Select(pair => new MazeEntry(pair.Key, pair.Value.Name, pair.Value.Size)).ToArray();
    }
    public MazeDefinition Load(string path) => FailLoad ? throw new IOException("Read failed") : Files[path];
    public string GetNewPath(string name) => name + ".maze";
    public bool Exists(string path) => Files.ContainsKey(path);
    public void Save(string path, MazeDefinition maze)
    {
        if (FailSave) throw new IOException("Write failed");
        Files[path] = maze;
    }
}

internal sealed class TestEditorDialogs : IEditorDialogs
{
    public bool Crop { get; set; } = true;
    public bool Overwrite { get; set; } = true;
    public UnsavedChoice Unsaved { get; set; } = UnsavedChoice.Cancel;
    public int CropPrompts { get; private set; }
    public int OverwritePrompts { get; private set; }
    public Action? OnCrop { get; set; }
    public bool ConfirmCrop() { CropPrompts++; OnCrop?.Invoke(); return Crop; }
    public bool ConfirmOverwrite(string path) { OverwritePrompts++; return Overwrite; }
    public UnsavedChoice ConfirmUnsaved() => Unsaved;
}
