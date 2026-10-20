using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace ContentContent.Dialog.Tests
{
    /// <summary>
    /// Unit tests verifying stateful cursor traversal and variable substitution in <see cref="DialogGraphSource"/>.
    /// </summary>
    [TestFixture]
    public sealed class DialogGraphSourceTests
    {
        /// <summary>
        /// Verifies that the graph source traverses nodes linearly and evaluates text processors correctly.
        /// </summary>
        [Test]
        public void GetNextLine_WithLinearGraph_TraversesNodesAndAppliesTextProcessor()
        {
            List<DialogNode> nodes = new List<DialogNode>
            {
                new DialogNode("SpeakerA", "Hello {User}", nextNodeIndex: 1),
                new DialogNode("SpeakerB", "Goodbye {User}", nextNodeIndex: DialogNode.EndConversationIndex)
            };

            DialogGraphSource source = new DialogGraphSource(
                nodes,
                startNodeIndex: 0,
                textProcessor: text => text.Replace("{User}", "???"));

            source.CanContinue.Should().BeTrue();
            DialogLine line1 = source.GetNextLine();
            line1.Speaker.Should().Be("SpeakerA");
            line1.Text.Should().Be("Hello ???");
            line1.HasChoices.Should().BeFalse();

            source.CanContinue.Should().BeTrue();
            DialogLine line2 = source.GetNextLine();
            line2.Speaker.Should().Be("SpeakerB");
            line2.Text.Should().Be("Goodbye ???");

            source.CanContinue.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that selecting a choice correctly routes the graph cursor to the target node index.
        /// </summary>
        [Test]
        public void SelectChoice_WhenCalled_RoutesToTargetNode()
        {
            List<DialogNode> nodes = new List<DialogNode>
            {
                new DialogNode("Narrator", "Choose path", nextNodeIndex: DialogNode.EndConversationIndex, choices: new[]
                {
                    new DialogChoice("Path A", targetNodeIndex: 1),
                    new DialogChoice("Path B", targetNodeIndex: 2)
                }),
                new DialogNode("Narrator", "Reached Path A", nextNodeIndex: DialogNode.EndConversationIndex),
                new DialogNode("Narrator", "Reached Path B", nextNodeIndex: DialogNode.EndConversationIndex)
            };

            DialogGraphSource source = new DialogGraphSource(nodes, startNodeIndex: 0);

            source.CanContinue.Should().BeTrue();
            DialogLine line1 = source.GetNextLine();
            line1.HasChoices.Should().BeTrue();
            line1.Choices.Count.Should().Be(2);

            source.SelectChoice(1); // Select Path B (targetNodeIndex: 2)

            source.CanContinue.Should().BeTrue();
            DialogLine line2 = source.GetNextLine();
            line2.Text.Should().Be("Reached Path B");
        }
    }
}