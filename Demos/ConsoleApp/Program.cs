using System;
using System.Threading;
using ConsoleDemo.Adapters;
using ContentContent.Dialog;

namespace ConsoleDemo
{
    /// <summary>
    /// The application composition root. Acts as a manual dependency injection bootstrapper
    /// substituting the role of VContainer.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the terminal application.
        /// </summary>
        public static void Main()
        {
            Console.Title = "ContentContent Dialog System - Terminal Demo";

            // 1. Query User State Setup
            Console.Clear();
            Console.Write("Enter your name: ");
            string? inputName = Console.ReadLine();
            string resolvedName = string.IsNullOrWhiteSpace(inputName) ? "???" : inputName;

            // 2. Instantiate Adapters
            using ConsoleInputProvider inputProvider = new ConsoleInputProvider();
            ConsoleDialogView view = new ConsoleDialogView(inputProvider);
            TaskDelayStrategy delayStrategy = new TaskDelayStrategy();

            // Use 0.05s per character for reading clarity in terminal
            DialogSettings settings = new DialogSettings(secondsPerCharacter: 0.05f, allowTypewriterSkip: true);

            // 3. Compose the Core Service via the Fluent Builder
            using IDialogService dialogService = new DialogServiceBuilder()
                .WithView(view)
                .WithInputProvider(inputProvider)
                .WithDelayStrategy(delayStrategy)
                .WithSettings(settings)
                .Build();

            using ManualResetEventSlim sessionCompletedEvent = new ManualResetEventSlim(false);

            void HandleDialogEnded()
            {
                sessionCompletedEvent.Set();
            }

            dialogService.DialogEnded += HandleDialogEnded;

            // 4. Construct the Source with injected state and Start
            MockDialogSource mockSource = new MockDialogSource(resolvedName);
            dialogService.StartDialog(mockSource);

            // 5. Block the main thread deterministically until the sequence resolves
            sessionCompletedEvent.Wait();

            dialogService.DialogEnded -= HandleDialogEnded;

            Console.Clear();
        }
    }
}