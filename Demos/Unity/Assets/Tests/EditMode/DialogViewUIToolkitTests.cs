using ContentContent.Dialog;
using ContentContent.Dialog.Unity;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace ContentContent.Dialog.Unity.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests verifying layout binding, rich-text alpha slicing, and button pooling in <see cref="DialogViewUIToolkit"/>.
    /// </summary>
    [TestFixture]
    public sealed class DialogViewUIToolkitTests
    {
        private VisualElement rootElement = null!;
        private DialogViewUIToolkit view = null!;

        /// <summary>
        /// Sets up a synthetic UI Toolkit visual tree matching required UXML element names.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.rootElement = new VisualElement();

            VisualElement container = new VisualElement { name = DialogViewUIToolkit.DialogContainerName };
            VisualElement speakerContainer = new VisualElement { name = DialogViewUIToolkit.SpeakerContainerName };
            Label speakerLabel = new Label { name = DialogViewUIToolkit.SpeakerLabelName };
            Label bodyLabel = new Label { name = DialogViewUIToolkit.BodyLabelName };
            VisualElement choicesContainer = new VisualElement { name = DialogViewUIToolkit.ChoicesContainerName };

            container.Add(speakerContainer);
            speakerContainer.Add(speakerLabel);
            container.Add(bodyLabel);
            container.Add(choicesContainer);
            this.rootElement.Add(container);

            this.view = new DialogViewUIToolkit(this.rootElement, initialChoicePoolSize: 2);
        }

        /// <summary>
        /// Disposes of the view after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.view.Dispose();
        }

        /// <summary>
        /// Verifies that BindLine and RenderStaticLayout correctly update label text and visibility,
        /// and that RevealCharacters inserts the rich-text alpha tag at the exact character index.
        /// </summary>
        [Test]
        public void BindAndRender_WithValidLine_UpdatesUIElementsWithAlphaTag()
        {
            DialogLine line = new DialogLine("Narrator", "Hello Unity");

            this.view.BindLine(line);
            this.view.RenderStaticLayout();
            this.view.RevealCharacters(5);

            Label speakerLabel = this.rootElement.Q<Label>(DialogViewUIToolkit.SpeakerLabelName);
            Label bodyLabel = this.rootElement.Q<Label>(DialogViewUIToolkit.BodyLabelName);

            speakerLabel.text.Should().Be("Narrator");
            bodyLabel.text.Should().Be("Hello<alpha=#00> Unity");
        }

        /// <summary>
        /// Verifies that RenderChoices reuses pre-allocated pooled buttons and dynamically expands the pool when needed.
        /// </summary>
        [Test]
        public void RenderChoices_WhenChoicesExceedInitialPool_ExpandsPoolAndDisplaysButtons()
        {
            DialogLine line = new DialogLine(
                "Narrator",
                "Make a choice",
                new[] { "Option A", "Option B", "Option C" });

            this.view.BindLine(line);
            this.view.RenderStaticLayout();
            this.view.RenderChoices();

            Button button0 = this.rootElement.Q<Button>($"{DialogViewUIToolkit.ChoiceButtonClassName}-0");
            Button button2 = this.rootElement.Q<Button>($"{DialogViewUIToolkit.ChoiceButtonClassName}-2");

            button0.text.Should().Be("Option A");
            button0.style.display.value.Should().Be(DisplayStyle.Flex);

            button2.Should().NotBeNull();
            button2.text.Should().Be("Option C");
            button2.style.display.value.Should().Be(DisplayStyle.Flex);
        }
    }
}