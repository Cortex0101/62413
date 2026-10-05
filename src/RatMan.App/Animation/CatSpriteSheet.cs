using RatMan.Game;

namespace RatMan.App.Animation;

// Measured alpha bounds: the supplied rows are not a regular 4x4 grid.
public static class CatSpriteSheet
{
    private static readonly Dictionary<string, Lazy<(SpriteSheetDefinition Sheet, SpriteFrameProvider Frames)>> Cache =
        Names.ToDictionary(name => name, name => new Lazy<(SpriteSheetDefinition, SpriteFrameProvider)>(() =>
        {
            var sheet = Create(name);
            return (sheet, new SpriteFrameProvider(sheet, canvasSize: 336));
        }));

    public static IEnumerable<string> Names => new[] { "red", "cyan", "pink", "orange" }
        .SelectMany(color => new[] { $"{color}-cat-spritesheet", $"{color}-cat-scared-spritesheet" })
        .Append("ratman-steroid-spritesheet");

    public static string Name(CatKind kind, bool scared)
    {
        string color = kind switch
        {
            CatKind.Blinky => "red", CatKind.Pinky => "cyan", CatKind.Inky => "pink",
            CatKind.Clyde => "orange", _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return $"{color}-cat{(scared ? "-scared" : "")}-spritesheet";
    }

    public static SpriteAnimator CreateAnimator(CatKind kind, bool scared) => CreateAnimator(Name(kind, scared));

    public static SpriteAnimator CreateAnimator(string name)
    {
        var (sheet, frames) = Cache[name].Value;
        return new(sheet, frames, "idle-down");
    }

    public static SpriteSheetDefinition Create(string name)
    {
        SpriteRectangle[][] rows = name switch
        {
            "red-cat-spritesheet" =>
            [Row(13, 317, (64, 271), (392, 583), (692, 894), (1005, 1202)),
             Row(333, 592, (15, 321), (324, 626), (637, 942), (942, 1247)),
             Row(629, 880, (12, 316), (324, 627), (638, 943), (948, 1250)),
             Row(911, 1223, (38, 270), (353, 581), (658, 888), (970, 1195))],
            "red-cat-scared-spritesheet" =>
            [Row(83, 349, (58, 267), (371, 578), (680, 894), (997, 1203)),
             Row(411, 605, (12, 312), (313, 622), (623, 939), (940, 1247)),
             Row(662, 851, (8, 315), (317, 626), (627, 936), (937, 1245)),
             Row(904, 1195, (67, 267), (377, 574), (688, 889), (1005, 1203))],
            "cyan-cat-spritesheet" =>
            [Row(84, 382, (78, 261), (391, 583), (705, 901), (1019, 1214)),
             Row(428, 619, (20, 322), (333, 627), (639, 952), (960, 1246)),
             Row(677, 868, (14, 321), (324, 625), (634, 939), (946, 1237)),
             Row(897, 1206, (87, 257), (399, 567), (707, 874), (1022, 1193))],
            "cyan-cat-scared-spritesheet" =>
            [Row(91, 388, (60, 272), (379, 591), (690, 904), (1009, 1219)),
             Row(456, 627, (17, 320), (325, 629), (639, 942), (948, 1244)),
             Row(699, 868, (9, 314), (319, 619), (625, 938), (943, 1246)),
             Row(904, 1207, (79, 258), (389, 576), (702, 885), (1018, 1200))],
            "pink-cat-spritesheet" =>
            [Row(57, 363, (44, 262), (361, 583), (673, 892), (994, 1208)),
             Row(407, 625, (21, 322), (328, 640), (641, 946), (958, 1254)),
             Row(668, 884, (13, 311), (313, 625), (628, 934), (935, 1238)),
             Row(907, 1205, (49, 264), (366, 571), (676, 891), (995, 1211))],
            "pink-cat-scared-spritesheet" =>
            [Row(89, 379, (42, 282), (366, 597), (668, 903), (991, 1216)),
             Row(410, 620, (17, 324), (327, 637), (637, 947), (956, 1250)),
             Row(673, 874, (17, 312), (317, 623), (628, 939), (947, 1245)),
             Row(909, 1206, (60, 259), (371, 569), (671, 880), (996, 1195))],
            "orange-cat-spritesheet" =>
            [Row(60, 386, (66, 276), (385, 599), (691, 901), (1012, 1226)),
             Row(449, 638, (21, 310), (325, 620), (639, 926), (946, 1242)),
             Row(708, 886, (24, 316), (333, 622), (645, 935), (952, 1247)),
             Row(928, 1209, (47, 256), (384, 579), (672, 878), (997, 1205))],
            "orange-cat-scared-spritesheet" =>
            [Row(59, 383, (51, 280), (375, 606), (680, 902), (1000, 1225)),
             Row(448, 643, (21, 312), (335, 626), (641, 935), (953, 1249)),
             Row(706, 891, (19, 316), (335, 622), (643, 935), (950, 1242)),
             Row(921, 1203, (52, 273), (384, 593), (684, 887), (1002, 1221))],
            "ratman-steroid-spritesheet" =>
            [Row(38, 317, (21, 328), (347, 630), (645, 935), (962, 1239)),
             Row(322, 603, (21, 327), (328, 630), (639, 938), (949, 1244)),
             Row(624, 904, (17, 304), (315, 613), (630, 921), (942, 1230)),
             Row(908, 1218, (25, 303), (341, 616), (645, 917), (951, 1232))],
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        Direction[] directions = [Direction.Down, Direction.Left, Direction.Right, Direction.Up];
        List<SpriteAnimationClip> clips = [];
        for (int row = 0; row < rows.Length; row++)
        {
            string direction = directions[row].ToString().ToLowerInvariant();
            clips.Add(new($"idle-{direction}", [new(rows[row][0], TimeSpan.FromSeconds(1))], true));
            clips.Add(new($"walking-{direction}", rows[row].Select(rect => new SpriteFrameDefinition(rect, TimeSpan.FromMilliseconds(95))), true));
            clips.Add(new($"blocked-{direction}", new[] { 0, 1, 0 }.Select(i =>
                new SpriteFrameDefinition(rows[row][i], TimeSpan.FromMilliseconds(70))), false));
        }
        return new(new Uri($"/RatMan;component/Assets/Images/{name}.png", UriKind.Relative), clips);
    }

    private static SpriteRectangle[] Row(int top, int bottom, params (int Left, int Right)[] columns) =>
        columns.Select(c => new SpriteRectangle(c.Left, top, c.Right - c.Left, bottom - top)).ToArray();
}

