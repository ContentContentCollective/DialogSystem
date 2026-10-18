using System;
using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Core.Interfaces;
using ContentContent.Dialog.Core.Models;

namespace ContentContent.Dialog.Core.Services
{
    /// <summary>
    /// The central orchestrator for dialog playback. Connects data sources to the passive view,
    /// manages the typewriter state machine, and handles user input routing.
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly IDialogView _view;
        private readonly IDialogInputProvider _inputProvider;
        private readonly IDelayStrategy _delayStrategy;
        private readonly DialogSettings _settings;

        private IDialogSource _currentSource;
        private CancellationTokenSource _typewriterCancellationTokenSource;
        private DialogLine _activeLine;
        private bool _isTypewriting;

        /// <summary>
        /// Occurs when a dialog session successfully starts.
        /// </summary>
        public event Action DialogStarted;

        /// <summary>
        /// Occurs when a dialog session naturally ends or is explicitly stopped.
        /// </summary>
        public event Action DialogEnded;

        /// <summary>
        /// Gets a value indicating whether a dialog session is currently actively running.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogService"/> class.
        /// </summary>
        /// <param name="view">The passive presentation view.</param>
        /// <param name="inputProvider">The abstract input provider for handling advancements.</param>
        /// <param name="delayStrategy">The timing strategy for the typewriter effect.</param>
        /// <param name="settings">The domain settings governing typewriter speeds and skip rules.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
        public DialogService(
            IDialogView view,
            IDialogInputProvider inputProvider,
            IDelayStrategy delayStrategy,
            DialogSettings settings)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            _delayStrategy = delayStrategy ?? throw new ArgumentNullException(nameof(delayStrategy));
            _settings = settings;

            _view.ChoiceSelected += HandleChoiceSelected;
            _inputProvider.AdvanceRequested += HandleAdvanceRequested;
        }

        /// <summary>
        /// Starts a new dialog session using the provided narrative source.
        /// </summary>
        /// <param name="source">The narrative state machine or data stream.</param>
        /// <exception cref="ArgumentNullException">Thrown if the source is null.</exception>
        public void StartDialog(IDialogSource source)
        {
            if (IsActive)
            {
                StopDialog();
            }

            _currentSource = source ?? throw new ArgumentNullException(nameof(source));
            IsActive = true;

            _inputProvider.Enable();
            _view.Show();

            DialogStarted?.Invoke();

            AdvanceToNextNode();
        }

        /// <summary>
        /// Stops the active dialog session, cleans up asynchronous tasks, and hides the view.
        /// </summary>
        public void StopDialog()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _isTypewriting = false;

            CancelTypewriter();
            _inputProvider.Disable();
            _view.ClearChoices();
            _view.Hide();

            _currentSource = null;
            DialogEnded?.Invoke();
        }

        /// <summary>
        /// Disposes of the service and unbinds all persistent event listeners to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            StopDialog();

            _view.ChoiceSelected -= HandleChoiceSelected;
            _inputProvider.AdvanceRequested -= HandleAdvanceRequested;

            _typewriterCancellationTokenSource?.Dispose();
        }

        private void HandleAdvanceRequested()
        {
            if (!IsActive)
            {
                return;
            }

            if (_isTypewriting)
            {
                if (_settings.AllowTypewriterSkip)
                {
                    CancelTypewriter();
                }

                return;
            }

            if (_activeLine.HasChoices)
            {
                // Must select a choice; generic advancement is blocked.
                return;
            }

            if (_currentSource != null && _currentSource.CanContinue)
            {
                AdvanceToNextNode();
            }
            else
            {
                StopDialog();
            }
        }

        private void HandleChoiceSelected(int choiceIndex)
        {
            if (!IsActive || _isTypewriting || _currentSource == null)
            {
                return;
            }

            if (_activeLine.HasChoices && choiceIndex >= 0 && choiceIndex < _activeLine.Choices.Count)
            {
                _view.ClearChoices();
                _currentSource.SelectChoice(choiceIndex);
                AdvanceToNextNode();
            }
        }

        private void AdvanceToNextNode()
        {
            if (_currentSource == null || !_currentSource.CanContinue)
            {
                StopDialog();
                return;
            }

            _activeLine = _currentSource.GetNextLine();

            // Fire and forget the async typewriter workflow. 
            // The Task acts as a safe coroutine replacement in pure C#.
            _ = PlayLineSequenceAsync(_activeLine);
        }

        private async Task PlayLineSequenceAsync(DialogLine line)
        {
            _view.SetSpeaker(line.Speaker);
            _view.SetFullText(line.Text);
            _view.RevealCharacters(0);
            _view.ClearChoices();

            _isTypewriting = true;
            _typewriterCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _typewriterCancellationTokenSource.Token;

            try
            {
                int length = line.Text.Length;
                for (int i = 0; i <= length; i++)
                {
                    token.ThrowIfCancellationRequested();

                    _view.RevealCharacters(i);

                    if (i < length)
                    {
                        await _delayStrategy.DelayAsync(_settings.SecondsPerCharacter, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Sequence was skipped by user input. Immediately reveal full text.
                _view.RevealCharacters(line.Text.Length);
            }
            finally
            {
                _isTypewriting = false;

                _typewriterCancellationTokenSource?.Dispose();
                _typewriterCancellationTokenSource = null;

                if (line.HasChoices)
                {
                    _view.DisplayChoices(line.Choices);
                }
            }
        }

        private void CancelTypewriter()
        {
            if (_typewriterCancellationTokenSource != null && !_typewriterCancellationTokenSource.IsCancellationRequested)
            {
                _typewriterCancellationTokenSource.Cancel();
            }
        }
    }
}