using System;
using UnityEngine;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Unity-serializable authoring data representing a branching choice within a <see cref="DialogAsset"/>.
    /// Converted into an immutable <see cref="DialogChoice"/> domain struct at runtime.
    /// </summary>
    [Serializable]
    public struct DialogChoiceData
    {
        [Tooltip("The localized display text presented on the choice button.")]
        [SerializeField]
        private string text;

        [Tooltip("The zero-based target node index in the dialog asset, or -1 to end the conversation.")]
        [SerializeField]
        private int targetNodeIndex;

        /// <summary>
        /// Gets the display text for this choice.
        /// </summary>
        public string Text
        {
            get
            {
                return this.text ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the target node index to transition to when selected.
        /// </summary>
        public int TargetNodeIndex
        {
            get
            {
                return this.targetNodeIndex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogChoiceData"/> struct.
        /// </summary>
        /// <param name="text">The choice display label.</param>
        /// <param name="targetNodeIndex">The target node index, or <see cref="DialogNode.EndConversationIndex"/>.</param>
        public DialogChoiceData(string text, int targetNodeIndex)
        {
            this.text = text ?? string.Empty;
            this.targetNodeIndex = targetNodeIndex;
        }

        /// <summary>
        /// Converts the serialized authoring data into an immutable <see cref="DialogChoice"/> domain model.
        /// </summary>
        /// <returns>The constructed <see cref="DialogChoice"/> struct.</returns>
        public DialogChoice ToDomainModel()
        {
            return new DialogChoice(this.Text, this.targetNodeIndex);
        }
    }
}