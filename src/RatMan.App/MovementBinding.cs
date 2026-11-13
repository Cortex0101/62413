using System.Windows.Input;
using RatMan.Game;

namespace RatMan.App;

public sealed record MovementBinding(Direction Direction, Key FirstKey, Key SecondKey)
{
    public static IReadOnlyList<MovementBinding> Defaults { get; } = Array.AsReadOnly<MovementBinding>(
    [
        new(Direction.Up, Key.W, Key.Up),
        new(Direction.Down, Key.S, Key.Down),
        new(Direction.Left, Key.A, Key.Left),
        new(Direction.Right, Key.D, Key.Right)
    ]);

    public string Description => $"Move {Direction.ToString().ToLowerInvariant()}";
    public string FirstKeyLabel => KeyLabel(FirstKey);
    public string SecondKeyLabel => KeyLabel(SecondKey);

    internal static bool CanBind(Key key) => Enum.IsDefined(key) && key is not (
        Key.None or Key.Escape or Key.Tab or Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System or Key.ImeProcessed
        or Key.DeadCharProcessed);

    private static string KeyLabel(Key key) => key switch
    {
        Key.Up => "Up Arrow",
        Key.Down => "Down Arrow",
        Key.Left => "Left Arrow",
        Key.Right => "Right Arrow",
        Key.Return => "Enter",
        >= Key.D0 and <= Key.D9 => (key - Key.D0).ToString(),
        _ => key.ToString()
    };
}
