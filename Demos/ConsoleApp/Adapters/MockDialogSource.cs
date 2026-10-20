using System;
using ContentContent.Dialog;
using ContentContent.Dialog.Ports;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// Terminal demo adapter wrapping <see cref="DialogGraphSource"/> to supply sample conversation nodes.
    /// </summary>
    public sealed class MockDialogSource : IDialogSource
    {
        private readonly IDialogSource innerSource;

        /// <inheritdoc />
        public bool CanContinue
        {
            get
            {
                return this.innerSource.CanContinue;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDialogSource"/> class.
        /// </summary>
        /// <param name="userName">The player name substituted into narrative placeholders.</param>
        public MockDialogSource(string userName = "???")
        {
            string resolvedName = string.IsNullOrWhiteSpace(userName) ? "???" : userName;

            DialogNode[] nodes = new DialogNode[]
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

            this.innerSource = new DialogGraphSource(
                nodes,
                startNodeIndex: 0,
                textProcessor: text => text.Replace("{PlayerName}", resolvedName));
        }

        /// <inheritdoc />
        public DialogLine GetNextLine()
        {
            return this.innerSource.GetNextLine();
        }

        /// <inheritdoc />
        public void SelectChoice(int choiceIndex)
        {
            this.innerSource.SelectChoice(choiceIndex);
        }
    }
}