# ContentContent Dialog System

A modular, zero-allocation, engine-agnostic dialog system prioritizing strict S.O.L.I.D. principles, passive views, and dependency injection. 

While designed natively for Unity 6000.5+ and UI Toolkit, the core domain is written in pure C# (.NET Standard 2.1) with zero engine references, allowing identical behavior across Unity, Godot, WPF, or standard Terminal applications.

## Architecture & Design Philosophy

The system enforces a strict boundary between narrative orchestration and engine-specific presentation:
*   **Pure Domain Core (`ContentContent.Dialog.Core`):** Contains immutable data payloads (`DialogLine`, `DialogNode`), state machines, and the MVP Presenter (`DialogService`). It is entirely oblivious to Unity, `ScriptableObject`, or UI frameworks.
*   **Engine Adapters (`ContentContent.Dialog.Unity`):** Concrete implementations of the core interfaces. In Unity, this maps `IDialogView` to `PanelRenderer` (UI Toolkit), `IDelayStrategy` to `UniTask`, and `IDialogInputProvider` to `com.unity.inputsystem`.
*   **Narrative Extensions (`ContentContent.Dialog.Ink`):** Isolated modules that adapt external narrative engines (like Ink) to the core `IDialogSource` stream without polluting the core package.

## Repository Structure

To support multi-engine distribution and prevent IDE/MSBuild conflicts, executable hosts are separated from the distributable library source.

*   `Source/`: Contains the discrete distributable packages. `ContentContent.Dialog.Unity` is the UPM package root.
*   `Demos/`: Contains executable sandboxes to verify the system. 
    *   `Demos/Unity/` is the quarantined Unity project.
    *   `Demos/Terminal/` is a standard `.NET 8` console application proving pure C# compatibility.
*   `Documentation/`: The unified DocFX build pipeline for generating static HTML API references and guides.

## Quick Start (Unity)

The Unity adapter package is distributed via OpenUPM.

1.  Add the package via the Unity Package Manager using the Git URL: 
    `https://github.com/johnnyjaywu/dialogsystem.git?path=/Source/ContentContent.Dialog.Unity`
2.  Use a Dependency Injection framework (e.g., VContainer) to bind the core `DialogService` to the Unity adapters (`DialogViewUIToolkit`, `DialogInputProvider`).
3.  Assign your `DialogAsset` (`ScriptableObject`) and execute `StartDialog()`.

## Quick Start (Terminal Demo)

To verify the core logic runs independently of Unity:
1.  Navigate to `Demos/Terminal/`
2.  Run `dotnet build` and `dotnet run`.
3.  The console will utilize `System.Console` to render the UI and `Task.Delay` to execute the asynchronous typewriter effect.