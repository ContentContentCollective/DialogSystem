using System;
using System.Collections.Generic;

namespace ContentContent.Dialog.Core.Models
{
    /// <summary>
    /// Engine-agnostic graph node representing a single step in a branching conversation.
    /// Converts into an immutable <see cref="DialogLine"/> payload on demand.
    /// </summary>
    [Serializable]
    public readonly struct DialogNode
    {
        /// <summary>
        /// Sentinel node index (-1) indicating that the conversation terminates after the active node.
        /// </summary>
        public const int EndConversationIndex = -1;

        private readonly string _speaker;
        private readonly string _text;
        private readonly int _nextNodeIndex;
        private readonly IReadOnlyList<DialogChoice> _choices;

        /// <summary>
        /// Gets the speaker name for this node. Guaranteed non-null.
        /// </summary>
        public string Speaker
        {
            get
            {
                return _speaker ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the body text for this node. Guaranteed non-null.
        /// </summary>
        public string Text
        {
            get
            {
                return _text ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the index of the next node to transition to when no choices are present.
        /// </summary>
        public int NextNodeIndex
        {
            get
            {
                return _nextNodeIndex;
            }
        }

        /// <summary>
        /// Gets the read-only list of branching choices attached to this node. Guaranteed non-null.
        /// </summary>
        public IReadOnlyList<DialogChoice> Choices
        {
            get
            {
                return _choices ?? Array.Empty<DialogChoice>();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogNode"/> struct.
        /// </summary>
        /// <param name="speaker">The speaker name.</param>
        /// <param name="text">The dialog body text. Cannot be null.</param>
        /// <param name="nextNodeIndex">The target node index for linear progression.</param>
        /// <param name="choices">Optional collection of branching choices for this node.</param>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public DialogNode(
            string speaker,
            string text,
            int nextNodeIndex = EndConversationIndex,
            IReadOnlyList<DialogChoice> choices = null)
        {
            _speaker = speaker ?? string.Empty;
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _nextNodeIndex = nextNodeIndex;
            _choices = choices ?? Array.Empty<DialogChoice>();
        }

        /// <summary>
        /// Constructs the runtime <see cref="DialogLine"/> value payload from this node's data.
        /// </summary>
        /// <returns>A new <see cref="DialogLine"/> containing this node's speaker, text, and extracted choice labels.</returns>
        public DialogLine ToDialogLine()
        {
            if (_choices == null || _choices.Count == 0)
            {
                return new DialogLine(Speaker, Text);
            }

            string[] choiceLabels = new string[_choices.Count];
            for (int i = 0; i < _choices.Count; i++)
            {
                choiceLabels[i] = _choices[i].Text;
            }

            return new DialogLine(Speaker, Text, choiceLabels);
        }
    }
}