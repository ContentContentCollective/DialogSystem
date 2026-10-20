using System;
using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Ports;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Internal domain orchestrator for dialog playback. Connects narrative data sources to the passive view,
    /// manages the typewriter state machine, and handles user input routing.
    /// </summary>
    internal sealed class DialogService : IDialogService
    {
        private readonly IDialogView view;
        private readonly IDialogInputProvider inputProvider;
        private readonly IDelayStrategy delayStrategy;
        private readonly DialogSettings settings;

        private IDialogSource? currentSource;
        private CancellationTokenSource? typewriterCancellationTokenSource;
        private DialogLine activeLine;
        private bool isTypewriting;
        private bool isDisposed;

        /// <inheritdoc />
        public event Action? DialogStarted;

        /// <inheritdoc />
        public event Action? DialogEnded;

        /// <inheritdoc />
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogService"/> class.
        /// </summary>
        /// <param name="view">The passive presentation view.</param>
        /// <param name="inputProvider">The abstract input provider for handling advancements.</param>
        /// <param name="delayStrategy">The timing strategy for the typewriter effect.</param>
        /// <param name="settings">The domain settings governing typewriter speeds and skip rules.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is <c>null</c>.</exception>
        internal DialogService(
            IDialogView view,
            IDialogInputProvider inputProvider,
            IDelayStrategy delayStrategy,
            DialogSettings settings)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            this.delayStrategy = delayStrategy ?? throw new ArgumentNullException(nameof(delayStrategy));
            this.settings = settings;

            this.view.ChoiceSelected += this.HandleChoiceSelected;
            this.inputProvider.AdvanceRequested += this.HandleAdvanceRequested;
        }

        /// <inheritdoc />
        public void StartDialog(IDialogSource source)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(DialogService));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (this.IsActive)
            {
                this.StopDialog();
            }

            this.currentSource = source;
            this.IsActive = true;

            this.inputProvider.Enable();
            this.view.Show();

            this.DialogStarted?.Invoke();

            this.AdvanceToNextNode();
        }

        /// <inheritdoc />
        public void StopDialog()
        {
            if (!this.IsActive)
            {
                return;
            }

            this.IsActive = false;
            this.isTypewriting = false;

            this.CancelTypewriter();
            this.inputProvider.Disable();
            this.view.Hide();

            this.currentSource = null;
            this.DialogEnded?.Invoke();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.StopDialog();
            this.isDisposed = true;

            this.view.ChoiceSelected -= this.HandleChoiceSelected;
            this.inputProvider.AdvanceRequested -= this.HandleAdvanceRequested;
        }

        private void HandleAdvanceRequested()
        {
            if (!this.IsActive)
            {
                return;
            }

            if (this.isTypewriting)
            {
                if (this.settings.AllowTypewriterSkip)
                {
                    this.CancelTypewriter();
                }

                return;
            }

            if (this.activeLine.HasChoices)
            {
                return;
            }

            if (this.currentSource != null && this.currentSource.CanContinue)
            {
                this.AdvanceToNextNode();
            }
            else
            {
                this.StopDialog();
            }
        }

        private void HandleChoiceSelected(int choiceIndex)
        {
            if (!this.IsActive || this.isTypewriting || this.currentSource == null)
            {
                return;
            }

            if (this.activeLine.HasChoices && choiceIndex >= 0 && choiceIndex < this.activeLine.Choices.Count)
            {
                this.currentSource.SelectChoice(choiceIndex);
                this.AdvanceToNextNode();
            }
        }

        private void AdvanceToNextNode()
        {
            if (this.currentSource == null || !this.currentSource.CanContinue)
            {
                this.StopDialog();
                return;
            }

            this.activeLine = this.currentSource.GetNextLine();
            _ = this.PlayLineSequenceAsync(this.activeLine);
        }

        private async Task PlayLineSequenceAsync(DialogLine line)
        {
            this.CancelTypewriter();

            this.view.BindLine(line);
            this.view.RenderStaticLayout();
            this.view.RevealCharacters(0);

            this.isTypewriting = true;

            using CancellationTokenSource localCts = new CancellationTokenSource();
            this.typewriterCancellationTokenSource = localCts;
            CancellationToken token = localCts.Token;

            try
            {
                int length = line.Text.Length;
                for (int i = 0; i <= length; i++)
                {
                    token.ThrowIfCancellationRequested();

                    this.view.RevealCharacters(i);

                    if (i < length)
                    {
                        await this.delayStrategy.DelayAsync(this.settings.SecondsPerCharacter, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (this.IsActive)
                {
                    this.view.RevealCharacters(line.Text.Length);
                }
            }
            finally
            {
                if (ReferenceEquals(this.typewriterCancellationTokenSource, localCts))
                {
                    this.typewriterCancellationTokenSource = null;
                    this.isTypewriting = false;

                    if (this.IsActive && line.HasChoices)
                    {
                        this.view.RenderChoices();
                    }
                }
            }
        }

        private void CancelTypewriter()
        {
            if (this.typewriterCancellationTokenSource != null && !this.typewriterCancellationTokenSource.IsCancellationRequested)
            {
                this.typewriterCancellationTokenSource.Cancel();
            }
        }
    }
}