# 🎯 Mouse-and-Cats WPF Course Project Architecture and Roadmap

Design a balanced, polished single-level maze-chase game that demonstrates advanced object-oriented C# and .NET knowledge without turning language features into artificial gimmicks. Retain WPF as the presentation technology, use MVVM for screens and UI behavior, isolate a deterministic domain/game-engine layer from WPF, render the board and actors on a retained-mode `Canvas`, persist settings and high scores as JSON, and place core engine behavior under automated tests.

The current solution is a minimal .NET 10 WPF project: `62413.csproj` enables WPF, nullable references, and implicit usings; `MainWindow.xaml` is still the template-like view with one button; and `MainWindow.xaml.cs` contains only a checked-overflow experiment. No engine, MVVM, persistence, or test infrastructure exists yet. Replace the experiment as the application architecture is introduced, while retaining checked arithmetic only where it has a genuine domain use.

Suggested solution structure:

- `_62413` (WPF application): views, view models, converters, commands, WPF rendering, composition root, and resources.
- `_62413.Game` (class library): game model, rules, state machine, movement, collision, cat AI, events, and abstractions; no dependency on WPF.
- `_62413.Tests` (test project): unit tests for the engine, state transitions, scoring, collision, AI, and persistence-facing contracts where appropriate.

Core domain model:

- `GameSession`: aggregate/root object coordinating the current level, actors, score, lives, elapsed time, update ticks, win/loss rules, and emitted domain events.
- `Maze` and immutable `Tile`/`TileType`: board dimensions, walls, corridors, cheese, power-ups, spawn points, and tunnel/portal information. Prefer value semantics through `readonly record struct GridPosition(int Row, int Column)` and direction vectors.
- `Entity` abstract base class: stable ID, position, direction, speed, and common movement behavior. Derive `Mouse` and abstract `Cat`; keep inheritance shallow and favor composition for interchangeable behavior.
- Cat variants such as `ChaserCat`, `AmbusherCat`, `PatrolCat`, and `RandomCat`, differentiated through an `ICatMovementStrategy` implementation rather than duplicated subclasses where possible.
- `Collectible` hierarchy or discriminated enum-backed model for cheese and power items. Use polymorphism only when items have meaningfully different behavior.
- `Score`, `Lives`, and movement-related value objects where invariants justify them; demonstrate encapsulation, validation, equality, and operator use naturally rather than adding ceremonial wrappers.
- `GameRules`/`GameOptions`: immutable configuration for speeds, points, frightened duration, starting lives, and update interval.

State and behavior:

- A `GameStateMachine` owns `IGameState CurrentState` and explicit transitions among `MainMenuState`, `PlayingState`, `PausedState`, `RoundStartingState`, `LifeLostState`, `GameOverState`, and `HighScoresState`. Each state defines valid input/update behavior and optional `Enter`/`Exit` hooks.
- Keep application screen navigation synchronized with, but conceptually separate from, transient actor modes. Cats use a second small mode model (`Scatter`, `Chase`, `Frightened`, `ReturningHome`) so screen state does not become entangled with AI state.
- `GameLoop` uses a WPF-appropriate timer or rendering callback at the UI boundary, accumulates elapsed time, and advances the engine at a fixed timestep. The engine receives elapsed-time/update commands and remains testable without clocks or WPF dispatchers.
- `MovementSystem`, `CollisionSystem`, and `CollectibleSystem` implement focused rules. A tile-aware movement service validates turns near tile centers and prevents wall penetration.
- Cat AI uses pathfinding (breadth-first search is sufficient for an unweighted maze) behind `IPathfinder`, plus strategy objects that select target tiles. Seed or abstract randomness through `IRandomSource` for repeatable tests.
- Publish typed domain events such as `CheeseCollected`, `PowerModeStarted`, `MouseCaught`, `ScoreChanged`, and `GameEnded`. Use standard .NET events where immediate in-process notification is appropriate, and avoid a needlessly complex event bus.

MVVM and WPF presentation:

- `MainWindow` becomes a host shell with a `ContentControl`; DataTemplates map screen view models to `MainMenuView`, `GameView`, `PauseOverlay`, `GameOverView`, and `HighScoresView`.
- `MainViewModel` exposes the active screen. `GameViewModel` projects read-only engine state into bindable HUD properties and canvas actor/tile items. `MainMenuViewModel`, `HighScoresViewModel`, and `GameOverViewModel` handle their screens.
- A reusable `ObservableObject` implements `INotifyPropertyChanged`; `RelayCommand` and `AsyncRelayCommand` implement `ICommand`, can-execute rules, and exception-safe async execution. Commands cover start, pause/resume, return to menu, and score submission; keyboard movement can be adapted from WPF key input into domain `GameAction` values.
- `GameView.xaml` contains a `Canvas` with bound `ItemsControl` layers for static tiles, collectibles, and actors. DataTemplates/styles display each item, while `Canvas.Left`, `Canvas.Top`, and transforms project logical grid coordinates to pixels. Keep collision and game coordinates out of WPF visuals.
- Place colors, brushes, dimensions, typography, and actor templates in ResourceDictionaries. Add converters only for genuinely presentational transformations; prefer direct properties and DataTemplates over converter-heavy XAML.
- Use image/audio assets as content resources with clear attribution and licensing. A small `IAudioService` may play WPF media effects without leaking media types into the engine.

Persistence and infrastructure:

- Define `IHighScoreRepository` and `ISettingsRepository` in an application-facing abstraction area, with JSON implementations using `System.Text.Json` and async file APIs.
- Persist versioned DTOs rather than serializing domain objects directly. Store data under the user-local application-data directory, handle missing/corrupt files gracefully, and write atomically through a temporary file.
- Model `HighScoreEntry` with player name, score, date, and optional rules/version metadata; order and cap the scoreboard deterministically.
- Compose dependencies in `App.xaml.cs` using explicit constructor injection. A full dependency-injection package is optional and unnecessary at this scale unless introduced as a deliberate report topic.
- Add structured diagnostics through `Microsoft.Extensions.Logging` if useful; keep user-facing validation and recoverable persistence errors distinct from programmer exceptions.

C#/.NET topics suitable for the report and defense:

- Classes, abstract classes, interfaces, records/record structs, enums, accessibility, encapsulation, constructors, and invariants.
- Inheritance and runtime polymorphism for states/entities; composition and Strategy for cat behavior; State, Command, Repository, Observer/events, and MVVM patterns with explanations of tradeoffs.
- Generics in reusable repositories, commands, collections, or algorithms only where they improve type safety.
- Delegates, lambdas, standard events, custom `EventArgs` or immutable domain event records, and careful subscription lifetime management.
- LINQ for score ranking, projections, and queries—not per-frame code where allocations would be harmful.
- Nullable reference analysis, pattern matching, switch expressions, init-only/required members where suitable, exception handling, checked score arithmetic, and immutability.
- `async`/`await`, `Task`, cancellation for file I/O or optional background level loading; keep WPF UI access on the dispatcher and do not force asynchronous logic into the deterministic update loop.
- Collections selected by behavior: arrays/2D storage for map tiles, lists for actors, dictionaries for lookup, queues and hash sets for BFS.
- Resource management and `IDisposable` where timers, event subscriptions, streams, or media resources actually require lifecycle control.
- Reflection, unsafe code, custom threading, and metaprogramming should be omitted unless a real game requirement emerges; relevance and sound design are stronger evidence than feature counting.

Testing and quality:

- Make engine tests independent of WPF and wall-clock time. Test legal and illegal state transitions, movement around walls, tile collection exactly once, score/life updates, power-mode expiry, collision outcomes, tunnel traversal, pathfinding, each targeting strategy, and level-completion/game-over conditions.
- Use deterministic fake clocks/random sources or pass elapsed time explicitly. Add JSON repository tests using temporary directories and cases for missing, valid, and corrupt data.
- Keep a small number of presentation tests/manual acceptance scenarios for command enablement, input focus, pause behavior, resizing/scaling, and screen navigation; avoid fragile tests of WPF pixels.
- Enable warnings and consider treating warnings as errors after the scaffold stabilizes. Use XML documentation selectively for public abstractions and complex rules.

Thirteen-week delivery sequence:

1. Scope, user stories, rules, architecture diagram, Git workflow, and a playable-definition checklist.
2. Solution restructuring, engine and test projects, core value types, maze representation, and test fixtures.
3. Main window shell, MVVM foundations, menu/navigation, resources, and initial Canvas board rendering.
4. Fixed-step game loop, keyboard input adaptation, mouse movement, wall rules, and movement tests.
5. Cheese/power collectibles, scoring, lives/HUD binding, domain events, and related tests.
6. State machine implementation and menu/play/pause/life-lost/game-over/high-score transitions.
7. Cat entity modes, BFS pathfinding, one basic chasing strategy, and deterministic AI tests.
8. Remaining cat strategies and frightened/return-home behavior, balancing distinct behavior without duplicating logic.
9. Collision edge cases, round reset, win/loss flow, tunnel behavior, and complete-level acceptance testing.
10. JSON settings/high-score repositories, async I/O, validation, corrupt-file recovery, and repository tests.
11. Visual polish, animation, optional audio service, accessibility/readability, scaling, and input-focus fixes.
12. Refactoring, profiling, warning cleanup, documentation diagrams, test coverage review, and release packaging.
13. Final playtesting and balancing, report completion, demo script, architecture/tradeoff review, and oral-defense rehearsal.

Keep an engineering log each week with requirements, UML/architecture changes, design decisions and alternatives, representative code, tests, defects, screenshots, and retrospective notes. The final report should explain why the engine is WPF-independent, how fixed-step updates and state transitions work, why Strategy and State were selected, how MVVM binds the engine to the Canvas, how persistence failures are handled, and what test evidence supports correctness.

**Last Updated**: 2026-09-09 11:54:35

## 📝 Plan Steps
-  **Restructure the solution — add WPF, engine, and test projects with one-way references and establish naming, nullable, warning, and test conventions.**
-  **Define the domain vocabulary — implement positions, directions, tiles, maze data, rules, entities, and session invariants with focused tests.**
-  **Build MVVM foundations — replace the test button and checked-overflow code with the shell, observable base, commands, view models, views, and resource dictionaries.**
-  **Implement Canvas projection — bind layered tile, collectible, and actor presentation models to retained-mode WPF visuals while keeping domain coordinates independent.**
-  **Implement deterministic gameplay — add fixed-step updates, input commands, movement, collision, collection, scoring, lives, and typed events.**
-  **Implement hierarchical state behavior — add the game-screen state machine and separate cat mode transitions with explicit valid transitions and lifecycle hooks.**
-  **Implement cat intelligence — add pathfinding, target-selection strategies, seeded randomness, distinct cats, and deterministic tests.**
-  **Add navigation and HUD flows — connect menu, play, pause, life-lost, game-over, and high-score screens through commands and state projections.**
-  **Add JSON persistence — implement async, versioned, atomic high-score and settings storage with validation and recovery tests.**
-  **Polish presentation — introduce licensed assets, styles, animations, optional sound, scaling, accessibility, and robust keyboard focus.**
-  **Harden and document the application — test edge cases, profile update/render paths, remove leaks and warnings, document decisions, and package a repeatable build.**
-  **Maintain the weekly report evidence — capture design evolution, UML, test results, tradeoffs, screenshots, and reflection throughout the 13-week roadmap.**

