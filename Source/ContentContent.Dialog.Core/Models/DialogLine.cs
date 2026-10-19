using System;
using System.Collections.Generic;

namespace ContentContent.Dialog.Core.Models
{
    /// <summary>
    /// Immutable value payload representing a single dialog turn.
    /// Implemented as a readonly struct to prevent heap allocations during conversation progression.
    /// This ensures the presentation layer (Passive View) cannot mutate the narrative state.
    /// </summary>
    public readonly struct DialogLine
    {
        /// <summary>
        /// Gets the name of the speaker for this dialog line. 
        /// Guaranteed non-null; returns an empty string for narration.
        /// </summary>
        public string Speaker { get; }

        /// <summary>
        /// Gets the body text of the dialog line. Guaranteed non-null.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the read-only list of choice labels available on this turn. Guaranteed non-null.
        /// </summary>
        public IReadOnlyList<string> Choices { get; }

        /// <summary>
        /// Gets a value indicating whether this dialog line presents branching choices to the user.
        /// </summary>
        public bool HasChoices
        {
            get
            {
                return Choices != null && Choices.Count > 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogLine"/> struct.
        /// </summary>
        /// <param name="speaker">The speaker name. If null, defaults to an empty string.</param>
        /// <param name="text">The body text of the dialog line. Cannot be null.</param>
        /// <param name="choices">Optional list of branching choice labels. If null, defaults to an empty array.</param>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public DialogLine(string speaker, string text, IReadOnlyList<string> choices = null)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Choices = choices ?? Array.Empty<string>();
        }
    }
}