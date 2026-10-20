using System;
using System.Collections.Generic;
using ContentContent.Dialog;
using ContentContent.Dialog.Ports;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// A temporary mock implementation of the narrative source supplying simple nodes for the demo.
    /// Demonstrates the architectural boundary where raw node data is converted
    /// into an immutable presentation payload, and where metadata/variables are evaluated.
    /// </summary>
    public sealed class MockDialogSource : IDialogSource
    {
        private readonly IReadOnlyList<DialogNode> nodes;
        private readonly string userName;

        private int currentNodeIndex;
        private int activeNodeIndex = -1;

        /// <summary>
        /// Gets a value indicating whether there is another node available to read.
        /// </summary>
        public bool CanContinue
        {
            get
            {
                return this.currentNodeIndex != DialogNode.EndConversationIndex && this.currentNodeIndex < this.nodes.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDialogSource"/> class with dummy data.
        /// </summary>
        /// <param name="userName">The player name substituted into variable placeholders.</param>
        public MockDialogSource(string userName = "???")
        {
            this.userName = string.IsNullOrWhiteSpace(userName) ? "???" : userName;
            this.nodes = new List<DialogNode>
            {
                new DialogNode(
                    speaker: "ContentContent",
                    text: "Welcome, {PlayerName}. Press Enter/Space to advance.",
                    nextNodeIndex: 1),

                new DialogNode(
                    speaker: "ContentContent",
                    text: "Here are some choices. Use the arrow keys to navigate and Enter/Space to select.",
                    nextNodeIndex: DialogNode.EndConversationIndex,
                    choices: new[]
                    {
                        new DialogChoice("Choice 1.", 2),
                        new DialogChoice("Choice 2.", 3),
                        new DialogChoice("Restart this conversation.", 0),
                        new DialogChoice("End this conversation.", 4)
                    }),

                new DialogNode(
                    speaker: "ContentContent",
                    text: "This is the result of selecting Choice 1.",
                    nextNodeIndex: 1),

                new DialogNode(
                    speaker: "ContentContent",
                    text: "This is the result of selecting Choice 2.",
                    nextNodeIndex: 1),

                new DialogNode(
                    speaker: "ContentContent",
                    text: "Goodbye, {PlayerName}.",
                    nextNodeIndex: DialogNode.EndConversationIndex)
            };
        }

        /// <summary>
        /// Retrieves the active node, evaluates runtime state (metadata/variables),
        /// and constructs the immutable presentation payload.
        /// </summary>
        /// <returns>The immutable line payload.</returns>
        /// <exception cref="InvalidOperationException">Thrown if attempting to read past the end of the graph.</exception>
        public DialogLine GetNextLine()
        {
            if (!this.CanContinue)
            {
                throw new InvalidOperationException("Attempted to read beyond the bounds of the dialog graph.");
            }

            this.activeNodeIndex = this.currentNodeIndex;

            DialogNode rawNode = this.nodes[this.currentNodeIndex];
            this.currentNodeIndex = rawNode.NextNodeIndex;

            string evaluatedText = rawNode.Text.Replace("{PlayerName}", this.userName);

            List<string> evaluatedChoices = new List<string>();
            if (rawNode.Choices != null)
            {
                foreach (DialogChoice choice in rawNode.Choices)
                {
                    evaluatedChoices.Add(choice.Text);
                }
            }

            return new DialogLine(rawNode.Speaker, evaluatedText, evaluatedChoices);
        }

        /// <summary>
        /// Routes the node cursor based on the chosen branch from the active node.
        /// </summary>
        /// <param name="choiceIndex">The index of the selected choice.</param>
        public void SelectChoice(int choiceIndex)
        {
            if (this.activeNodeIndex == DialogNode.EndConversationIndex || this.activeNodeIndex >= this.nodes.Count)
            {
                return;
            }

            DialogNode activeNode = this.nodes[this.activeNodeIndex];

            if (activeNode.Choices != null && choiceIndex >= 0 && choiceIndex < activeNode.Choices.Count)
            {
                this.currentNodeIndex = activeNode.Choices[choiceIndex].TargetNodeIndex;
            }
        }
    }
}