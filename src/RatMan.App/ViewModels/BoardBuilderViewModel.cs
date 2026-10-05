using System.Windows.Input;
using System.Windows.Media;
using RatMan.App.Animation;
using RatMan.App.Mvvm;
using RatMan.App.Persistence;
using RatMan.App.Services;
using RatMan.Game;

namespace RatMan.App.ViewModels;

public enum BuilderTool { Wall, Player, Monster, NormalCheese, SteroidCheese }

public sealed class BoardBuilderViewModel : ScreenViewModel
{
    private readonly IMazeRepository repository;
    private readonly IEditorDialogs dialogs;
    private readonly HashSet<Wall> walls = [];
    private readonly HashSet<int> openRows = [], openColumns = [];
    private readonly HashSet<GridPosition> emptyFoodTiles = [], steroidCheeseTiles = [];
    private BoardSize size = new(11, 12);
    private string name = "", widthInput = "12", heightInput = "11", status = "";
    private GridPosition? playerSpawn, monsterSpawn;
    private BuilderTool tool;
    private bool vertical, dirty;
    private string? filePath;
    private Wall? previewWall;
    private GridPosition? previewCell;
    private bool applyingDimensions;
    private IReadOnlyList<BoardCellViewModel>? boardCells;

    public BoardBuilderViewModel(IMazeRepository repository, IEditorDialogs dialogs,
        Action returnToMenu, Action loadBoard) : base("Board Builder")
    {
        this.repository = repository;
        this.dialogs = dialogs;
        SaveCommand = new RelayCommand(_ => Save());
        NewBoardCommand = new RelayCommand(_ => { if (CanLeave()) Reset(); });
        LoadBoardCommand = new RelayCommand(_ => { if (CanLeave()) loadBoard(); });
        ReturnToMenuCommand = new RelayCommand(_ => { if (CanLeave()) returnToMenu(); });
        SelectToolCommand = new RelayCommand(value => { if (value is BuilderTool selected) SelectedTool = selected; });
        RotateCommand = new RelayCommand(_ => Rotate());
        RefreshBoard();
        Food.Animator.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Food.Animator.CurrentImage) && SelectedTool == BuilderTool.SteroidCheese)
                OnPropertyChanged(nameof(PreviewFoodImage));
        };
    }

    public ICommand SaveCommand { get; }
    public ICommand NewBoardCommand { get; }
    public ICommand LoadBoardCommand { get; }
    public ICommand ReturnToMenuCommand { get; }
    public ICommand SelectToolCommand { get; }
    public ICommand RotateCommand { get; }
    public BoardSize Size => size;
    public string? FilePath => filePath;
    public bool IsDirty => dirty;
    public string BoardName { get => name; set { if (SetProperty(ref name, value)) Changed(); } }
    public string WidthInput { get => widthInput; set { if (SetProperty(ref widthInput, value)) Changed(); } }
    public string HeightInput { get => heightInput; set { if (SetProperty(ref heightInput, value)) Changed(); } }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    public BuilderTool SelectedTool
    {
        get => tool;
        set { if (SetProperty(ref tool, value)) { ClearPreview(); OnPropertyChanged(nameof(ToolDescription)); } }
    }
    public bool IsVertical => vertical;
    public FoodPresentationViewModel Food { get; } = new();
    public bool IsFoodTool => SelectedTool is BuilderTool.NormalCheese or BuilderTool.SteroidCheese;
    public void Advance(TimeSpan elapsed) => Food.Advance(elapsed);
    public string ToolDescription => SelectedTool switch
    {
        BuilderTool.Wall => $"Selected: {(vertical ? "vertical" : "horizontal")} wall · R rotates",
        BuilderTool.Player => "Selected: player spawn",
        BuilderTool.Monster => "Selected: monster spawn",
        BuilderTool.NormalCheese => "Normal cheese · 1 point · Right-click clears food",
        _ => "Steroid cheese · 5 points · Right-click clears food"
    };
    public IReadOnlyList<BoardCellViewModel> BoardCells => boardCells ??= BoardProjection.Cells(size);
    public IReadOnlyList<WallSegmentViewModel> WallSegments => BoardProjection.Segments(walls);
    public IReadOnlyList<WallSegmentViewModel> RenderedWalls => WallSegments.Concat(BoardProjection.Borders(size, openRows, openColumns)).ToArray();
    public double BoardWidth => size.Columns * BoardProjection.CellSize;
    public double BoardHeight => size.Rows * BoardProjection.CellSize;
    public GridPosition? PlayerSpawn => playerSpawn;
    public GridPosition? MonsterSpawn => monsterSpawn;
    public bool HasPlayerSpawn => playerSpawn.HasValue;
    public bool HasMonsterSpawn => monsterSpawn.HasValue;
    public double PlayerLeft => (playerSpawn?.Column ?? 0) * BoardProjection.CellSize;
    public double PlayerTop => (playerSpawn?.Row ?? 0) * BoardProjection.CellSize;
    public double MonsterLeft => (monsterSpawn?.Column ?? 0) * BoardProjection.CellSize;
    public double MonsterTop => (monsterSpawn?.Row ?? 0) * BoardProjection.CellSize;
    public IReadOnlyList<WallSegmentViewModel> PreviewSegments => previewWall is { } wall ? [BoardProjection.Segment(wall)] : [];
    public bool HasPreviewCell => previewCell.HasValue && !IsFoodTool;
    public bool HasFoodPreview => previewCell is { } cell && IsFoodTool && CanPlaceFood(cell);
    public ImageSource? PreviewFoodImage => !HasFoodPreview ? null : SelectedTool == BuilderTool.SteroidCheese
        ? Food.Animator.CurrentImage : CheeseSpriteSheet.NormalImage(Food.VariantAt(previewCell!.Value));
    public double PreviewLeft => (previewCell?.Column ?? 0) * BoardProjection.CellSize;
    public double PreviewTop => (previewCell?.Row ?? 0) * BoardProjection.CellSize;
    public string PreviewLabel => SelectedTool == BuilderTool.Player ? "P" : "M";

    public MazeDefinition Definition => new(name, size, walls.ToArray(), playerSpawn, monsterSpawn)
    {
        OpenBorderRows = openRows.ToArray(), OpenBorderColumns = openColumns.ToArray(),
        EmptyFoodTiles = emptyFoodTiles.ToArray(), SteroidCheeseTiles = steroidCheeseTiles.ToArray()
    };

    private void Changed()
    {
        dirty = true;
        OnPropertyChanged(nameof(IsDirty));
        Status = "Unsaved changes";
    }

    public bool ApplyDimensions()
    {
        // A crop dialog moves keyboard focus, which can trigger the view's
        // dimension-commit handler again before this confirmation completes.
        if (applyingDimensions) return false;
        applyingDimensions = true;
        try { return ApplyDimensionsCore(); }
        finally { applyingDimensions = false; }
    }

    private bool ApplyDimensionsCore()
    {
        if (!int.TryParse(widthInput, out int width) || !int.TryParse(heightInput, out int height) ||
            width is < 1 or > 100 || height is < 1 or > 100)
        {
            Status = "Width and height must be whole numbers between 1 and 100.";
            return false;
        }
        BoardSize next = new(height, width);
        if (next == size) return true;
        bool crops = emptyFoodTiles.Concat(steroidCheeseTiles).Any(position => !next.Contains(position)) ||
            openRows.Any(row => row >= next.Rows) || openColumns.Any(column => column >= next.Columns) || walls.Any(w => !next.Contains(w.FirstTile) || !next.Contains(w.SecondTile)) ||
            playerSpawn is { } p && !next.Contains(p) || monsterSpawn is { } m && !next.Contains(m);
        if (crops && !dialogs.ConfirmCrop())
        {
            widthInput = size.Columns.ToString(); heightInput = size.Rows.ToString();
            OnPropertyChanged(nameof(WidthInput)); OnPropertyChanged(nameof(HeightInput));
            return false;
        }
        size = next;
        emptyFoodTiles.RemoveWhere(position => !size.Contains(position));
        steroidCheeseTiles.RemoveWhere(position => !size.Contains(position));
        openRows.RemoveWhere(row => row >= size.Rows);
        openColumns.RemoveWhere(column => column >= size.Columns);
        boardCells = null;
        walls.RemoveWhere(w => !size.Contains(w.FirstTile) || !size.Contains(w.SecondTile));
        if (playerSpawn is { } player && !size.Contains(player)) playerSpawn = null;
        if (monsterSpawn is { } monster && !size.Contains(monster)) monsterSpawn = null;
        ClearPreview(); Changed(); RefreshBoard();
        if (crops) Status = "Board cropped. " + string.Join(" ", Definition.Validate());
        return true;
    }

    public void Rotate()
    {
        if (SelectedTool != BuilderTool.Wall) return;
        vertical = !vertical;
        ClearPreview();
        OnPropertyChanged(nameof(IsVertical)); OnPropertyChanged(nameof(ToolDescription));
    }

    public Wall? WallAt(double x, double y, bool verticalWall)
    {
        const double cell = BoardProjection.CellSize;
        if (x < 0 || y < 0 || x > BoardWidth || y > BoardHeight) return null;
        int column = Math.Min((int)(x / cell), size.Columns - 1), row = Math.Min((int)(y / cell), size.Rows - 1);
        int boundary = (int)Math.Round((verticalWall ? x : y) / cell, MidpointRounding.AwayFromZero);
        if (Math.Abs((verticalWall ? x : y) - boundary * cell) > 8) return null;
        if (verticalWall)
            return boundary >= 0 && boundary <= size.Columns ? new Wall(new(row, boundary - 1), new(row, boundary)) : null;
        return boundary >= 0 && boundary <= size.Rows ? new Wall(new(boundary - 1, column), new(boundary, column)) : null;
    }

    private GridPosition? CellAt(double x, double y) =>
        x >= 0 && y >= 0 && x < BoardWidth && y < BoardHeight
            ? new((int)(y / BoardProjection.CellSize), (int)(x / BoardProjection.CellSize)) : null;

    public void Preview(double x, double y)
    {
        previewWall = SelectedTool == BuilderTool.Wall ? WallAt(x, y, vertical) : null;
        previewCell = SelectedTool == BuilderTool.Wall ? null : CellAt(x, y);
        RefreshPreview();
    }

    public void ClearPreview() { previewWall = null; previewCell = null; RefreshPreview(); }
    private void RefreshPreview()
    {
        OnPropertyChanged(nameof(PreviewSegments)); OnPropertyChanged(nameof(HasPreviewCell));
        OnPropertyChanged(nameof(PreviewLeft)); OnPropertyChanged(nameof(PreviewTop)); OnPropertyChanged(nameof(PreviewLabel));
        OnPropertyChanged(nameof(HasFoodPreview)); OnPropertyChanged(nameof(PreviewFoodImage));
    }

    public void Place(double x, double y)
    {
        if (IsFoodTool) { EditFood(x, y, remove: false); return; }
        bool changed = false;
        if (SelectedTool == BuilderTool.Wall)
        {
            if (WallAt(x, y, vertical) is { } wall) changed = SetWall(wall, true);
        }
        else if (CellAt(x, y) is { } cell)
        {
            if (SelectedTool == BuilderTool.Player) { changed = playerSpawn != cell; playerSpawn = cell; }
            else { changed = monsterSpawn != cell; monsterSpawn = cell; }
        }
        if (changed) { Changed(); RefreshBoard(); }
    }

    public void Remove(double x, double y)
    {
        if (IsFoodTool) { EditFood(x, y, remove: true); return; }
        Wall? horizontal = WallAt(x, y, false), upright = WallAt(x, y, true);
        Wall? candidate = new[] { horizontal, upright }.Where(w => w.HasValue && HasWall(w.Value))
            .OrderBy(w => w!.Value.FirstTile.Row == w.Value.SecondTile.Row
                ? Math.Abs(x - w.Value.SecondTile.Column * BoardProjection.CellSize)
                : Math.Abs(y - w.Value.SecondTile.Row * BoardProjection.CellSize)).FirstOrDefault();
        bool changed = candidate is { } wall && SetWall(wall, false);
        if (!changed && CellAt(x, y) is { } cell)
        {
            if (playerSpawn == cell) { playerSpawn = null; changed = true; }
            if (monsterSpawn == cell) { monsterSpawn = null; changed = true; }
        }
        if (changed) { Changed(); RefreshBoard(); }
    }

    private HashSet<int>? BorderOpenings(Wall wall) =>
        wall.FirstTile.Column < 0 || wall.SecondTile.Column >= size.Columns ? openRows :
        wall.FirstTile.Row < 0 || wall.SecondTile.Row >= size.Rows ? openColumns : null;

    private bool CanPlaceFood(GridPosition cell) => size.Contains(cell) && cell != playerSpawn &&
        !BoardCells[cell.Row * size.Columns + cell.Column].IsBlocked;

    private void EditFood(double x, double y, bool remove)
    {
        if (CellAt(x, y) is not { } cell) return;
        if (!CanPlaceFood(cell))
        {
            Status = cell == playerSpawn ? "The player starting tile stays empty." : "Food needs a tile reachable from the player.";
            return;
        }
        bool changed;
        if (remove) changed = emptyFoodTiles.Add(cell) | steroidCheeseTiles.Remove(cell);
        else if (SelectedTool == BuilderTool.SteroidCheese)
            changed = steroidCheeseTiles.Add(cell) | emptyFoodTiles.Remove(cell);
        else changed = emptyFoodTiles.Remove(cell) | steroidCheeseTiles.Remove(cell);
        if (changed) { Changed(); RefreshBoard(); }
    }

    private int BorderIndex(Wall wall) => wall.FirstTile.Row == wall.SecondTile.Row
        ? wall.FirstTile.Row : wall.FirstTile.Column;

    private bool HasWall(Wall wall) => BorderOpenings(wall) is { } openings
        ? !openings.Contains(BorderIndex(wall)) : walls.Contains(wall);

    private bool SetWall(Wall wall, bool present) => BorderOpenings(wall) is { } openings
        ? present ? openings.Remove(BorderIndex(wall)) : openings.Add(BorderIndex(wall))
        : present ? walls.Add(wall) : walls.Remove(wall);

    public bool Save()
    {
        if (!ApplyDimensions()) return false;
        IReadOnlyList<string> errors = Definition.Validate();
        if (errors.Count > 0) { Status = string.Join(" ", errors); return false; }
        try
        {
            string destination = filePath ?? repository.GetNewPath(name);
            if (filePath is null && repository.Exists(destination) && !dialogs.ConfirmOverwrite(destination)) return false;
            repository.Save(destination, Definition);
            filePath = destination; dirty = false;
            OnPropertyChanged(nameof(IsDirty)); OnPropertyChanged(nameof(FilePath));
            Status = $"Saved {System.IO.Path.GetFileName(destination)}";
            return true;
        }
        catch (Exception exception) when (FileMazeRepository.IsFileError(exception))
        { Status = "Could not save board: " + exception.Message; return false; }
    }

    public bool CanLeave() => !dirty || dialogs.ConfirmUnsaved() switch
    {
        UnsavedChoice.Save => Save(), UnsavedChoice.Discard => true, _ => false
    };

    public void Load(MazeDefinition maze, string path)
    {
        size = maze.Size; name = maze.Name; playerSpawn = maze.PlayerSpawn; monsterSpawn = maze.MonsterSpawn;
        openRows.Clear(); openRows.UnionWith(maze.OpenBorderRows);
        openColumns.Clear(); openColumns.UnionWith(maze.OpenBorderColumns);
        walls.Clear(); walls.UnionWith(maze.Walls); filePath = path;
        emptyFoodTiles.Clear(); emptyFoodTiles.UnionWith(maze.EmptyFoodTiles);
        steroidCheeseTiles.Clear(); steroidCheeseTiles.UnionWith(maze.SteroidCheeseTiles);
        ResetPresentation();
        Status = "Board loaded";
    }

    private void Reset()
    {
        size = new(11, 12); name = ""; playerSpawn = null; monsterSpawn = null; walls.Clear(); openRows.Clear(); openColumns.Clear(); filePath = null;
        emptyFoodTiles.Clear(); steroidCheeseTiles.Clear();
        ResetPresentation(); Status = "Choose a tool and click the board to begin.";
    }

    private void ResetPresentation()
    {
        boardCells = null;
        ClearPreview();
        Food.Reset();
        widthInput = size.Columns.ToString(); heightInput = size.Rows.ToString(); dirty = false;
        SelectedTool = BuilderTool.Wall; vertical = false;
        foreach (string property in new[] { nameof(BoardName), nameof(WidthInput), nameof(HeightInput), nameof(FilePath), nameof(IsDirty), nameof(IsVertical), nameof(ToolDescription) }) OnPropertyChanged(property);
        ClearPreview(); RefreshBoard();
    }

    private void RefreshBoard()
    {
        BoardProjection.UpdateBlocked(BoardCells, size, walls, playerSpawn, openRows, openColumns);
        Food.Refresh(FoodLayout.Create(Definition));
        RefreshPreview();
        OnPropertyChanged(nameof(RenderedWalls));
        foreach (string property in new[] { nameof(Size), nameof(BoardCells), nameof(WallSegments), nameof(BoardWidth), nameof(BoardHeight), nameof(PlayerSpawn), nameof(MonsterSpawn), nameof(HasPlayerSpawn), nameof(HasMonsterSpawn), nameof(PlayerLeft), nameof(PlayerTop), nameof(MonsterLeft), nameof(MonsterTop) }) OnPropertyChanged(property);
    }
}
