# RatMan

RatMan is a .NET 10 WPF maze-chase course project. The solution separates the deterministic game domain from WPF presentation code and uses MVVM for screen navigation, commands, and Canvas projection.

## Current scope

The Week 3 milestone includes:

- A main-window shell with view-model-first navigation.
- Main menu commands for play, board building, high scores, settings, about, and exit.
- Placeholder screens for features scheduled in later weeks.
- A retained-mode Canvas showing the selected maze, player, and static monster spawn marker.
- A board builder with editable dimensions, boundary walls, and player/monster spawns.
- Versioned `.maze` files and map selection when multiple saved boards are available.
- Arrow-key and WASD movement with board-boundary enforcement.
- Shared WPF colors, brushes, dimensions, typography, and control styles.
- Automated engine and application view-model tests.

Collectibles, scoring, moving cats, and additional gameplay states are scheduled for later milestones. Monster markers currently indicate starting positions only.

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
| Arrow keys | Start moving or request a turn. |
| W, A, S, D | Start moving or request a turn. |
| Escape | Return from the game to the main menu. |

The game view restores keyboard focus when opened. The player keeps moving until a wall or board edge blocks the current direction. A blocked turn waits until the first opening without changing the player's facing direction or interrupting movement. The latest direction input replaces any pending turn.

## Board Builder

Open **Board Builder** from the main menu. The board is on the left; naming, dimensions, tools, and file actions are on the right. New boards start at 12 columns by 11 rows. Width and height accept whole numbers from 1 through 100; commit a change with Enter or by leaving the field. Shrinking asks for confirmation if walls or spawn points would be removed.

| Input | Builder action |
| --- | --- |
| 1 / 2 / 3 (including numpad) | Select wall / player spawn / monster spawn. |
| R | Rotate the wall tool between horizontal and vertical. |
| Left-click | Place a wall near an internal cell boundary, or a spawn inside a cell. |
| Right-click | Remove a nearby wall or the spawn in a cell. |
| Escape | Return to the menu, checking unsaved changes. |

Selecting a spawn and clicking another cell moves its marker. The preview follows the pointer. Keyboard shortcuts do not interfere with typing in the name or dimension fields. Outer boundaries are fixed. Saving requires a name and exactly one **P** (player) and **M** (monster) on different cells; disconnected areas are allowed.

**Save** keeps the editor open. **Load Board** lists existing boards for editing. **New Board**, loading another board, and returning to the menu offer Save/Discard/Cancel when changes are pending. Changing a loaded board's display name updates the same file; use New Board to create a separate map.

## Saved maps

Maps live in `%LOCALAPPDATA%\RatMan\Mazes`, shared across builds. On first use, the app copies the bundled `Classic.maze` there without overwriting an existing file. The `.initialized` marker prevents later launches from restoring a deliberately deleted Classic map. New files use sanitized board names and ask before replacing an existing file. Saves replace files atomically.

**Start Game** starts a fresh session directly when there is one valid map, shows a clickable list when there are multiple, and offers the builder when none are valid. Invalid files are reported without preventing valid boards from being played. Copy compatible `.maze` files into this folder to add them to the library; the catalog refreshes whenever opening Start Game or Load Board.

### File format

`.maze` files contain UTF-8 JSON, version 1. Width counts columns and height counts rows. Coordinates are zero-based. Each wall separates two orthogonally adjacent cells; coordinates describe cells, not grid intersections. Both spawn fields are required. For example, this valid map intentionally has no route between its two cells:

```json
{
  "version": 1,
  "name": "Two rooms",
  "width": 2,
  "height": 1,
  "playerSpawn": { "row": 0, "column": 0 },
  "monsterSpawn": { "row": 0, "column": 1 },
  "walls": [
    {
      "firstTile": { "row": 0, "column": 0 },
      "secondTile": { "row": 0, "column": 1 }
    }
  ]
}
```

## Documentation

- [Project roadmap](plan.md)
- [Week 3 deliverables](docs/week-03/README.md)
- [Week 3 report](docs/week-03/week-03-report.md)
- [Week 3 architecture](docs/week-03/architecture.md)
- [Week 3 acceptance checklist](docs/week-03/acceptance-checklist.md)
