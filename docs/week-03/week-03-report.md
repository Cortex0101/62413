# Week 3 Report: MVVM Shell, Navigation, Resources, and Canvas

## 1. Week objective

The Week 3 goal was to replace the prototype-only window with a maintainable WPF presentation architecture. The planned scope was a main-window shell, MVVM foundations, menu/navigation, shared visual resources, and initial retained-mode Canvas rendering. The existing keyboard movement prototype was retained as early functionality while remaining outside the Week 3 gameplay-loop scope.

## 2. Requirements and completion

| Requirement | Implementation | Status |
| --- | --- | --- |
| Host shell | `MainWindow` contains a `ContentControl` bound to `ActiveScreen`. | Complete |
| Explicit composition | `App.xaml.cs` constructs `MainViewModel` and `MainWindow`. | Complete |
| MVVM foundations | Existing `ObservableObject` and `RelayCommand` are used by all screen models. | Complete |
| Main menu | Start, High Scores, Settings, About, and Exit commands are exposed. | Complete |
| Navigation | `MainViewModel` swaps typed screen view models. | Complete |
| Placeholder screens | High Scores, Settings, and About establish future navigation paths. | Complete |
| Dedicated views | Menu, game, and placeholder user controls are selected by DataTemplates. | Complete |
| Canvas prototype | A 10-by-14 board and player are rendered in retained-mode WPF. | Complete |
| Shared resources | Theme and control dictionaries centralize brushes, dimensions, and styles. | Complete |
| Keyboard behavior | Arrow/WASD bindings are scoped to the game; Escape returns to the menu. | Complete |
| Automated evidence | Navigation, projection, notification, boundary, and engine tests pass. | Complete |
| Visual evidence | Repeatable checklist and screenshot list supplied. | Awaiting hands-on capture |

## 3. Engineering log

### Starting point

The solution already had separate application, engine, and test projects. `MainWindow` directly instantiated `MainViewModel`, contained the complete board UI, and owned global movement key bindings. `MainViewModel` mixed shell, board projection, and movement responsibilities.

### Work performed

1. Removed `StartupUri` and established `App.xaml.cs` as the composition root.
2. Reduced `MainWindow` to a host bound to `MainViewModel.ActiveScreen`.
3. Added `ScreenViewModel`, `MainMenuViewModel`, and `PlaceholderViewModel`.
4. Extracted the existing board projection and movement command into `GameViewModel`.
5. Added typed WPF views and implicit DataTemplates in `App.xaml`.
6. Rebuilt the board as a layered Canvas inside `GameView`.
7. Added `Theme.xaml` and `Controls.xaml` resource dictionaries.
8. Scoped movement input and focus restoration to the game view.
9. Added application view-model tests and retained all engine tests.
10. Built the solution, ran 30 tests, and launched the application for a startup smoke test.

### Defect encountered

Visual Studio reported `XLS0414` for `MainMenuView` while invoking one workspace build action. Full compiler validation with `dotnet build` and `dotnet test` successfully compiled the same XAML and code-behind. The evidence indicates a transient XAML designer/design-time diagnostic rather than a source compilation error. Reloading the designer or rebuilding the application project should refresh its generated-type cache if the diagnostic remains visible.

Nullable warnings in `MainViewModel` were also found because menu lambdas captured placeholder fields before assignment. Initialization was reordered so destination fields are assigned before command callbacks capture them.

## 4. Design decisions and alternatives

### View-model-first navigation

**Decision:** Keep one `ActiveScreen` object in `MainViewModel` and use implicit DataTemplates to resolve views.

**Why:** It keeps navigation testable without creating WPF controls in tests, leaves `MainWindow` stable, and supports later screen additions.

**Alternative rejected:** Replace the window content directly from code-behind. This would couple navigation to WPF and weaken MVVM testability.

### Explicit composition without a DI package

**Decision:** Construct the root object graph in `App.xaml.cs`.

**Why:** The current graph is small, dependencies remain visible, and no package is needed merely to demonstrate dependency injection.

**Alternative deferred:** A dependency-injection container may be reconsidered only if persistence, audio, and other services make manual composition unwieldy.

### One reusable placeholder view

**Decision:** Use one `PlaceholderViewModel`/`PlaceholderView` pair for unfinished destinations.

**Why:** The screens currently share behavior and differ only by content. Dedicated high-score and settings models will replace placeholders when their requirements exist.

**Alternative rejected:** Empty dedicated classes for every future screen would add ceremony without behavior.

### Input owned by the game view

**Decision:** Put movement key bindings on `GameView` and restore focus in minimal code-behind.

**Why:** Input cannot move the player from menu screens, while focus remains a presentation concern. The command itself stays testable.

**Alternative rejected:** Window-level key bindings remain active regardless of current screen and can accidentally mutate hidden game state.

### Pixel projection in the application layer

**Decision:** Keep `GridPosition` in the engine and expose pixel values from `GameViewModel`.

**Why:** The engine remains independent of DPI, WPF controls, and visual dimensions.

**Alternative rejected:** Deriving domain movement from `Canvas.Left`/`Canvas.Top` would make rules presentation-dependent.

### Retained-mode Canvas

**Decision:** Use an `ItemsControl` with a Canvas panel for board cells plus a separate actor layer.

**Why:** This matches WPF’s retained-mode model and provides a clear route to later collectible and actor layers.

**Alternative deferred:** Immediate-mode drawing may improve very large-scene performance, but this board does not justify the added complexity.

## 5. Representative implementation references

- `src/RatMan.App/App.xaml.cs` — explicit startup composition.
- `src/RatMan.App/MainWindow.xaml` — shell-only `ContentControl`.
- `src/RatMan.App/ViewModels/MainViewModel.cs` — active-screen coordination.
- `src/RatMan.App/ViewModels/MainMenuViewModel.cs` — navigation commands.
- `src/RatMan.App/ViewModels/GameViewModel.cs` — engine-to-Canvas projection.
- `src/RatMan.App/Views/GameView.xaml` — retained-mode board and game input.
- `src/RatMan.App/Resources/Theme.xaml` — palette and dimensions.
- `src/RatMan.App/Resources/Controls.xaml` — reusable WPF styles.
- `tests/RatMan.Tests/AppViewModelTests.cs` — application behavior evidence.

## 6. Test and build evidence

Commands executed:

```powershell
dotnet build .\62413.slnx --no-restore
dotnet test .\tests\RatMan.Tests\RatMan.Tests.csproj --no-restore
dotnet run --project .\src\RatMan.App\RatMan.App.csproj --no-build
```

Results:

- Solution build succeeded.
- Test summary: 30 total, 30 succeeded, 0 failed, 0 skipped.
- The application stayed running during the startup smoke test and was then stopped intentionally.

The tests avoid fragile WPF pixel assertions. They exercise view models and engine behavior directly, while visual appearance and focus remain on the manual checklist.

## 7. Known limitations and risks

- The board has boundaries but no maze walls; wall-aware movement belongs to Week 4.
- Movement is one tile per key command; no fixed-step game loop exists yet.
- There are no collectibles, cats, score, lives, or transient gameplay states yet.
- High Scores and Settings are placeholders and do not persist data.
- Canvas dimensions are fixed; later presentation work should introduce scaling suitable for different window sizes.
- Screenshots require a human-run visual session and are listed as pending in the acceptance checklist.
- WPF focus behavior should be rechecked after future overlays or controls are added.

## 8. Retrospective

### What went well

- The existing domain separation allowed the UI to be reorganized without changing engine rules.
- View-model-first navigation made screen flow straightforward to unit test.
- Moving key bindings into `GameView` removed hidden input behavior from non-game screens.
- Resource dictionaries established a consistent baseline before visual complexity increases.

### What could improve

- Application tests were introduced only after the navigation structure existed; writing them alongside each view model would shorten feedback loops.
- Screenshot evidence cannot be produced reliably through a non-interactive build run and should be captured during the weekly review.
- The fixed board size is adequate for the prototype but resizing behavior needs an explicit design before visual polish.

## 9. Week 4 handoff

Week 4 should build on `GameViewModel` and `GameView` without moving domain behavior into WPF. Planned work:

1. Introduce a fixed-step update boundary driven by a WPF timer or rendering callback.
2. Adapt key input into domain game actions rather than moving immediately per key press.
3. Add a maze representation with walls and passability checks.
4. Implement tile-center-aware mouse movement and rejected turns.
5. Add deterministic movement and wall-collision tests.
6. Preserve the Week 3 navigation shell and keep the engine free of WPF clocks and dispatchers.

## 10. Evidence still to attach

Use [acceptance-checklist.md](acceptance-checklist.md) during the weekly review, then add the five requested screenshots under `docs/week-03/screenshots/`. Do not mark visual checks complete without performing them.
