# Week 3 Acceptance Checklist

## Automated evidence

Run from the repository root:

```powershell
dotnet build .\62413.slnx --no-restore
dotnet test .\tests\RatMan.Tests\RatMan.Tests.csproj --no-restore
```

Recorded Week 3 result:

- Full solution build succeeded.
- 30 tests succeeded in the combined engine/application test project.
- No tests failed or were skipped.
- `dotnet run --project .\src\RatMan.App\RatMan.App.csproj --no-build` remained running during the startup smoke test.

Automated coverage includes:

- Initial main-menu selection.
- Menu-to-placeholder navigation and return navigation.
- Menu-to-game navigation.
- Board cell count and pixel dimensions.
- Initial projected player position.
- Movement projection notifications.
- Rejected movement at a board boundary.
- Existing value-object and engine movement behavior.

## Hands-on Visual Studio checklist

Run `RatMan.App`, then record initials/date and result for each item.

| Scenario | Expected result | Result |
| --- | --- | --- |
| Startup | Main menu is the first screen and all five buttons are visible. | Pending hands-on check |
| Start Game | The game screen shows a 10-by-14 Canvas board and yellow player. | Pending hands-on check |
| Arrow keys | Each arrow key moves the player by one cell. | Pending hands-on check |
| WASD | W, A, S, and D match arrow-key movement. | Pending hands-on check |
| Boundaries | Repeated movement stops at every board edge. | Pending hands-on check |
| Escape | Escape returns from the game to the main menu. | Pending hands-on check |
| Focus | Movement works immediately after entering the game without clicking the board. | Pending hands-on check |
| Return button | Return to Menu navigates from the game to the main menu. | Pending hands-on check |
| High Scores | Opens the Week 10 placeholder and returns successfully. | Pending hands-on check |
| Settings | Opens the future-settings placeholder and returns successfully. | Pending hands-on check |
| About | Opens project information and returns successfully. | Pending hands-on check |
| Resize | Content remains usable at the minimum window size and after enlargement. | Pending hands-on check |
| Exit | Exit closes the application cleanly. | Pending hands-on check |

## Screenshot evidence

Capture these images after completing the hands-on checklist and place them in `docs/week-03/screenshots/`:

1. `01-main-menu.png` — initial menu.
2. `02-game-canvas.png` — full Canvas and player.
3. `03-player-moved.png` — player at a different grid position.
4. `04-placeholder.png` — one future-feature placeholder.
5. `05-test-results.png` — Test Explorer or terminal showing 30 passing tests.

Screenshots are intentionally listed as pending rather than fabricated by automated tooling.
