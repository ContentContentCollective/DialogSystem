using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Engine-agnostic graph node representing a single step in a branching conversation.
    /// Contains routing logic and acts as the structural blueprint before being evaluated into a presentation payload.
    /// </summary>
    [Serializable]
    public class DialogNode
    {
        /// <summary>
        /// Sentinel node index (-1) indicating that the conversation terminates after the active node or choice completes.
        /// </summary>
        public const int EndConversationIndex = -1;

        private readonly string speaker;
        private readonly string text;
        private readonly int nextNodeIndex;
        private readonly IReadOnlyList<DialogChoice> choices;

        /// <summary>
        /// Gets the speaker name for this node. Guaranteed non-null.
        /// </summary>
        public string Speaker
        {
            get
            {
                return this.speaker ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the body text for this node. Guaranteed non-null.
        /// </summary>
        public string Text
        {
            get
            {
                return this.text ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the index of the next node to transition to when no choices are present, or <see cref="EndConversationIndex"/> to terminate.
        /// </summary>
        public int NextNodeIndex
        {
            get
            {
                return this.nextNodeIndex;
            }
        }

        /// <summary>
        /// Gets the read-only list of branching choices attached to this node. Guaranteed non-null.
        /// </summary>
        public IReadOnlyList<DialogChoice> Choices
        {
            get
            {
                return this.choices ?? Array.Empty<DialogChoice>();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogNode"/> class.
        /// </summary>
        /// <param name="speaker">The speaker name. Null values are sanitized to <see cref="string.Empty"/>.</param>
        /// <param name="text">The dialog body text. Cannot be <c>null</c>.</param>
        /// <param name="nextNodeIndex">The target node index for linear progression, or <see cref="EndConversationIndex"/>.</param>
        /// <param name="choices">Optional collection of branching choices for this node.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <c>null</c>.</exception>
        public DialogNode(
            string? speaker,
            string text,
            int nextNodeIndex = EndConversationIndex,
            IReadOnlyList<DialogChoice>? choices = null)
        {
            this.speaker = speaker ?? string.Empty;
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.nextNodeIndex = nextNodeIndex;
            this.choices = choices ?? Array.Empty<DialogChoice>();
        }
    }
}