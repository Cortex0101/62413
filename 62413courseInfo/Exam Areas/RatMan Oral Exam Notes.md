# 62413 Oral Exam Notes for RatMan

Short answers for the seven Autumn 2026 exam areas. [Examination areas](</C:/dev/projects/VS/62413/62413courseInfo/Exam Areas/Examination areas-E26.pdf>)

## 1. Object-oriented programming

**Theory.** A class defines the state and behavior of its objects. Encapsulation controls how that state changes. Inheritance lets a derived class share a base class’s members. An interface defines a contract, and polymorphism lets the same call use different implementations of that contract. [Lesson 3](C:/dev/projects/VS/62413/62413courseInfo/Lesson03/Lesson3_E26.pdf)

**In RatMan.** `GameSession` exposes `Score` and `Lives` with private setters so the game engine controls those changes. `GameViewModel` inherits `Title` and property-change behavior through `ScreenViewModel`. For cat behavior, the session calls `Target` through `ICatTargetStrategy`. Blinky targets RatMan’s current tile, while Pinky aims ahead using RatMan’s facing direction. [Session state](/C:/dev/projects/VS/62413/src/RatMan.Game/GameSession.cs:20) · [Screen base class](/C:/dev/projects/VS/62413/src/RatMan.App/ViewModels/ScreenViewModel.cs:5) · [Cat targeting strategies](/C:/dev/projects/VS/62413/src/RatMan.Game/Cat.cs:21)

A `static` member belongs to the type itself. For example, `HighScoreRules.Top` takes a sequence of scores and can be called directly on `HighScoreRules`. [Static score rules](/C:/dev/projects/VS/62413/src/RatMan.App/Persistence/HighScores.cs:34)

## 2. Exceptions, collections and generics

**Theory.** `throw` signals a failure, `catch` handles matching exceptions, and `finally` runs when control leaves the `try`/`catch`, making it useful for cleanup. Generics let a collection or method work with different types while preserving compile-time type checking. A queue gives first-in, first-out order, a set tracks unique items, and a dictionary maps keys to values. [Lesson 4](C:/dev/projects/VS/62413/62413courseInfo/Lesson04/Lesson4_F26.pdf)

**In RatMan.** `FileMazeRepository` wraps JSON parsing errors in `InvalidDataException`. The map listing records the error for that file and continues loading the others. For reachability, `BoardTopology` uses a `Queue<GridPosition>` to explore tiles in breadth-first order and a `HashSet<GridPosition>` to avoid revisiting them. `GameSession` uses a `Dictionary<GridPosition, FoodKind>` to look up the cheese on a tile. [Maze loading and error handling](/C:/dev/projects/VS/62413/src/RatMan.App/Persistence/FileMazeRepository.cs:45) · [Reachability search](/C:/dev/projects/VS/62413/src/RatMan.Game/BoardTopology.cs:30) · [Cheese lookup](/C:/dev/projects/VS/62413/src/RatMan.Game/GameSession.cs:115)

## 3. Delegates, events and advanced type construction

**Theory.** A delegate represents one or more methods with a matching signature. A lambda can supply a method body. With an event, listeners can subscribe and unsubscribe, and the publisher controls when it is raised. An extension method adds instance-call syntax for a static method. A record struct is a value type with compiler-generated value equality. A partial class can have its definition split across source files that the compiler combines. [Lesson 5](C:/dev/projects/VS/62413/62413courseInfo/Lesson05/lesson5_E26.pdf)

**In RatMan.** `RelayCommand` wraps an `Action<object?>` and an optional `Predicate<object?>` to implement WPF’s `ICommand`. `GameViewModel` raises `RoundCompleted`, and `MainViewModel` subscribes with a lambda that starts the high-score check. `Direction.Opposite()` is an extension method the cat navigator uses when deciding whether to reverse. `GridPosition` is a `readonly record struct`, so positions with the same row and column work as equal dictionary keys. [Command](/C:/dev/projects/VS/62413/src/RatMan.App/Mvvm/RelayCommand.cs:5) · [Round completion handler](/C:/dev/projects/VS/62413/src/RatMan.App/ViewModels/MainViewModel.cs:73) · [Direction extension](/C:/dev/projects/VS/62413/src/RatMan.Game/GameRules.cs:30) · [Grid position](/C:/dev/projects/VS/62413/src/RatMan.Game/GridPosition.cs:3)

`GameView` is a partial class. WPF generates the `InitializeComponent()` method from XAML, and the handwritten part calls it in the constructor. [Game view code-behind](/C:/dev/projects/VS/62413/src/RatMan.App/Views/GameView.xaml.cs:8)

## 4. WPF

**Theory.** WPF is a Windows desktop UI framework. XAML declares the view, bindings connect controls to properties on a data source, and commands connect input to actions. In MVVM, the model holds the application rules, the view displays the UI, and the view model exposes the state and commands the view needs. The view model can notify bindings through `INotifyPropertyChanged`. [Lesson 6](C:/dev/projects/VS/62413/62413courseInfo/Lesson06/lesson6_F26.pdf) · [Course reading on MVVM](<C:/dev/projects/VS/62413/62413courseInfo/Lesson08/MVVM Patterns-Microsoft article.pdf>)

**In RatMan.** The main window binds its `ContentControl` to `ActiveScreen`, and data templates select the matching view. When RatMan eats cheese, `GameSession` updates the score, `GameViewModel` copies it into its bindable `Score` property, and `PropertyChanged` lets the score `TextBlock` refresh. The game view also uses a `Canvas` to place walls and sprites at coordinates supplied by the view models. [Window binding](/C:/dev/projects/VS/62413/src/RatMan.App/MainWindow.xaml:12) · [View templates](/C:/dev/projects/VS/62413/src/RatMan.App/App.xaml:12) · [Score update](/C:/dev/projects/VS/62413/src/RatMan.App/ViewModels/GameViewModel.cs:146) · [Game view](/C:/dev/projects/VS/62413/src/RatMan.App/Views/GameView.xaml:22)

## 5. .NET assemblies, reflection, late binding and attributes

**Theory.** A managed assembly packages compiled intermediate language (IL), type metadata, and a manifest that identifies the assembly and its dependencies. It can also contain resources. Reflection inspects types and members at runtime. Late binding resolves a member at runtime, such as looking up a field by name or finding and invoking a method through reflection. Attributes attach metadata to assemblies, types or members for tools and frameworks to interpret. [Lesson 8](C:/dev/projects/VS/62413/62413courseInfo/Lesson08/lesson8_E26.pdf)

**In RatMan.** `RatMan.dll` references the engine assembly `RatMan.Game.dll`. The app embeds `Classic.maze` and opens it through `typeof(FileMazeRepository).Assembly.GetManifestResourceStream(...)`. In `FoodViewTests`, `GetField("renderingClock", ...)` resolves a private field by name at runtime and `GetValue` reads it to verify that the clock stops when a view unloads. MSTest uses `[TestClass]` and `[TestMethod]` to discover tests. [Assembly reference and resource](/C:/dev/projects/VS/62413/src/RatMan.App/RatMan.App.csproj:5) · [Embedded maze lookup](/C:/dev/projects/VS/62413/src/RatMan.App/Persistence/FileMazeRepository.cs:30) · [Test attributes](/C:/dev/projects/VS/62413/tests/RatMan.Tests/FoodViewTests.cs:16) · [Reflection in the clock test](/C:/dev/projects/VS/62413/tests/RatMan.Tests/FoodViewTests.cs:81)

## 6. AppDomains, multithreading and parallel programming

**Theory.** An AppDomain was a unit of isolation within a .NET Framework process. Modern .NET uses a single AppDomain per process and provides `AssemblyLoadContext` for controlling assembly loading. A thread is a path of execution. Parallelism runs work simultaneously, for example across CPU cores with `Parallel.For`. With `async`/`await`, a method can yield while a task is incomplete so its caller can continue. Synchronization protects shared state when operations overlap. [Threading lecture](C:/dev/projects/VS/62413/62413courseInfo/Lesson09/lesson09_F26.pdf) · [AppDomains in .NET](https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability) · [Asynchronous programming](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)

**AppDomains in RatMan**

**Threads and async in RatMan.** The WPF rendering test creates a thread in a single-threaded apartment (STA) for its UI objects, then calls `Join` to wait for it. The high-score repository awaits file I/O and uses one `SemaphoreSlim` per score-file path to serialize overlapping submissions within the process. That protects the read, modify and write sequence from lost updates. [WPF test thread](/C:/dev/projects/VS/62413/tests/RatMan.Tests/FoodViewTests.cs:20) · [Score submission synchronization](/C:/dev/projects/VS/62413/src/RatMan.App/Persistence/FileHighScoreRepository.cs:49)

**Parallel programming in RatMan**

## 7. EF Core and LINQ

**Theory.** LINQ provides operators such as `Where` to filter, `Select` to project, and `OrderBy` to sort. Many operators defer execution until the sequence is enumerated, and `ToArray()` evaluates it immediately. EF Core maps .NET entities to a database. Its provider translates supported LINQ queries to the database’s query language, and `DbContext.SaveChanges()` persists tracked changes. [EF Core queries](https://learn.microsoft.com/en-us/ef/core/querying/) · [Saving tracked changes](https://learn.microsoft.com/en-us/ef/core/saving/)

**LINQ in RatMan.** `HighScoreRules.Top` runs `OrderByDescending(e => e.Score).Take(Capacity).ToArray()` on the in-memory score entries to produce the top ten. The sort is stable, so an existing score stays ahead of a newly appended score with the same value. [Leaderboard ranking](/C:/dev/projects/VS/62413/src/RatMan.App/Persistence/HighScores.cs:34) · [Adding a score](/C:/dev/projects/VS/62413/src/RatMan.App/Persistence/FileHighScoreRepository.cs:68) · [`OrderByDescending` documentation](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.orderbydescending?view=net-10.0)

**EF Core in RatMan**
