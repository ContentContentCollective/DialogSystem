using System;
using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Ports;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// A standard .NET implementation of the delay strategy using <see cref="Task.Delay(int, CancellationToken)"/>.
    /// This acts as the Terminal equivalent to UniTaskDelayStrategy.
    /// </summary>
    public sealed class TaskDelayStrategy : IDelayStrategy
    {
        /// <summary>
        /// Delays execution asynchronously for the specified time using standard .NET Tasks.
        /// </summary>
        /// <param name="seconds">The duration to delay in seconds.</param>
        /// <param name="cancellationToken">A token to cancel the wait operation.</param>
        /// <returns>A task representing the delay.</returns>
        public async Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
            {
                return;
            }

            int millisecondsDelay = (int)Math.Round(seconds * 1000f);
            await Task.Delay(millisecondsDelay, cancellationToken);
        }
    }
}