using System;

namespace ContentContent.Dialog.Core.Interfaces
{
    /// <summary>
    /// Abstracts user advancement and skip inputs.
    /// Allows the core dialog orchestrator to be driven deterministically by unit test mocks or distinct engine inputs.
    /// </summary>
    public interface IDialogInputProvider : IDisposable
    {
        /// <summary>
        /// Occurs when the user triggers the mapped dialog advancement input action.
        /// </summary>
        event Action AdvanceRequested;

        /// <summary>
        /// Enables input listening and binds the necessary callbacks.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables input listening and unbinds the associated callbacks.
        /// </summary>
        void Disable();
    }
}