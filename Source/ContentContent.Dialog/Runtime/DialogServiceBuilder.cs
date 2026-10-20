using System;
using ContentContent.Dialog.Ports;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Fluent builder for configuring and instantiating an encapsulated <see cref="IDialogService"/> instance.
    /// Enforces dependency validation at composition time while keeping the concrete orchestrator internal.
    /// </summary>
    public sealed class DialogServiceBuilder
    {
        private IDialogView? view;
        private IDialogInputProvider? inputProvider;
        private IDelayStrategy? delayStrategy;
        private DialogSettings settings = DialogSettings.Default;

        /// <summary>
        /// Configures the passive view implementation used to render dialog state.
        /// </summary>
        /// <param name="dialogView">The passive presentation view.</param>
        /// <returns>The current <see cref="DialogServiceBuilder"/> instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dialogView"/> is <c>null</c>.</exception>
        public DialogServiceBuilder WithView(IDialogView dialogView)
        {
            this.view = dialogView ?? throw new ArgumentNullException(nameof(dialogView));
            return this;
        }

        /// <summary>
        /// Configures the input provider used to advance and skip dialog sequences.
        /// </summary>
        /// <param name="dialogInputProvider">The input provider implementation.</param>
        /// <returns>The current <see cref="DialogServiceBuilder"/> instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dialogInputProvider"/> is <c>null</c>.</exception>
        public DialogServiceBuilder WithInputProvider(IDialogInputProvider dialogInputProvider)
        {
            this.inputProvider = dialogInputProvider ?? throw new ArgumentNullException(nameof(dialogInputProvider));
            return this;
        }

        /// <summary>
        /// Configures the timing delay strategy used to drive the character typewriter sequence.
        /// </summary>
        /// <param name="typewriterDelayStrategy">The asynchronous delay strategy implementation.</param>
        /// <returns>The current <see cref="DialogServiceBuilder"/> instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typewriterDelayStrategy"/> is <c>null</c>.</exception>
        public DialogServiceBuilder WithDelayStrategy(IDelayStrategy typewriterDelayStrategy)
        {
            this.delayStrategy = typewriterDelayStrategy ?? throw new ArgumentNullException(nameof(typewriterDelayStrategy));
            return this;
        }

        /// <summary>
        /// Configures the domain settings governing typewriter speed and skip permissions.
        /// If omitted, <see cref="DialogSettings.Default"/> is used.
        /// </summary>
        /// <param name="dialogSettings">The immutable dialog settings configuration.</param>
        /// <returns>The current <see cref="DialogServiceBuilder"/> instance for method chaining.</returns>
        public DialogServiceBuilder WithSettings(DialogSettings dialogSettings)
        {
            this.settings = dialogSettings;
            return this;
        }

        /// <summary>
        /// Validates all required infrastructure ports and constructs a new <see cref="IDialogService"/> instance.
        /// </summary>
        /// <returns>A fully initialized <see cref="IDialogService"/> orchestrator.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any required port (<see cref="IDialogView"/>, <see cref="IDialogInputProvider"/>, or <see cref="IDelayStrategy"/>) was not provided.</exception>
        public IDialogService Build()
        {
            if (this.view == null)
            {
                throw new InvalidOperationException($"Cannot build {nameof(IDialogService)}: {nameof(IDialogView)} must be configured via {nameof(this.WithView)}.");
            }

            if (this.inputProvider == null)
            {
                throw new InvalidOperationException($"Cannot build {nameof(IDialogService)}: {nameof(IDialogInputProvider)} must be configured via {nameof(this.WithInputProvider)}.");
            }

            if (this.delayStrategy == null)
            {
                throw new InvalidOperationException($"Cannot build {nameof(IDialogService)}: {nameof(IDelayStrategy)} must be configured via {nameof(this.WithDelayStrategy)}.");
            }

            return new DialogService(this.view, this.inputProvider, this.delayStrategy, this.settings);
        }
    }
}