using ContentContent.Dialog.Core.Models;
using System;

namespace ContentContent.Dialog.Core.Interfaces
{
    /// <summary>
    /// Abstracts the narrative data stream from the core presentation orchestrator.
    /// Implement this interface to adapt external dialog engines (such as Ink) without modifying core domain logic.
    /// </summary>
    public interface IDialogSource
    {
        /// <summary>
        /// Gets a value indicating whether the dialog source has a valid next node ready to be read.
        /// Returns false when the conversation reaches its end or is waiting for <see cref="SelectChoice"/> to be called.
        /// </summary>
        bool CanContinue { get; }

        /// <summary>
        /// Retrieves the next line of dialog, evaluates any internal variables/conditions, 
        /// and advances the internal narrative cursor.
        /// </summary>
        /// <returns>The immutable <see cref="DialogLine"/> payload for the current turn.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="CanContinue"/> is false.</exception>
        DialogLine GetNextLine();

        /// <summary>
        /// Routes the conversation cursor to the branch associated with the specified choice index on the active node.
        /// </summary>
        /// <param name="choiceIndex">Zero-based index of the selected choice option.</param>
        void SelectChoice(int choiceIndex);
    }
}