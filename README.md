# RatMan

RatMan is a .NET 10 WPF maze-chase course project. The solution separates the deterministic game domain from WPF presentation code and uses MVVM for screen navigation, commands, and Canvas projection.

## Current scope

The current game includes:

- A main-window shell with view-model-first navigation.
- Main menu commands for play, board building, high scores, settings, about, and exit.
- A retained-mode Canvas showing the selected maze, animated Ratman, and four animated cats.
- A board builder with editable dimensions, boundary walls, and player/monster spawns.
- Versioned `.maze` files and map selection when multiple saved boards are available.
- Two configurable keyboard bindings per movement direction, with WASD and arrow keys as defaults.
- Shared WPF colors, brushes, dimensions, typography, and control styles.
- Automated engine and application view-model tests.
- Normal and animated steroid cheese, editor food tools, and a live per-game score.
- Distinct cat targets, cheese-based releases, steroid scatter, edible cats, and harmless return-home movement.
- Three lives, a power timer, cat status indicators, victory/Game Over overlays, and replay.
- Persistent top-10 high scores for each map layout, with optional player names.

Game rules run in the WPF-independent engine using a deterministic 10 ms simulation step.

## Prerequisites

- Windows capable of running WPF.
- Visual Studio 2026 with the .NET desktop development workload, or the .NET 10 SDK.

## Solution structure

```text
src/
  RatMan.App/      WPF shell, views, view models, commands, and resources
  RatMan.Game/     WPF-independent domain model and game rules
tests/
  RatMan.Tests/    MSTest engine and application view-model tests
docs/
  week-03/         Week 3 report, architecture, and acceptance evidence
```

`RatMan.App` references `RatMan.Game`. The game project has no dependency on WPF or the application project.

## Build and test

Open `62413.slnx` in Visual Studio and use **Build Solution** and **Test Explorer**, or run these commands from the repository root:

```powershell
dotnet restore .\62413.slnx
dotnet build .\62413.slnx --no-restore
dotnet test .\tests\RatMan.Tests\RatMan.Tests.csproj --no-restore
```

## Run

Set `RatMan.App` as the startup project in Visual Studio, or run:

```powershell
dotnet run --project .\src\RatMan.App\RatMan.App.csproj
```

## Controls

| Input | Action |
| --- | --- |
| Arrow keys (default) | Start moving or request a turn. |
| W, A, S, D (default) | Start moving or request a turn. |
| Escape | Return from the game to the main menu. |

Open **Settings** and select either binding beside a movement action, then press the new key. Press Escape to cancel. Selecting a key already in use swaps the two bindings. Changes are saved automatically in `%LOCALAPPDATA%\RatMan\settings.json`.

The game view restores keyboard focus when opened. The player keeps moving until a wall or board edge blocks the current direction. A blocked turn waits until the first opening without changing the player's facing direction or interrupting movement. The latest direction input replaces any pending turn.

## Food and score

Every tile reachable from the player starts with normal cheese, except the player's starting tile and tiles cleared in the editor. Normal cheese randomly uses one of four appearances, stable throughout that game, and awards **1 point**. Animated steroid cheese replaces normal cheese and awards **5 points**, powers up Ratman, and sends active cats into scatter for **10 seconds**. Another steroid refreshes that timer.

Food disappears and the score increases when the mouse finishes entering the tile, including after a tunnel wrap. Each cheese can be collected only once. Eating a scared cat awards **20 points**. Collecting all cheese wins the round. New games and Replay restore the map's food and reset the current score to **0**; qualifying final scores can be saved to the leaderboard.

## High scores

After victory or Game Over, a score that reaches its layout's **top 10** opens a name-entry dialog. Enter an optional name (up to 24 characters), then choose **Save Score** or press Enter. A blank name saves as **Anonymous**. **Skip**, Escape, or closing the dialog discards the entry. Leaving an unfinished game does not submit a score. Each replay is a separate run.

Scores rank highest first, with previously saved entries winning ties. Matching the tenth-place score does not qualify; zero points can qualify while the board has fewer than ten entries. Multiple runs can use the same name.

Open **High Scores** from the main menu or the finished-game screen. The layout selector includes current maps and historical layouts with saved scores, with dimensions and last-played dates to help distinguish them. The most recently played layout is selected in the current app session; after restarting, the layout with the latest saved completion is selected.

Leaderboards are identified by dimensions, spawn positions, walls, tunnels, and effective starting food. Renaming or copying a map keeps its scores, while changing its layout creates a separate leaderboard. Restoring an earlier layout reconnects its scores. Cosmetic cheese appearances and hidden food overrides do not affect identity.

Scores live in `%LOCALAPPDATA%\RatMan\highscores.json`, a versioned JSON file shared across builds. Saves are asynchronous and replace the file atomically. Failed reads leave existing files unchanged and offer Refresh or Retry High Score. Failed saves keep the dialog and entered name available for another attempt or Skip. Unsupported or damaged files are reported without being overwritten; repair or move the affected file, then retry. Existing `.maze` files require no migration.

## Cats and lives

All cats leave the existing monster spawn. Blinky (red) releases immediately; Pinky (cyan), Inky (pink), and Clyde (orange) release after **10%, 25%, and 50%** of the initial cheese count, rounded upward. Steroid cheese counts as one piece for release progress. Each cat uses its own chase target and scatter corner, with shortest-path navigation around walls and through tunnels. The names and colors follow this project's [cat specification](src/RatMan.App/docs/cats.md).

During steroid power, cats head toward their corners, show scared sprites, and become edible. Eaten cats return home at double their normal speed, harmless and faded, then wait for the active power effect to end. Waiting and recovering cats appear in the sidebar.

Ratman starts with **three lives**. A chasing cat's touch costs one life and resets actors after a one-second pause, retaining score, eaten cheese, and release eligibility. The third catch ends the game. Victory and Game Over stop gameplay and offer Replay of the same map or Return to Menu. Disconnected cat homes produce a warning while preserving the map's walls and tunnels.

## Board Builder

Open **Board Builder** from the main menu. The board is on the left; naming, dimensions, tools, and file actions are on the right. New boards start at 12 columns by 11 rows. Width and height accept whole numbers from 1 through 100; commit a change with Enter or by leaving the field. Shrinking asks for confirmation if walls, spawn points, or food overrides would be removed. Growing adds normal cheese to new reachable tiles.

| Input | Builder action |
| --- | --- |
| 1 / 2 / 3 (including numpad) | Select wall / player spawn / monster spawn. |
| 4 / 5 (including numpad) | Select normal cheese / steroid cheese. |
| R | Rotate the wall tool between horizontal and vertical. |
| Left-click | Place the selected wall, spawn, or cheese. Normal cheese restores cleared food or replaces steroid cheese. |
| Right-click with a cheese tool | Clear food in the cell without changing walls or spawns. |
| Right-click with a wall/spawn tool | Remove a nearby wall or the spawn in a cell. |
| Escape | Return to the menu, checking unsaved changes. |

Selecting a spawn and clicking another cell moves its marker. The preview follows the pointer, including animated steroid cheese. Keyboard shortcuts do not interfere with typing in the name or dimension fields. Removing an outer boundary opens the matching opposite boundary for tunnel wrapping. Saving requires a name and exactly one **P** (player) and **M** (monster) on different cells; disconnected areas are allowed.

Before placing P, food previews throughout the board. Afterwards, unreachable areas fill as walls and hide their food. The player starting tile is always empty; reachable monster spawn tiles can contain food. Wall, tunnel, and player-spawn changes preserve hidden food overrides, restoring them when the tiles become eligible again. Explicitly cleared tiles stay empty until restored with a cheese tool. Food placement on the player start or an unreachable tile is disabled.

**Save** keeps the editor open. **Load Board** lists existing boards for editing. **New Board**, loading another board, and returning to the menu offer Save/Discard/Cancel when changes are pending. Changing a loaded board's display name updates the same file; use New Board to create a separate map.

## Saved maps

Maps live in `%LOCALAPPDATA%\RatMan\Mazes`, shared across builds. On first use, the app copies the bundled `Classic.maze` there without overwriting an existing file. The `.initialized` marker prevents later launches from restoring a deliberately deleted Classic map. New files use sanitized board names and ask before replacing an existing file. Saves replace files atomically.

**Start Game** starts a fresh session directly when there is one valid map, shows a clickable list when there are multiple, and offers the builder when none are valid. Invalid files are reported without preventing valid boards from being played. Copy compatible `.maze` files into this folder to add them to the library; the catalog refreshes whenever opening Start Game or Load Board.

### File format

New saves contain UTF-8 JSON, **version 2**. Version 1 maps still load with default food and are upgraded only when saved. Width counts columns and height counts rows. Coordinates are zero-based. Each wall separates two orthogonally adjacent cells; coordinates describe cells, not grid intersections. Both spawn fields are required.

`emptyFoodTiles` and `steroidCheeseTiles` store food overrides; omitted arrays default to empty. Coordinates must be inside the board and cannot occur in both arrays. Normal cheese is implicit on other reachable tiles except the player start. Hidden overrides remain stored. Scores, consumed food, and cosmetic variants are never saved. `openBorderRows` and `openBorderColumns` optionally enable paired tunnel openings. For example:

```json
{
  "version": 2,
  "name": "Cheese corridor",
  "width": 4,
  "height": 1,
  "playerSpawn": { "row": 0, "column": 0 },
  "monsterSpawn": { "row": 0, "column": 3 },
  "walls": [],
  "openBorderRows": [],
  "openBorderColumns": [],
  "emptyFoodTiles": [{ "row": 0, "column": 1 }],
  "steroidCheeseTiles": [{ "row": 0, "column": 2 }]
}
```

## Documentation

- [Project roadmap](plan.md)
- [Week 3 deliverables](docs/week-03/README.md)
- [Week 3 report](docs/week-03/week-03-report.md)
- [Week 3 architecture](docs/week-03/architecture.md)
- [Week 3 acceptance checklist](docs/week-03/acceptance-checklist.md)
