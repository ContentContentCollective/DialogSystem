using System;
using ContentContent.Dialog.Ports;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Top-level domain facade for starting, stopping, and observing dialog sessions.
    /// Implements <see cref="IDisposable"/> to ensure active typewriter tasks and input subscriptions are cleaned up.
    /// </summary>
    public interface IDialogService : IDisposable
    {
        /// <summary>
        /// Occurs when a dialog session starts.
        /// </summary>
        event Action? DialogStarted;

        /// <summary>
        /// Occurs when a dialog session ends or is stopped.
        /// </summary>
        event Action? DialogEnded;

        /// <summary>
        /// Gets a value indicating whether a dialog session is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Starts a dialog session using the provided dialog source. If a session is already active, it is stopped before starting the new one.
        /// </summary>
        /// <param name="source">The narrative data source driving the conversation.</param>
        void StartDialog(IDialogSource source);

        /// <summary>
        /// Stops the current dialog session if one is active. If no session is active, this method has no effect.
        /// </summary>
        void StopDialog();
    }
}