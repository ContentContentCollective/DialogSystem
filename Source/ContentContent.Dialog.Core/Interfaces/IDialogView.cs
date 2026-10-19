using ContentContent.Dialog.Core.Models;
using System;
using System.Collections.Generic;

namespace ContentContent.Dialog.Core.Interfaces
{
    /// <summary>
    /// Presentation contract for rendering dialog state following the Passive View (MVP) pattern.
    /// Utilizes atomic data binding to eliminate temporal coupling, followed by explicit visual execution commands.
    /// </summary>
    public interface IDialogView
    {
        /// <summary>
        /// Occurs when the user selects a choice button. Passes the zero-based index of the selected choice.
        /// </summary>
        event Action<int> ChoiceSelected;

        /// <summary>
        /// Displays the dialog view container on screen.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the dialog view container and clears active presentation state.
        /// </summary>
        void Hide();

        /// <summary>
        /// Atomically binds the complete dialog payload to the view.
        /// This acts as the lifecycle trigger for the view to archive previous history, 
        /// clear stale choice data, and pre-calculate upcoming layout requirements.
        /// </summary>
        /// <param name="line">The immutable dialog payload for the current turn.</param>
        void BindLine(DialogLine line);

        /// <summary>
        /// Triggers the visual rendering for static layout elements such as the speaker nameplate and container borders.
        /// </summary>
        void RenderStaticLayout();

        /// <summary>
        /// Reveals a specific count of characters from the bound text payload without reflowing the layout.
        /// </summary>
        /// <param name="visibleCharacters">The number of visible characters to reveal from the start of the line.</param>
        void RevealCharacters(int visibleCharacters);

        /// <summary>
        /// Renders interactive choice buttons if the bound payload contains choices.
        /// </summary>
        void RenderChoices();
    }
}