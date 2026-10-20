using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Ports;
using UnityEngine;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Unity main-thread implementation of <see cref="IDelayStrategy"/> backed by <see cref="Awaitable.WaitForSecondsAsync"/>.
    /// Executes deterministically on the Unity PlayerLoop without ThreadPool context switches.
    /// </summary>
    public sealed class AwaitableDelayStrategy : IDelayStrategy
    {
        /// <inheritdoc />
        public async Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
            {
                return;
            }

            await Awaitable.WaitForSecondsAsync(seconds, cancellationToken);
        }
    }
}