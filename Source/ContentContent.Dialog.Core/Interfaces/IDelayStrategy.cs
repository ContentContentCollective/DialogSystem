using System.Threading;
using System.Threading.Tasks;

namespace ContentContent.Dialog.Core.Interfaces
{
    /// <summary>
    /// Abstracts the timing mechanism used for the typewriter effect.
    /// Allows injection of UniTask in Unity, or Task.Delay in standard .NET applications.
    /// </summary>
    public interface IDelayStrategy
    {
        /// <summary>
        /// Suspends execution for the specified duration.
        /// </summary>
        /// <param name="seconds">The number of seconds to delay.</param>
        /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
        /// <returns>A task representing the asynchronous delay operation.</returns>
        Task DelayAsync(float seconds, CancellationToken cancellationToken);
    }
}