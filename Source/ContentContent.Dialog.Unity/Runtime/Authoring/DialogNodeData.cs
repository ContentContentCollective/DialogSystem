using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Unity-serializable authoring data representing a single conversation node within a <see cref="DialogAsset"/>.
    /// Converted into an immutable <see cref="DialogNode"/> domain instance at runtime.
    /// </summary>
    [Serializable]
    public struct DialogNodeData
    {
        [Tooltip("The speaker name displayed in the nameplate. Leave empty for narration.")]
        [SerializeField]
        private string speaker;

        [Tooltip("The body text of the dialog line.")]
        [TextArea(3, 6)]
        [SerializeField]
        private string text;

        [Tooltip("The next node index for linear progression when no choices exist, or -1 to end the conversation.")]
        [SerializeField]
        private int nextNodeIndex;

        [Tooltip("Optional branching choices presented at the end of this line.")]
        [SerializeField]
        private DialogChoiceData[] choices;

        /// <summary>
        /// Gets the speaker name for this node.
        /// </summary>
        public string Speaker
        {
            get
            {
                return this.speaker ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the body text for this node.
        /// </summary>
        public string Text
        {
            get
            {
                return this.text ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the linear next node index.
        /// </summary>
        public int NextNodeIndex
        {
            get
            {
                return this.nextNodeIndex;
            }
        }

        /// <summary>
        /// Gets the read-only list of serialized choice data entries.
        /// </summary>
        public IReadOnlyList<DialogChoiceData> Choices
        {
            get
            {
                return this.choices ?? Array.Empty<DialogChoiceData>();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogNodeData"/> struct.
        /// </summary>
        /// <param name="speaker">The speaker name.</param>
        /// <param name="text">The dialog body text.</param>
        /// <param name="nextNodeIndex">The next node index for linear progression.</param>
        /// <param name="choices">The branching choices array.</param>
        public DialogNodeData(
            string speaker,
            string text,
            int nextNodeIndex = DialogNode.EndConversationIndex,
            DialogChoiceData[]? choices = null)
        {
            this.speaker = speaker ?? string.Empty;
            this.text = text ?? string.Empty;
            this.nextNodeIndex = nextNodeIndex;
            this.choices = choices ?? Array.Empty<DialogChoiceData>();
        }

        /// <summary>
        /// Converts the serialized authoring data into an immutable <see cref="DialogNode"/> domain model.
        /// </summary>
        /// <returns>The constructed <see cref="DialogNode"/> instance.</returns>
        public DialogNode ToDomainModel()
        {
            DialogChoice[] domainChoices;

            if (this.choices != null && this.choices.Length > 0)
            {
                domainChoices = new DialogChoice[this.choices.Length];
                for (int i = 0; i < this.choices.Length; i++)
                {
                    domainChoices[i] = this.choices[i].ToDomainModel();
                }
            }
            else
            {
                domainChoices = Array.Empty<DialogChoice>();
            }

            return new DialogNode(this.Speaker, this.Text, this.nextNodeIndex, domainChoices);
        }
    }
}