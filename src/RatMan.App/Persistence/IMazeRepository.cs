using RatMan.Game;

namespace RatMan.App.Persistence;

public sealed record MazeEntry(string FilePath, string Name, BoardSize? Size, string? Error = null)
{
    public bool IsValid => Error is null;
    public string Description => Size is { } size
        ? $"{size.Columns} × {size.Rows} · {System.IO.Path.GetFileName(FilePath)}"
        : $"{System.IO.Path.GetFileName(FilePath)} · {Error}";
}

public interface IMazeRepository
{
    IReadOnlyList<MazeEntry> List();
    MazeDefinition Load(string path);
    string GetNewPath(string name);
    bool Exists(string path);
    void Save(string path, MazeDefinition maze);
}
