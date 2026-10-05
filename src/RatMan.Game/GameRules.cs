namespace RatMan.Game;

public sealed record GameRules
{
    public static readonly TimeSpan SimulationStep = TimeSpan.FromMilliseconds(10);
    public TimeSpan PlayerMoveDuration { get; init; } = TimeSpan.FromMilliseconds(350);
    public TimeSpan ChaseMoveDuration { get; init; } = TimeSpan.FromMilliseconds(400);
    public TimeSpan ScatterMoveDuration { get; init; } = TimeSpan.FromMilliseconds(600);
    public TimeSpan ReturnMoveDuration { get; init; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan PowerDuration { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan RespawnDelay { get; init; } = TimeSpan.FromSeconds(1);
    public int StartingLives { get; init; } = 3;
    public int CatPoints { get; init; } = 20;

    internal void Validate()
    {
        foreach (TimeSpan duration in new[] { PlayerMoveDuration, ChaseMoveDuration,
            ScatterMoveDuration, ReturnMoveDuration, PowerDuration, RespawnDelay })
            if (duration <= TimeSpan.Zero || duration.Ticks % SimulationStep.Ticks != 0)
                throw new ArgumentException("Gameplay durations must be positive multiples of the simulation step.");
        if (StartingLives <= 0 || CatPoints < 0)
            throw new ArgumentException("Lives must be positive and cat points cannot be negative.");
    }
}

public enum RoundStatus { Playing, LifeLost, Won, GameOver }
public enum CatKind { Blinky, Pinky, Inky, Clyde }
public enum CatState { Waiting, Chase, Scatter, ReturningHome, Recovering }

public static class Directions
{
    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Left => Direction.Right,
        Direction.Down => Direction.Up,
        Direction.Right => Direction.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };
}
