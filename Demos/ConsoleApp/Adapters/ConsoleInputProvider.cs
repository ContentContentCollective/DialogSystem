using System;
using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Core.Interfaces;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// Polls the console input on a background thread to raise interaction events 
    /// without blocking the asynchronous typewriter loop.
    /// </summary>
    public class ConsoleInputProvider : IDialogInputProvider
    {
        private CancellationTokenSource _inputCancellationTokenSource;

        /// <summary>
        /// Occurs when the user requests to advance the dialog.
        /// </summary>
        public event Action AdvanceRequested;
        
        /// <summary>
        /// Occurs when the user selects a highlighted choice.
        /// </summary>
        public event Action SelectRequested;
        
        /// <summary>
        /// Occurs when the user navigates to the next choice.
        /// </summary>
        public event Action NavigateNextRequested;
        
        /// <summary>
        /// Occurs when the user navigates to the previous choice.
        /// </summary>
        public event Action NavigatePreviousRequested;

        /// <summary>
        /// Enables background input listening.
        /// </summary>
        public void Enable()
        {
            if (_inputCancellationTokenSource != null)
            {
                return;
            }

            _inputCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _inputCancellationTokenSource.Token;

            Task.Run(() => ListenForInputAsync(token), token);
        }

        /// <summary>
        /// Disables background input listening and gracefully cancels the active task.
        /// </summary>
        public void Disable()
        {
            if (_inputCancellationTokenSource != null)
            {
                _inputCancellationTokenSource.Cancel();
                _inputCancellationTokenSource.Dispose();
                _inputCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Disposes of the input provider.
        /// </summary>
        public void Dispose()
        {
            Disable();
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
                            AdvanceRequested?.Invoke();
                            SelectRequested?.Invoke();
                        }
                        else if (keyInfo.Key == ConsoleKey.UpArrow || keyInfo.Key == ConsoleKey.LeftArrow)
                        {
                            NavigatePreviousRequested?.Invoke();
                        }
                        else if (keyInfo.Key == ConsoleKey.DownArrow || keyInfo.Key == ConsoleKey.RightArrow)
                        {
                            NavigateNextRequested?.Invoke();
                        }
                    }

                    // Delay polling to prevent CPU pegging. 
                    // This throws OperationCanceledException when Disable() is called.
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