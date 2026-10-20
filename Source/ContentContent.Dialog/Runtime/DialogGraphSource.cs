using System;
using System.Collections.Generic;
using ContentContent.Dialog.Ports;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Engine-agnostic implementation of <see cref="IDialogSource"/> that traverses an in-memory list of <see cref="DialogNode"/> entries.
    /// Maintains isolated per-session cursor state so shared graph definitions remain immutable.
    /// </summary>
    public sealed class DialogGraphSource : IDialogSource
    {
        private readonly IReadOnlyList<DialogNode> nodes;
        private readonly Func<string, string>? textProcessor;

        private int currentNodeIndex;
        private int activeNodeIndex = DialogNode.EndConversationIndex;

        /// <inheritdoc />
        public bool CanContinue
        {
            get
            {
                return this.currentNodeIndex != DialogNode.EndConversationIndex
                    && this.currentNodeIndex >= 0
                    && this.currentNodeIndex < this.nodes.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogGraphSource"/> class.
        /// </summary>
        /// <param name="nodes">The ordered collection of dialog graph nodes.</param>
        /// <param name="startNodeIndex">The zero-based index of the entry node.</param>
        /// <param name="textProcessor">Optional delegate used to substitute variables or localize text before binding.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is <c>null</c>.</exception>
        public DialogGraphSource(
            IReadOnlyList<DialogNode> nodes,
            int startNodeIndex = 0,
            Func<string, string>? textProcessor = null)
        {
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            this.currentNodeIndex = this.nodes.Count > 0 ? startNodeIndex : DialogNode.EndConversationIndex;
            this.textProcessor = textProcessor;
        }

        /// <inheritdoc />
        public DialogLine GetNextLine()
        {
            if (!this.CanContinue)
            {
                throw new InvalidOperationException("Attempted to read beyond the bounds of the dialog graph.");
            }

            this.activeNodeIndex = this.currentNodeIndex;

            DialogNode rawNode = this.nodes[this.currentNodeIndex];
            this.currentNodeIndex = rawNode.NextNodeIndex;

            string evaluatedText = this.textProcessor != null
                ? this.textProcessor(rawNode.Text)
                : rawNode.Text;

            IReadOnlyList<DialogChoice> rawChoices = rawNode.Choices;
            string[] evaluatedChoices;

            if (rawChoices.Count > 0)
            {
                evaluatedChoices = new string[rawChoices.Count];
                for (int i = 0; i < rawChoices.Count; i++)
                {
                    string choiceText = rawChoices[i].Text;
                    evaluatedChoices[i] = this.textProcessor != null
                        ? this.textProcessor(choiceText)
                        : choiceText;
                }
            }
            else
            {
                evaluatedChoices = Array.Empty<string>();
            }

            return new DialogLine(rawNode.Speaker, evaluatedText, evaluatedChoices);
        }

        /// <inheritdoc />
        public void SelectChoice(int choiceIndex)
        {
            if (this.activeNodeIndex == DialogNode.EndConversationIndex || this.activeNodeIndex >= this.nodes.Count)
            {
                return;
            }

            DialogNode activeNode = this.nodes[this.activeNodeIndex];
            IReadOnlyList<DialogChoice> choices = activeNode.Choices;

            if (choiceIndex >= 0 && choiceIndex < choices.Count)
            {
                this.currentNodeIndex = choices[choiceIndex].TargetNodeIndex;
            }
        }
    }
}