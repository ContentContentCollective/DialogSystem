using System;
using System.Threading;
using ContentContent.Dialog.Core.Models;
using ContentContent.Dialog.Core.Services;
using ConsoleDemo.Adapters;

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
            string inputName = Console.ReadLine();

            // 2. Instantiate Adapters
            using ConsoleInputProvider inputProvider = new ConsoleInputProvider();
            ConsoleDialogView view = new ConsoleDialogView(inputProvider);
            TaskDelayStrategy delayStrategy = new TaskDelayStrategy();

            // Use 0.05s per character for reading clarity in terminal
            DialogSettings settings = new DialogSettings(secondsPerCharacter: 0.05f, allowTypewriterSkip: true);

            // 3. Compose the Core Service
            using DialogService dialogService = new DialogService(view, inputProvider, delayStrategy, settings);

            bool isRunning = true;

            void HandleDialogEnded()
            {
                isRunning = false;
            }

            dialogService.DialogEnded += HandleDialogEnded;

            // 4. Construct the Source with injected state and Start
            MockDialogSource mockSource = new MockDialogSource(inputName);
            dialogService.StartDialog(mockSource);

            // 5. Block the main thread until the sequence resolves
            while (isRunning)
            {
                Thread.Sleep(100);
            }

            dialogService.DialogEnded -= HandleDialogEnded;

            Console.Clear();
        }
    }
}