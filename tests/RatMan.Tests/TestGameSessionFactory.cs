using RatMan.Game;

namespace RatMan.Tests;

internal static class TestGameSessionFactory
{
    public static GameSession Create(
        int rows = 5,
        int columns = 7,
        int playerRow = 2,
        int playerColumn = 3) =>
        new(new BoardSize(rows, columns), new GridPosition(playerRow, playerColumn));
}
