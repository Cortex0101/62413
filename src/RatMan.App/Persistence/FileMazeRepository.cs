using System.IO;
using System.Text.Json;
using RatMan.Game;

namespace RatMan.App.Persistence;

public sealed class FileMazeRepository : IMazeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly string directory;

    public FileMazeRepository(string? directory = null)
    {
        this.directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RatMan", "Mazes");
    }

    private void Initialize()
    {
        Directory.CreateDirectory(directory);
        string marker = Path.Combine(directory, ".initialized");
        if (File.Exists(marker)) return;
        string classic = Path.Combine(directory, "Classic.maze");
        if (!File.Exists(classic))
        {
            using Stream source = typeof(FileMazeRepository).Assembly.GetManifestResourceStream("RatMan.App.Classic.maze")
                ?? throw new IOException("The bundled Classic maze is missing.");
            string temporary = classic + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream target = new(temporary, FileMode.CreateNew, FileAccess.Write)) source.CopyTo(target);
                // Another instance may have initialized the library while we copied.
                try { File.Move(temporary, classic, overwrite: false); }
                catch (IOException) when (File.Exists(classic)) { }
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        File.WriteAllText(marker, "1");
    }

    public IReadOnlyList<MazeEntry> List()
    {
        Initialize();
        List<MazeEntry> entries = [];
        foreach (string path in Directory.EnumerateFiles(directory, "*.maze"))
        {
            try
            {
                MazeDefinition maze = Load(path);
                entries.Add(new(path, maze.Name, maze.Size));
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                entries.Add(new(path, Path.GetFileNameWithoutExtension(path), null, exception.Message));
            }
        }
        return entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public MazeDefinition Load(string path)
    {
        try
        {
            MazeFile file = JsonSerializer.Deserialize<MazeFile>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException("Empty maze file.");
            if (file.Version is not (1 or 2)) throw new InvalidDataException("Unsupported maze version.");
            if (file.Width is < 1 or > 100 || file.Height is < 1 or > 100)
                throw new InvalidDataException("Width and height must be between 1 and 100.");
            if (file.Walls is null) throw new InvalidDataException("The walls array is missing.");
            if (file.Walls.Any(w => w is null || w.FirstTile is null || w.SecondTile is null))
                throw new InvalidDataException("Every wall needs two tile coordinates.");
            BoardSize size = new(file.Height, file.Width);
            if (file.Walls.Any(w => !size.Contains(w.FirstTile.ToPosition()) || !size.Contains(w.SecondTile.ToPosition())))
                throw new InvalidDataException("Every wall must be inside the board.");
            MazeDefinition maze = new(file.Name ?? "", size,
                file.Walls.Select(w => new Wall(w.FirstTile.ToPosition(), w.SecondTile.ToPosition())).ToHashSet(),
                file.PlayerSpawn?.ToPosition(), file.MonsterSpawn?.ToPosition())
            {
                OpenBorderRows = file.OpenBorderRows ?? [], OpenBorderColumns = file.OpenBorderColumns ?? [],
                EmptyFoodTiles = file.Version == 2 ? ReadFoodCoordinates(file.EmptyFoodTiles) : [],
                SteroidCheeseTiles = file.Version == 2 ? ReadFoodCoordinates(file.SteroidCheeseTiles) : []
            };
            IReadOnlyList<string> errors = maze.Validate();
            if (errors.Count > 0) throw new InvalidDataException(string.Join(" ", errors));
            return maze;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            throw new InvalidDataException("Invalid maze data: " + exception.Message, exception);
        }
    }

    public string GetNewPath(string name)
    {
        string safeName = string.Concat(name.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c))
            .TrimEnd('.', ' ');
        if (string.IsNullOrEmpty(safeName)) safeName = "Board";
        safeName = safeName[..Math.Min(safeName.Length, 80)];
        string stem = safeName.Split('.')[0];
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(stem, StringComparer.OrdinalIgnoreCase))
            safeName = "_" + safeName;
        return Path.Combine(directory, safeName + ".maze");
    }

    public bool Exists(string path)
    {
        Initialize();
        return File.Exists(path);
    }

    public void Save(string path, MazeDefinition maze)
    {
        IReadOnlyList<string> errors = maze.Validate();
        if (errors.Count > 0) throw new InvalidDataException(string.Join(" ", errors));
        Initialize();
        MazeFile file = new()
        {
            OpenBorderRows = maze.OpenBorderRows.Order().ToArray(),
            OpenBorderColumns = maze.OpenBorderColumns.Order().ToArray(),
            Version = 2, Name = maze.Name.Trim(), Width = maze.Size.Columns, Height = maze.Size.Rows,
            EmptyFoodTiles = WriteFoodCoordinates(maze.EmptyFoodTiles),
            SteroidCheeseTiles = WriteFoodCoordinates(maze.SteroidCheeseTiles),
            PlayerSpawn = Coordinate.From(maze.PlayerSpawn!.Value),
            MonsterSpawn = Coordinate.From(maze.MonsterSpawn!.Value),
            Walls = maze.Walls.OrderBy(w => w.FirstTile.Row).ThenBy(w => w.FirstTile.Column)
                .ThenBy(w => w.SecondTile.Row).ThenBy(w => w.SecondTile.Column)
                .Select(w => new Edge { FirstTile = Coordinate.From(w.FirstTile), SecondTile = Coordinate.From(w.SecondTile) }).ToArray()
        };
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(file, JsonOptions));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static bool IsFileError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException;

    private static GridPosition[] ReadFoodCoordinates(Coordinate[]? coordinates)
    {
        if (coordinates is null) return [];
        if (coordinates.Any(coordinate => coordinate is null))
            throw new InvalidDataException("Every food tile needs row and column coordinates.");
        return coordinates.Select(coordinate => coordinate.ToPosition()).Distinct().ToArray();
    }

    private static Coordinate[] WriteFoodCoordinates(IEnumerable<GridPosition> positions) =>
        positions.Distinct().OrderBy(position => position.Row).ThenBy(position => position.Column)
            .Select(Coordinate.From).ToArray();

    private sealed class MazeFile
    {
        public Coordinate[]? EmptyFoodTiles { get; set; }
        public Coordinate[]? SteroidCheeseTiles { get; set; }
        public int[]? OpenBorderRows { get; set; }
        public int[]? OpenBorderColumns { get; set; }
        public int Version { get; set; }
        public string? Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Coordinate? PlayerSpawn { get; set; }
        public Coordinate? MonsterSpawn { get; set; }
        public Edge[]? Walls { get; set; }
    }

    private sealed class Coordinate
    {
        public required int Row { get; set; }
        public required int Column { get; set; }
        public GridPosition ToPosition() => new(Row, Column);
        public static Coordinate From(GridPosition position) => new() { Row = position.Row, Column = position.Column };
    }

    private sealed class Edge
    {
        public required Coordinate FirstTile { get; set; }
        public required Coordinate SecondTile { get; set; }
    }
}
