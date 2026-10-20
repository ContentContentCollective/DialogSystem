using System;
using ContentContent.Dialog.Ports;
using UnityEngine.InputSystem;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Unity Input System adapter implementing <see cref="IDialogInputProvider"/>.
    /// Accepts an injected <see cref="InputAction"/> from the consumer's generated C# Input Action class.
    /// </summary>
    public sealed class UnityDialogInputProvider : IDialogInputProvider
    {
        private readonly InputAction advanceAction;
        private readonly bool manageActionLifecycle;
        private bool isEnabled;
        private bool isDisposed;

        /// <inheritdoc />
        public event Action? AdvanceRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityDialogInputProvider"/> class.
        /// </summary>
        /// <param name="advanceAction">The input action triggered to advance or skip dialog (e.g., injected from a generated C# input wrapper).</param>
        /// <param name="manageActionLifecycle">
        /// If <c>true</c>, calls <see cref="InputAction.Enable"/> and <see cref="InputAction.Disable"/> on <paramref name="advanceAction"/> 
        /// when this provider is enabled or disabled. Set to <c>false</c> if the parent <see cref="InputActionMap"/> lifecycle is managed externally.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="advanceAction"/> is <c>null</c>.</exception>
        public UnityDialogInputProvider(InputAction advanceAction, bool manageActionLifecycle = true)
        {
            this.advanceAction = advanceAction ?? throw new ArgumentNullException(nameof(advanceAction));
            this.manageActionLifecycle = manageActionLifecycle;
        }

        /// <inheritdoc />
        public void Enable()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(UnityDialogInputProvider));
            }

            if (this.isEnabled)
            {
                return;
            }

            this.isEnabled = true;
            this.advanceAction.performed += this.HandleAdvancePerformed;

            if (this.manageActionLifecycle && !this.advanceAction.enabled)
            {
                this.advanceAction.Enable();
            }
        }

        /// <inheritdoc />
        public void Disable()
        {
            if (!this.isEnabled)
            {
                return;
            }

            this.isEnabled = false;
            this.advanceAction.performed -= this.HandleAdvancePerformed;

            if (this.manageActionLifecycle && this.advanceAction.enabled)
            {
                this.advanceAction.Disable();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.Disable();
            this.isDisposed = true;
        }

        private void HandleAdvancePerformed(InputAction.CallbackContext context)
        {
            this.AdvanceRequested?.Invoke();
        }
    }
}