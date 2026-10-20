# Getting Started

## Installation via UPM

Add the package to your Unity `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.contentcontent.dialog": "[https://github.com/contentcontentcollective/dialogsystem.git#upm](https://github.com/contentcontentcollective/dialogsystem.git#upm)"
  }
}
```

## Composing the Dialog Service

Use `DialogServiceBuilder` in your composition root to wire your `IDialogView`, `IDialogInputProvider`, and `IDelayStrategy` implementations:

```csharp
using IDialogService dialogService = new DialogServiceBuilder()
    .WithView(view)
    .WithInputProvider(inputProvider)
    .WithDelayStrategy(delayStrategy)
    .WithSettings(DialogSettings.Default)
    .Build();
```