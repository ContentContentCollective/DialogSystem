using System;
using System.Collections.Generic;

namespace ContentContent.Dialog.Core.Interfaces
{
    /// <summary>
    /// Presentation contract for rendering dialog state (Passive View).
    /// Keeps the domain service decoupled from UI frameworks to allow isolated unit testing.
    /// </summary>
    public interface IDialogView
    {
        /// <summary>
        /// Occurs when the user selects a choice button. Passes the zero-based index of the selected choice.
        /// </summary>
        event Action<int> ChoiceSelected;

        /// <summary>
        /// Shows the dialog view container on screen.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the dialog view container and clears active presentation state.
        /// </summary>
        void Hide();

        /// <summary>
        /// Updates the speaker nameplate text, or hides the nameplate if the speaker name is empty.
        /// </summary>
        /// <param name="speakerName">The display name of the active speaker.</param>
        void SetSpeaker(string speakerName);

        /// <summary>
        /// Assigns the full target string upfront so the view can pre-calculate text layout and word wrapping.
        /// </summary>
        /// <param name="text">The complete dialog line text to prepare for rendering.</param>
        void SetFullText(string text);

        /// <summary>
        /// Reveals a specific count of characters from the pre-assigned full text without reflowing layout.
        /// </summary>
        /// <param name="characterCount">The number of visible characters to reveal from the start of the line.</param>
        void RevealCharacters(int characterCount);

        /// <summary>
        /// Renders interactive choice buttons for the provided choice labels.
        /// </summary>
        /// <param name="choices">Ordered list of localized choice strings to display.</param>
        void DisplayChoices(IReadOnlyList<string> choices);

        /// <summary>
        /// Removes all active choice buttons and hides the choices container.
        /// </summary>
        void ClearChoices();
    }
}