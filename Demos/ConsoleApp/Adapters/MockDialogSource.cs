using System;
using System.Collections.Generic;
using ContentContent.Dialog.Core.Interfaces;
using ContentContent.Dialog.Core.Models;

namespace ConsoleDemo.Adapters
{
    /// <summary>
    /// A temporary mock implementation of the narrative source supplying simple nodes for the demo.
    /// Demonstrates the architectural boundary where raw node data is converted 
    /// into an immutable presentation payload, and where metadata/variables are evaluated.
    /// </summary>
    public class MockDialogSource : IDialogSource
    {
        private readonly IReadOnlyList<DialogNode> _nodes;

        private string _userName;
        private int _currentNodeIndex = 0;
        private int _activeNodeIndex = -1;

        /// <summary>
        /// Gets a value indicating whether there is another node available to read.
        /// </summary>
        public bool CanContinue
        {
            get
            {
                return _currentNodeIndex != DialogNode.EndConversationIndex && _currentNodeIndex < _nodes.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDialogSource"/> class with dummy data.
        /// </summary>
        public MockDialogSource(string userName = "???")
        {
            _userName = userName;
            _nodes = new List<DialogNode>
            {
                // Node 0
                new DialogNode(
                    speaker: "ContentContent",
                    text: "Welcome, {PlayerName}. Press Enter/Space to advance.",
                    nextNodeIndex: 1),
                
                // Node 1
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
                    
                // Node 2
                new DialogNode(
                    speaker: "ContentContent",
                    text: "This is the result of selecting Choice 1.",
                    nextNodeIndex: 1),
                
                // Node 3
                new DialogNode(
                    speaker: "ContentContent",
                    text: "This is the result of selecting Choice 2.",
                    nextNodeIndex: 1),

                // Node 4
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
            if (!CanContinue)
            {
                throw new InvalidOperationException("Attempted to read beyond the bounds of the dialog graph.");
            }

            // Cache the active node so SelectChoice can reference its branching paths
            _activeNodeIndex = _currentNodeIndex;

            DialogNode rawNode = _nodes[_currentNodeIndex];
            _currentNodeIndex = rawNode.NextNodeIndex;

            // ARCHITECTURAL BOUNDARY:
            // 1. Evaluate Variables & Metadata
            // In a production system, this queries your global game state or localization database.
            string evaluatedText = rawNode.Text.Replace("{PlayerName}", _userName);

            // 2. Evaluate & Filter Choices
            List<string> evaluatedChoices = new List<string>();
            if (rawNode.Choices != null)
            {
                foreach (DialogChoice choice in rawNode.Choices)
                {
                    // If choices have condition IDs, evaluate them here. 
                    // Only add the text if the condition passes, completely hiding locked paths from the View.
                    evaluatedChoices.Add(choice.Text);
                }
            }

            // 3. Construct and return the immutable DTO
            return new DialogLine(rawNode.Speaker, evaluatedText, evaluatedChoices);
        }

        /// <summary>
        /// Routes the node cursor based on the chosen branch from the active node.
        /// </summary>
        /// <param name="choiceIndex">The index of the selected choice.</param>
        public void SelectChoice(int choiceIndex)
        {
            if (_activeNodeIndex == DialogNode.EndConversationIndex || _activeNodeIndex >= _nodes.Count)
            {
                return;
            }

            DialogNode activeNode = _nodes[_activeNodeIndex];

            // Because GetNextLine() filtered choices, you must ensure the choiceIndex 
            // maps correctly to the original raw choices if any were filtered out. 
            // For this mock without conditionals, a direct 1:1 index map is safe.
            if (activeNode.Choices != null && choiceIndex >= 0 && choiceIndex < activeNode.Choices.Count)
            {
                _currentNodeIndex = activeNode.Choices[choiceIndex].TargetNodeIndex;
            }
        }
    }
}