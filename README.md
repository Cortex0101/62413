# RatMan

RatMan is a .NET 10 WPF maze-chase course project. The solution separates the deterministic game domain from WPF presentation code and uses MVVM for screen navigation, commands, and Canvas projection.

## Current scope

The Week 3 milestone includes:

- A main-window shell with view-model-first navigation.
- Main menu commands for play, high scores, settings, about, and exit.
- Placeholder screens for features scheduled in later weeks.
- A retained-mode Canvas showing a 10-by-14 board and player.
- Arrow-key and WASD movement with board-boundary enforcement.
- Shared WPF colors, brushes, dimensions, typography, and control styles.
- Automated engine and application view-model tests.

Maze walls, fixed-step updates, collectibles, scoring, cats, gameplay states, and persistence are intentionally scheduled for later milestones.

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
| Arrow keys | Move the player one grid cell. |
| W, A, S, D | Move the player one grid cell. |
| Escape | Return from the game to the main menu. |

The game view restores keyboard focus when opened. Movement stops at the board edges.

## Documentation

- [Project roadmap](plan.md)
- [Week 3 deliverables](docs/week-03/README.md)
- [Week 3 report](docs/week-03/week-03-report.md)
- [Week 3 architecture](docs/week-03/architecture.md)
- [Week 3 acceptance checklist](docs/week-03/acceptance-checklist.md)
