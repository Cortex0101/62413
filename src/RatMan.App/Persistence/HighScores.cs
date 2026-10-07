using System.Security.Cryptography;
using System.Text.Json;
using RatMan.Game;

namespace RatMan.App.Persistence;

public sealed record HighScoreLayout(string Id, string Name, int Width, int Height)
{
    public static HighScoreLayout FromMaze(MazeDefinition maze)
    {
        // Canonical gameplay data: display names, paths and cosmetic food variants are irrelevant.
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Version = 1,
            maze.Size.Columns,
            maze.Size.Rows,
            maze.PlayerSpawn,
            maze.MonsterSpawn,
            Walls = maze.Walls.Distinct().OrderBy(w => w.FirstTile.Row).ThenBy(w => w.FirstTile.Column)
                .ThenBy(w => w.SecondTile.Row).ThenBy(w => w.SecondTile.Column).ToArray(),
            OpenRows = maze.OpenBorderRows.Distinct().Order().ToArray(),
            OpenColumns = maze.OpenBorderColumns.Distinct().Order().ToArray(),
            Food = FoodLayout.Create(maze).OrderBy(p => p.Key.Row).ThenBy(p => p.Key.Column)
                .Select(p => new { p.Key.Row, p.Key.Column, Kind = (int)p.Value }).ToArray()
        });
        return new(Convert.ToHexString(SHA256.HashData(data)), maze.Name, maze.Size.Columns, maze.Size.Rows);
    }
}

public sealed record HighScoreEntry(Guid Id, string Name, int Score, DateTimeOffset CompletedAt);
public sealed record HighScoreBoard(HighScoreLayout Layout, IReadOnlyList<HighScoreEntry> Entries);
public sealed record HighScoreSubmission(bool Saved, int? Rank);

public static class HighScoreRules
{
    public const int Capacity = 10;
    public const int MaximumNameLength = 24;

    // Stable ordering deliberately leaves previously saved tied scores ahead of new entries.
    public static HighScoreEntry[] Top(IEnumerable<HighScoreEntry> entries) =>
        entries.OrderByDescending(e => e.Score).Take(Capacity).ToArray();

    public static int? QualifyingRank(IEnumerable<HighScoreEntry> entries, int score)
    {
        if (score < 0) return null;
        int rank = entries.Count(e => e.Score >= score) + 1;
        return rank <= Capacity ? rank : null;
    }

    public static string NormalizeName(string? name)
    {
        string value = (name ?? "").Trim();
        if (value.Length == 0) return "Anonymous";
        if (value.Length <= MaximumNameLength) return value;
        int length = char.IsHighSurrogate(value[MaximumNameLength - 1]) ? MaximumNameLength - 1 : MaximumNameLength;
        return value[..length];
    }
}

public interface IHighScoreRepository
{
    Task<IReadOnlyList<HighScoreBoard>> LoadAsync();
    Task<HighScoreSubmission> SubmitAsync(HighScoreLayout layout, HighScoreEntry entry);
}
