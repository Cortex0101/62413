using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace RatMan.App.Persistence;

public sealed class FileHighScoreRepository : IHighScoreRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);
    private readonly string path;

    public FileHighScoreRepository(string? path = null) => this.path = Path.GetFullPath(path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RatMan", "highscores.json"));

    public async Task<IReadOnlyList<HighScoreBoard>> LoadAsync()
    {
        string json;
        try { json = await File.ReadAllTextAsync(path).ConfigureAwait(false); }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
        try
        {
            var file = JsonSerializer.Deserialize<ScoreFile>(json, JsonOptions);
            if (file is null || file.Version != 1 || file.Boards is null)
                throw new InvalidDataException("The high-score file is empty or uses an unsupported format.");
            HashSet<string> layouts = [];
            HashSet<Guid> entries = [];
            foreach (var board in file.Boards)
            {
                if (board is null || !ValidLayout(board.Layout) || !layouts.Add(board.Layout.Id) || board.Entries is null ||
                    board.Entries.Any(e => e is null || e.Id == Guid.Empty || !entries.Add(e.Id) || e.Score < 0 ||
                        string.IsNullOrWhiteSpace(e.Name) || e.Name.Length > HighScoreRules.MaximumNameLength ||
                        e.CompletedAt == default))
                    throw new InvalidDataException("The high-score file contains invalid entries.");
            }
            return file.Boards.Select(b => b with { Entries = HighScoreRules.Top(b.Entries) }).ToArray();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The high-score file contains invalid JSON.", exception);
        }
    }

    public async Task<HighScoreSubmission> SubmitAsync(HighScoreLayout layout, HighScoreEntry entry)
    {
        if (!ValidLayout(layout)) throw new ArgumentException("Invalid high-score layout.", nameof(layout));
        if (entry.Id == Guid.Empty || entry.Score < 0 || entry.CompletedAt == default)
            throw new ArgumentException("Invalid high-score entry.", nameof(entry));
        var gate = Gates.GetOrAdd(path, _ => new(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Also prevent another running app from replacing a score written during our read/modify/write.
            using var fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var boards = (await LoadAsync().ConfigureAwait(false)).ToList();
            int index = boards.FindIndex(b => b.Layout.Id == layout.Id);
            var existing = index < 0 ? Array.Empty<HighScoreEntry>() : boards[index].Entries;
            int previous = existing.ToList().FindIndex(e => e.Id == entry.Id);
            if (previous >= 0) return new(true, previous + 1);
            int? rank = HighScoreRules.QualifyingRank(existing, entry.Score);
            if (rank is null) return new(false, null);
            var board = new HighScoreBoard(layout, HighScoreRules.Top(existing.Append(entry with
            {
                Name = HighScoreRules.NormalizeName(entry.Name)
            })));
            if (index < 0) boards.Add(board);
            else boards[index] = board;

            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new ScoreFile(1, boards.ToArray()), JsonOptions))
                    .ConfigureAwait(false);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return new(true, rank);
        }
        finally { gate.Release(); }
    }

    public static bool IsStorageError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or System.Security.SecurityException;

    private static bool ValidLayout(HighScoreLayout? layout) => layout is not null &&
        layout.Id is { Length: 64 } && layout.Id.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F') &&
        !string.IsNullOrWhiteSpace(layout.Name) && layout.Width is >= 1 and <= 100 && layout.Height is >= 1 and <= 100;

    private sealed record ScoreFile(int Version, HighScoreBoard[] Boards);
}
