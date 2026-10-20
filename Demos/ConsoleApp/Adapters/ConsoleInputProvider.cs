using System;
using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Ports;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// Polls the console input on a background thread to raise interaction events
    /// without blocking the asynchronous typewriter loop.
    /// </summary>
    public sealed class ConsoleInputProvider : IDialogInputProvider
    {
        private CancellationTokenSource? inputCancellationTokenSource;

        /// <summary>
        /// Occurs when the user requests to advance the dialog.
        /// </summary>
        public event Action? AdvanceRequested;

        /// <summary>
        /// Occurs when the user selects a highlighted choice.
        /// </summary>
        public event Action? SelectRequested;

        /// <summary>
        /// Occurs when the user navigates to the next choice.
        /// </summary>
        public event Action? NavigateNextRequested;

        /// <summary>
        /// Occurs when the user navigates to the previous choice.
        /// </summary>
        public event Action? NavigatePreviousRequested;

        /// <summary>
        /// Enables background input listening.
        /// </summary>
        public void Enable()
        {
            if (this.inputCancellationTokenSource != null)
            {
                return;
            }

            this.inputCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = this.inputCancellationTokenSource.Token;

            Task.Run(() => this.ListenForInputAsync(token), token);
        }

        /// <summary>
        /// Disables background input listening and gracefully cancels the active task.
        /// </summary>
        public void Disable()
        {
            if (this.inputCancellationTokenSource != null)
            {
                this.inputCancellationTokenSource.Cancel();
                this.inputCancellationTokenSource.Dispose();
                this.inputCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Disposes of the input provider.
        /// </summary>
        public void Dispose()
        {
            this.Disable();
        }

        private async Task ListenForInputAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                        if (keyInfo.Key == ConsoleKey.Spacebar || keyInfo.Key == ConsoleKey.Enter)
                        {
                            this.AdvanceRequested?.Invoke();
                            this.SelectRequested?.Invoke();
                        }
                        else if (keyInfo.Key == ConsoleKey.UpArrow || keyInfo.Key == ConsoleKey.LeftArrow)
                        {
                            this.NavigatePreviousRequested?.Invoke();
                        }
                        else if (keyInfo.Key == ConsoleKey.DownArrow || keyInfo.Key == ConsoleKey.RightArrow)
                        {
                            this.NavigateNextRequested?.Invoke();
                        }
                    }

                    await Task.Delay(16, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected behavior when the token is cancelled. Exit gracefully.
            }
        }
    }
}