namespace RatMan.Game;

public sealed class GameSession
{
    private readonly HashSet<Wall> walls;
    private readonly BoardTopology topology;
    private readonly Dictionary<GridPosition, FoodKind> remainingFood;
    private readonly Dictionary<GridPosition, FoodKind> initialFood;
    private readonly GridPosition playerSpawn;
    private readonly CatNavigator? navigator;
    private readonly Dictionary<CatKind, ICatTargetStrategy> strategies = Enum.GetValues<CatKind>()
        .ToDictionary(kind => kind, CatTargets.For);
    private Direction? queuedDirection;
    private long accumulatedTicks;
    private TimeSpan respawnRemaining;
    private bool continueWalking;
    public IReadOnlyDictionary<GridPosition, FoodKind> RemainingFood { get; }
    public IReadOnlyList<Cat> Cats { get; } = Array.Empty<Cat>();
    public GameRules Rules { get; }
    public int Lives { get; private set; }
    public int CollectedCheeseCount { get; private set; }
    public int InitialCheeseCount => initialFood.Count;
    public TimeSpan PowerRemaining { get; private set; }
    public bool IsPowered => PowerRemaining > TimeSpan.Zero;
    public RoundStatus Status { get; private set; } = RoundStatus.Playing;
    public bool IsFinished => Status is RoundStatus.Won or RoundStatus.GameOver;
    public bool IsMonsterDisconnected => navigator is not null && !navigator.CanReach(playerSpawn);
    public event Action<FoodCollection>? FoodCollected;
    public int Score { get; private set; }
    public IReadOnlyCollection<int> OpenBorderRows { get; } = [];
    public IReadOnlyCollection<int> OpenBorderColumns { get; } = [];

    public GameSession(MazeDefinition maze, GameRules? rules = null)
        : this(Validated(maze).Size, maze.PlayerSpawn!.Value, maze.Walls, rules)
    {
        Name = maze.Name;
        MonsterStartingPosition = maze.MonsterSpawn;
        OpenBorderRows = maze.OpenBorderRows.ToArray();
        OpenBorderColumns = maze.OpenBorderColumns.ToArray();
        topology = new(BoardSize, walls, OpenBorderRows, OpenBorderColumns);
        remainingFood.Clear();
        foreach (var item in FoodLayout.Create(maze)) remainingFood.Add(item.Key, item.Value);
        initialFood = new(remainingFood);
        navigator = new(topology, maze.MonsterSpawn!.Value);
        int[] percentages = [0, 10, 25, 50];
        Cats = Array.AsReadOnly(Enum.GetValues<CatKind>().Select((kind, index) =>
            new Cat(kind, maze.MonsterSpawn.Value, (initialFood.Count * percentages[index] + 99) / 100)).ToArray());
        ReleaseCats();
        StartCats();
    }

    private static MazeDefinition Validated(MazeDefinition maze)
    {
        ArgumentNullException.ThrowIfNull(maze);
        IReadOnlyList<string> errors = maze.Validate();
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(maze));
        return maze;
    }

    public string Name { get; } = "RatMan";

    public GridPosition? MonsterStartingPosition { get; }

    public GameSession(
        BoardSize boardSize,
        GridPosition playerStartingPosition,
        IEnumerable<Wall>? walls = null,
        GameRules? rules = null)
    {
        ArgumentNullException.ThrowIfNull(boardSize);

        if (!boardSize.Contains(playerStartingPosition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerStartingPosition),
                playerStartingPosition,
                "The player must start inside the board.");
        }

        this.walls = walls?.ToHashSet() ?? [];

        foreach (Wall wall in this.walls)
        {
            if (!boardSize.Contains(wall.FirstTile) || !boardSize.Contains(wall.SecondTile))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(walls),
                    wall,
                    "Every wall must be inside the board.");
            }
        }

        BoardSize = boardSize;
        Rules = rules ?? new();
        Rules.Validate();
        Lives = Rules.StartingLives;
        playerSpawn = playerStartingPosition;
        Player = new Player(playerStartingPosition);
        topology = new(BoardSize, this.walls, [], []);
        remainingFood = FoodLayout.Create(new("", boardSize, this.walls, playerStartingPosition, null));
        initialFood = new(remainingFood);
        RemainingFood = new System.Collections.ObjectModel.ReadOnlyDictionary<GridPosition, FoodKind>(remainingFood);
    }

    public BoardSize BoardSize { get; }

    public Player Player { get; }

    public IReadOnlySet<Wall> Walls => walls;

    // Explicit single-tile API for domain callers; live play uses RequestDirection/Advance.
    public FoodCollection? CollectFoodAtPlayer()
    {
        if (Player.Motion.IsMoving || Status != RoundStatus.Playing) return null;
        if (!remainingFood.TryGetValue(Player.Position, out FoodKind kind)) return null;
        int points = FoodPoints.For(kind);
        int nextScore = checked(Score + points);
        remainingFood.Remove(Player.Position);
        Score = nextScore;
        CollectedCheeseCount++;
        if (kind == FoodKind.Steroid)
        {
            PowerRemaining = Rules.PowerDuration;
            UpdateCatModes();
        }
        ReleaseCats();
        FoodCollection collected = new(Player.Position, kind, points);
        FoodCollected?.Invoke(collected);
        return collected;
    }

    public bool TryMovePlayer(Direction direction)
    {
        if (Player.Motion.IsMoving || Status != RoundStatus.Playing) return false;
        if (!topology.TryStep(Player.Position, direction, out GridPosition destination))
        {
            return false;
        }

        Player.MoveTo(destination, direction);
        return true;
    }

    public void RequestDirection(Direction direction)
    {
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        if (Status != RoundStatus.Playing) return;
        queuedDirection = direction;
        if (!Player.Motion.IsMoving && Player.BlockedRemaining == TimeSpan.Zero) StartPlayer();
    }

    public void Advance(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        if (IsFinished) return;
        accumulatedTicks = checked(accumulatedTicks + elapsed.Ticks);
        while (accumulatedTicks >= GameRules.SimulationStep.Ticks && !IsFinished)
        {
            accumulatedTicks -= GameRules.SimulationStep.Ticks;
            Tick();
        }
        if (IsFinished) accumulatedTicks = 0;
    }

    public void Restart()
    {
        remainingFood.Clear();
        foreach (var item in initialFood) remainingFood.Add(item.Key, item.Value);
        Score = CollectedCheeseCount = 0;
        Lives = Rules.StartingLives;
        accumulatedTicks = 0;
        ResetActors();
    }

    private void Tick()
    {
        TimeSpan step = GameRules.SimulationStep;
        if (Status == RoundStatus.LifeLost)
        {
            respawnRemaining -= step;
            if (respawnRemaining <= TimeSpan.Zero) ResetActors();
            return;
        }
        if (remainingFood.Count == 0)
        {
            Status = RoundStatus.Won;
            return;
        }

        bool wasBlocked = Player.BlockedRemaining > TimeSpan.Zero;
        Player.BlockedRemaining = MaxZero(Player.BlockedRemaining - step);
        MovementSegment playerSegment = Player.Motion.Advance(step);
        // Capture and advance everybody before deciding the next routes.
        var catSegments = Cats.Select(cat => (Cat: cat, Segment: cat.Motion.Advance(step))).ToArray();

        // Interior edge crossings happen before food pickup at the end of this tick.
        foreach (var contact in catSegments.Where(c => c.Cat.CanContactPlayer)
            .Select(c => (c.Cat, Time: playerSegment.ContactTime(c.Segment)))
            .Where(c => c.Time is >= 0 and < 1).OrderBy(c => c.Time))
        {
            Contact(contact.Cat);
            if (Status != RoundStatus.Playing) return;
        }

        PowerRemaining = MaxZero(PowerRemaining - step);
        UpdateCatModes();
        if (playerSegment.Arrived) CollectFoodAtPlayer();
        if (remainingFood.Count == 0)
        {
            Status = RoundStatus.Won;
            return;
        }

        foreach (Cat cat in Cats)
        {
            if (cat.State == CatState.ReturningHome && !cat.Motion.IsMoving && cat.Position == MonsterStartingPosition)
                cat.State = IsPowered ? CatState.Recovering : CatState.Chase;
            if (cat.CanContactPlayer && Player.Motion.Snapshot().ContactTime(cat.Motion.Snapshot()).HasValue)
            {
                Contact(cat);
                if (Status != RoundStatus.Playing) return;
            }
        }
        if (playerSegment.Arrived || wasBlocked && Player.BlockedRemaining == TimeSpan.Zero) StartPlayer();
        StartCats();
    }

    private void Contact(Cat cat)
    {
        if (cat.State == CatState.Scatter)
        {
            Score = checked(Score + Rules.CatPoints);
            cat.State = CatState.ReturningHome;
            return;
        }
        Lives--;
        PowerRemaining = TimeSpan.Zero;
        UpdateCatModes();
        queuedDirection = null;
        continueWalking = false;
        Status = Lives == 0 ? RoundStatus.GameOver : RoundStatus.LifeLost;
        respawnRemaining = Rules.RespawnDelay;
    }

    private void ResetActors()
    {
        Status = RoundStatus.Playing;
        PowerRemaining = respawnRemaining = TimeSpan.Zero;
        queuedDirection = null;
        continueWalking = false;
        Player.Motion.Reset(playerSpawn);
        Player.BlockedRemaining = TimeSpan.Zero;
        foreach (Cat cat in Cats)
        {
            cat.Motion.Reset(MonsterStartingPosition!.Value);
            cat.State = CatState.Waiting;
        }
        ReleaseCats();
        StartCats();
    }

    private void ReleaseCats()
    {
        foreach (Cat cat in Cats)
            if (cat.State == CatState.Waiting && CollectedCheeseCount >= cat.ReleaseCount)
                cat.State = IsPowered ? CatState.Scatter : CatState.Chase;
    }

    private void UpdateCatModes()
    {
        foreach (Cat cat in Cats)
            if (cat.CanContactPlayer || cat.State == CatState.Recovering && !IsPowered)
                cat.State = IsPowered ? CatState.Scatter : CatState.Chase;
    }

    private void StartPlayer()
    {
        if (queuedDirection is { } requested && Begin(Player.Motion, requested, Rules.PlayerMoveDuration))
        {
            queuedDirection = null;
            continueWalking = true;
            return;
        }
        if (!continueWalking) return;
        if (!Begin(Player.Motion, Player.FacingDirection, Rules.PlayerMoveDuration))
        {
            continueWalking = false;
            Player.BlockedRemaining = TimeSpan.FromMilliseconds(210);
        }
    }

    private void StartCats()
    {
        foreach (Cat cat in Cats)
        {
            if (!cat.IsVisible || cat.Motion.IsMoving) continue;
            bool returning = cat.State == CatState.ReturningHome;
            if (returning && cat.Position == MonsterStartingPosition)
            {
                cat.State = IsPowered ? CatState.Recovering : CatState.Chase;
                if (cat.State == CatState.Recovering) continue;
                returning = false;
            }
            GridPosition target = returning ? MonsterStartingPosition!.Value
                : cat.State == CatState.Scatter ? CatTargets.Corner(cat.Kind, BoardSize)
                : strategies[cat.Kind].Target(cat, Player, Cats[0].Position, BoardSize);
            Direction? direction = navigator!.ChooseDirection(cat.Position, target,
                cat.Motion.HasMoved ? cat.Motion.FacingDirection : null, returning);
            if (direction is { } next)
                Begin(cat.Motion, next, returning ? Rules.ReturnMoveDuration
                    : cat.State == CatState.Scatter ? Rules.ScatterMoveDuration : Rules.ChaseMoveDuration);
        }
    }

    private bool Begin(ActorMotion motion, Direction direction, TimeSpan duration)
    {
        if (!topology.TryStep(motion.Position, direction, out GridPosition destination) || destination == motion.Position) return false;
        motion.Begin(destination, direction, !BoardSize.Contains(motion.Position.AdjacentTo(direction)), duration);
        return true;
    }

    private static TimeSpan MaxZero(TimeSpan value) => value > TimeSpan.Zero ? value : TimeSpan.Zero;
}
