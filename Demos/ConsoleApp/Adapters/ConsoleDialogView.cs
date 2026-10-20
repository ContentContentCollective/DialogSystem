using System;
using System.Collections.Generic;
using ContentContent.Dialog;
using ContentContent.Dialog.Ports;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// Renders the dialog data to the standard console output.
    /// Implements atomic data binding to manage screen redrawing and history trimming deterministically.
    /// </summary>
    public sealed class ConsoleDialogView : IDialogView
    {
        private const int MaxHistoryLines = 100;

        private readonly ConsoleInputProvider inputProvider;
        private readonly List<(string Dialog, string? Choice)> historyBuffer;

        private DialogLine activeLine;
        private string? lastSelectedChoiceText;

        private bool isLayoutRendered;
        private bool isChoicesRendered;

        private (int X, int Y) textStartPosition;
        private (int X, int Y) choiceStartPosition;
        private int currentChoiceIndex;

        /// <summary>
        /// Occurs when a choice is selected via keyboard input.
        /// </summary>
        public event Action<int>? ChoiceSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleDialogView"/> class.
        /// </summary>
        /// <param name="inputProvider">The input provider used to capture choice navigation.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inputProvider"/> is <c>null</c>.</exception>
        public ConsoleDialogView(ConsoleInputProvider inputProvider)
        {
            this.inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            this.historyBuffer = new List<(string Dialog, string? Choice)>();

            this.inputProvider.SelectRequested += this.HandleSelection;
            this.inputProvider.NavigateNextRequested += this.HandleNavigateNext;
            this.inputProvider.NavigatePreviousRequested += this.HandleNavigatePrevious;
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
            this.ArchiveCurrentLine();

            this.activeLine = line;
            this.currentChoiceIndex = 0;

            this.isLayoutRendered = false;
            this.isChoicesRendered = false;
        }

        /// <summary>
        /// Trims history to fit the window, redraws the screen via <see cref="Console.Clear"/>,
        /// and renders the active speaker name.
        /// </summary>
        public void RenderStaticLayout()
        {
            this.RedrawHistoryAndLayout();

            if (!string.IsNullOrWhiteSpace(this.activeLine.Speaker))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{this.activeLine.Speaker}: ");
                Console.ResetColor();
            }

            this.textStartPosition = Console.GetCursorPosition();
            this.isLayoutRendered = true;
        }

        /// <summary>
        /// Reveals a specific count of characters on the same line as the speaker name.
        /// </summary>
        /// <param name="visibleCharacters">The number of visible characters to reveal.</param>
        public void RevealCharacters(int visibleCharacters)
        {
            if (!this.isLayoutRendered)
            {
                this.RenderStaticLayout();
            }

            Console.SetCursorPosition(this.textStartPosition.X, this.textStartPosition.Y);

            string visibleSubstring = string.IsNullOrEmpty(this.activeLine.Text)
                ? string.Empty
                : this.activeLine.Text.Substring(0, visibleCharacters);

            int paddingLength = Math.Max(0, Console.WindowWidth - this.textStartPosition.X - 1);
            Console.Write(visibleSubstring.PadRight(paddingLength));
        }

        /// <summary>
        /// Renders interactive choice buttons horizontally inside square brackets.
        /// </summary>
        public void RenderChoices()
        {
            if (!this.activeLine.HasChoices)
            {
                return;
            }

            Console.WriteLine();
            this.choiceStartPosition = Console.GetCursorPosition();
            this.isChoicesRendered = true;

            this.DrawChoices();
        }

        /// <summary>
        /// Commits the active speaker, text, and any selected choice into the history buffer.
        /// </summary>
        private void ArchiveCurrentLine()
        {
            if (string.IsNullOrWhiteSpace(this.activeLine.Text))
            {
                return;
            }

            string prefix = string.IsNullOrWhiteSpace(this.activeLine.Speaker) ? string.Empty : $"{this.activeLine.Speaker}: ";
            string dialogLine = $"{prefix}{this.activeLine.Text}";

            this.historyBuffer.Add((dialogLine, this.lastSelectedChoiceText));
            this.lastSelectedChoiceText = null;
        }

        /// <summary>
        /// Clears the console and redraws the scrollable history, evicting old lines if they exceed vertical space.
        /// </summary>
        private void RedrawHistoryAndLayout()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            while (this.CalculateTotalHistoryHeight() > MaxHistoryLines && this.historyBuffer.Count > 0)
            {
                this.historyBuffer.RemoveAt(0);
            }

            foreach ((string dialog, string? choice) in this.historyBuffer)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(dialog);

                if (!string.IsNullOrEmpty(choice))
                {
                    Console.WriteLine($"> {choice}");
                }
            }

            Console.ResetColor();

            if (this.historyBuffer.Count > 0)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Calculates the total vertical rows required to render the current history buffer,
        /// accounting for potential word-wrapping in standard terminals.
        /// </summary>
        private int CalculateTotalHistoryHeight()
        {
            int totalLines = 0;

            foreach ((string dialog, string? choice) in this.historyBuffer)
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
            if (!this.isChoicesRendered || !this.activeLine.HasChoices)
            {
                return;
            }

            Console.SetCursorPosition(this.choiceStartPosition.X, this.choiceStartPosition.Y);

            for (int i = 0; i < this.activeLine.Choices.Count; i++)
            {
                if (i == this.currentChoiceIndex)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.Write($"[{this.activeLine.Choices[i]}] ");
                Console.ResetColor();
            }

            Console.Write(string.Empty.PadRight(Math.Max(0, Console.WindowWidth - Console.CursorLeft)));
        }

        private void HandleSelection()
        {
            if (!this.isChoicesRendered)
            {
                return;
            }

            if (this.activeLine.HasChoices && this.currentChoiceIndex >= 0 && this.currentChoiceIndex < this.activeLine.Choices.Count)
            {
                this.lastSelectedChoiceText = this.activeLine.Choices[this.currentChoiceIndex];

                Console.SetCursorPosition(this.choiceStartPosition.X, this.choiceStartPosition.Y);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"> {this.lastSelectedChoiceText}".PadRight(Console.WindowWidth));
                Console.ResetColor();

                this.ChoiceSelected?.Invoke(this.currentChoiceIndex);
            }
        }

        private void HandleNavigateNext()
        {
            if (!this.isChoicesRendered || !this.activeLine.HasChoices)
            {
                return;
            }

            this.currentChoiceIndex = (this.currentChoiceIndex + 1) % this.activeLine.Choices.Count;
            this.DrawChoices();
        }

        private void HandleNavigatePrevious()
        {
            if (!this.isChoicesRendered || !this.activeLine.HasChoices)
            {
                return;
            }

            this.currentChoiceIndex = (this.currentChoiceIndex - 1 + this.activeLine.Choices.Count) % this.activeLine.Choices.Count;
            this.DrawChoices();
        }
    }
}