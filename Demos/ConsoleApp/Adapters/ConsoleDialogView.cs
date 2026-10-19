using System;
using System.Collections.Generic;
using ContentContent.Dialog.Core.Interfaces;
using ContentContent.Dialog.Core.Models;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// Renders the dialog data to the standard console output.
    /// Implements atomic data binding to manage screen redrawing and history trimming deterministically.
    /// </summary>
    public class ConsoleDialogView : IDialogView
    {
        private const int MaxHistoryLines = 100; // Maximum number of lines to retain in the history buffer
        private readonly ConsoleInputProvider _inputProvider;
        private readonly List<(string Dialog, string Choice)> _historyBuffer;

        private DialogLine _activeLine;
        private string _lastSelectedChoiceText;

        private bool _isLayoutRendered;
        private bool _isChoicesRendered;

        private (int X, int Y) _textStartPosition;
        private (int X, int Y) _choiceStartPosition;
        private int _currentChoiceIndex = 0;

        /// <summary>
        /// Occurs when a choice is selected via keyboard input.
        /// </summary>
        public event Action<int> ChoiceSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleDialogView"/> class.
        /// </summary>
        /// <param name="inputProvider">The input provider used to capture choice navigation.</param>
        /// <exception cref="ArgumentNullException">Thrown if the input provider is null.</exception>
        public ConsoleDialogView(ConsoleInputProvider inputProvider)
        {
            _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            _historyBuffer = new List<(string Dialog, string Choice)>();

            _inputProvider.SelectRequested += HandleSelection;
            _inputProvider.NavigateNextRequested += HandleNavigateNext;
            _inputProvider.NavigatePreviousRequested += HandleNavigatePrevious;
        }

        /// <summary>
        /// Prepares the console for rendering by clearing the buffer and hiding the cursor.
        /// </summary>
        public void Show()
        {
            Console.Clear();
            Console.CursorVisible = false;
        }

        /// <summary>
        /// Clears the console output upon session termination and restores cursor visibility.
        /// </summary>
        public void Hide()
        {
            Console.Clear();
            Console.CursorVisible = true;
        }

        /// <summary>
        /// Atomically binds the new payload, instantly archiving the previous turn's state 
        /// and blocking input until the visual layout is explicitly executed.
        /// </summary>
        /// <param name="line">The immutable dialog payload for the current turn.</param>
        public void BindLine(DialogLine line)
        {
            // Archive the previous state before overwriting it with the new payload
            ArchiveCurrentLine();

            _activeLine = line;
            _currentChoiceIndex = 0;

            _isLayoutRendered = false;
            _isChoicesRendered = false;
        }

        /// <summary>
        /// Trims history to fit the window, redraws the screen via Console.Clear(), 
        /// and renders the active speaker name.
        /// </summary>
        public void RenderStaticLayout()
        {
            RedrawHistoryAndLayout();

            if (!string.IsNullOrWhiteSpace(_activeLine.Speaker))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{_activeLine.Speaker}: ");
                Console.ResetColor();
            }

            _textStartPosition = Console.GetCursorPosition();
            _isLayoutRendered = true;
        }

        /// <summary>
        /// Reveals a specific count of characters on the same line as the speaker name.
        /// </summary>
        /// <param name="visibleCharacters">The number of visible characters to reveal.</param>
        public void RevealCharacters(int visibleCharacters)
        {
            if (!_isLayoutRendered)
            {
                RenderStaticLayout();
            }

            Console.SetCursorPosition(_textStartPosition.X, _textStartPosition.Y);

            string visibleSubstring = string.IsNullOrEmpty(_activeLine.Text)
                ? string.Empty
                : _activeLine.Text.Substring(0, visibleCharacters);

            int paddingLength = Math.Max(0, Console.WindowWidth - _textStartPosition.X - 1);
            Console.Write(visibleSubstring.PadRight(paddingLength));
        }

        /// <summary>
        /// Renders interactive choice buttons horizontally inside square brackets.
        /// </summary>
        public void RenderChoices()
        {
            if (!_activeLine.HasChoices)
            {
                return;
            }

            Console.WriteLine();
            _choiceStartPosition = Console.GetCursorPosition();
            _isChoicesRendered = true; // Unblock directional and selection inputs

            DrawChoices();
        }

        /// <summary>
        /// Commits the active speaker, text, and any selected choice into the history buffer.
        /// </summary>
        private void ArchiveCurrentLine()
        {
            // If the struct is default (empty), do not archive it.
            if (string.IsNullOrWhiteSpace(_activeLine.Text))
            {
                return;
            }

            string prefix = string.IsNullOrWhiteSpace(_activeLine.Speaker) ? string.Empty : $"{_activeLine.Speaker}: ";
            string dialogLine = $"{prefix}{_activeLine.Text}";

            _historyBuffer.Add((dialogLine, _lastSelectedChoiceText));

            // Clear the choice state for the next turn
            _lastSelectedChoiceText = null;
        }

        /// <summary>
        /// Clears the console and redraws the scrollable history, evicting old lines if they exceed vertical space.
        /// </summary>
        private void RedrawHistoryAndLayout()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            // Trim oldest history until the remaining entries fit the available height
            while (CalculateTotalHistoryHeight() > MaxHistoryLines && _historyBuffer.Count > 0)
            {
                _historyBuffer.RemoveAt(0);
            }

            foreach ((string dialog, string choice) in _historyBuffer)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(dialog);

                if (!string.IsNullOrEmpty(choice))
                {
                    Console.WriteLine($"> {choice}");
                }
            }

            Console.ResetColor();

            if (_historyBuffer.Count > 0)
            {
                Console.WriteLine(); // Visual separation before the new active node
            }
        }

        /// <summary>
        /// Calculates the total vertical rows required to render the current history buffer, 
        /// accounting for potential word-wrapping in standard terminals.
        /// </summary>
        private int CalculateTotalHistoryHeight()
        {
            int totalLines = 0;

            foreach ((string dialog, string choice) in _historyBuffer)
            {
                totalLines += (dialog.Length / Math.Max(1, Console.WindowWidth)) + 1;

                if (!string.IsNullOrEmpty(choice))
                {
                    totalLines += (choice.Length / Math.Max(1, Console.WindowWidth)) + 1;
                }
            }

            return totalLines;
        }

        /// <summary>
        /// Renders the current choices horizontally, highlighting the active selection.
        /// </summary>
        private void DrawChoices()
        {
            if (!_isChoicesRendered || !_activeLine.HasChoices)
            {
                return;
            }

            Console.SetCursorPosition(_choiceStartPosition.X, _choiceStartPosition.Y);

            for (int i = 0; i < _activeLine.Choices.Count; i++)
            {
                if (i == _currentChoiceIndex)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.Write($"[{_activeLine.Choices[i]}] ");
                Console.ResetColor();
            }

            // Overwrite any remaining trailing artifacts on the choice line
            Console.Write("".PadRight(Math.Max(0, Console.WindowWidth - Console.CursorLeft)));
        }

        private void HandleSelection()
        {
            if (!_isChoicesRendered)
            {
                return;
            }

            if (_activeLine.HasChoices && _currentChoiceIndex >= 0 && _currentChoiceIndex < _activeLine.Choices.Count)
            {
                _lastSelectedChoiceText = _activeLine.Choices[_currentChoiceIndex];

                // Instantly swap the rendered choices back to normal coloring to provide visual feedback
                Console.SetCursorPosition(_choiceStartPosition.X, _choiceStartPosition.Y);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"> {_lastSelectedChoiceText}".PadRight(Console.WindowWidth));
                Console.ResetColor();

                ChoiceSelected?.Invoke(_currentChoiceIndex);
            }
        }

        private void HandleNavigateNext()
        {
            if (!_isChoicesRendered || !_activeLine.HasChoices)
            {
                return;
            }

            _currentChoiceIndex = (_currentChoiceIndex + 1) % _activeLine.Choices.Count;
            DrawChoices();
        }

        private void HandleNavigatePrevious()
        {
            if (!_isChoicesRendered || !_activeLine.HasChoices)
            {
                return;
            }

            _currentChoiceIndex = (_currentChoiceIndex - 1 + _activeLine.Choices.Count) % _activeLine.Choices.Count;
            DrawChoices();
        }
    }
}