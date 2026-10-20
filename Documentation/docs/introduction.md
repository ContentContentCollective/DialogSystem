# Introduction

**ContentContent.Dialog** is a modular, engine-agnostic C# dialog system designed around Hexagonal Architecture (Ports and Adapters) with first-class support for **Unity 6000.5+**.

## Design Principles

1. **Zero Engine Coupling in Core:** The core assembly (`ContentContent.Dialog`) targets `.netstandard2.1` and compiles with `noEngineReferences: true`, allowing deterministic testing in standalone .NET runners.
2. **Passive View Presentation:** Views (`IDialogView`) perform zero narrative state mutation, executing atomic data binding (`BindLine`) followed by explicit rendering commands.[cite: 1]
3. **Strict Encapsulation:** Concrete runtime orchestrators remain `internal`, constructed exclusively via `DialogServiceBuilder`.