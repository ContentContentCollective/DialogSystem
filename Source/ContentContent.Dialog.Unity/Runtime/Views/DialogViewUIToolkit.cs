using System;
using System.Collections.Generic;
using ContentContent.Dialog.Ports;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Passive View implementation of <see cref="IDialogView"/> backed by Unity UI Toolkit.
    /// Operates on an injected root <see cref="VisualElement"/> to remain decoupled from scene components
    /// and support headless EditMode unit testing.
    /// 
    /// Two performance optimizations are enforced in this implementation:
    /// 1. Rich-Text Alpha Slicing (<alpha=#00>): Revealing characters via pure Substring(0, visibleCharacters) causes UI Toolkit's text layout engine to reflow words onto new lines mid-animation whenever a word crosses the container boundary. By inserting <alpha=#00> at the visibleCharacters index while keeping the complete string in the Label, UI Toolkit calculates word wrapping once on frame zero and reveals characters with zero layout reflow.
    /// 2. Pre-Allocated Choice Button Pooling: Dynamic Button instantiation during conversation turns triggers managed heap allocations and hierarchy dirtiness. DialogViewUIToolkit pre-allocates a pool of Button instances on construction and toggles DisplayStyle.Flex and DisplayStyle.None during RenderChoices().  
    /// </summary>
    public sealed class DialogViewUIToolkit : IDialogView, IDisposable
    {
        /// <summary>
        /// The UXML element name for the primary dialog root container.
        /// </summary>
        public const string DialogContainerName = "dialog-container";

        /// <summary>
        /// The UXML element name for the speaker nameplate container.
        /// </summary>
        public const string SpeakerContainerName = "dialog-speaker-container";

        /// <summary>
        /// The UXML element name for the speaker name label.
        /// </summary>
        public const string SpeakerLabelName = "dialog-speaker-label";

        /// <summary>
        /// The UXML element name for the dialog body text label.
        /// </summary>
        public const string BodyLabelName = "dialog-body-label";

        /// <summary>
        /// The UXML element name for the branching choices container.
        /// </summary>
        public const string ChoicesContainerName = "dialog-choices-container";

        /// <summary>
        /// The USS class applied to pooled choice buttons.
        /// </summary>
        public const string ChoiceButtonClassName = "dialog-choice-button";

        private const string HiddenAlphaTag = "<alpha=#00>";
        private const int DefaultChoicePoolCapacity = 4;

        private readonly VisualElement dialogContainer;
        private readonly VisualElement? speakerContainer;
        private readonly Label speakerLabel;
        private readonly Label bodyLabel;
        private readonly VisualElement choicesContainer;
        private readonly List<Button> choiceButtonPool;
        private readonly List<Action> choiceClickHandlers;

        private DialogLine boundLine;
        private bool isDisposed;

        /// <inheritdoc />
        public event Action<int>? ChoiceSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogViewUIToolkit"/> class.
        /// </summary>
        /// <param name="rootElement">The root <see cref="VisualElement"/> provided by a <c>PanelRenderer</c> or test harness.</param>
        /// <param name="initialChoicePoolSize">The number of choice buttons to pre-allocate in the visual tree.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rootElement"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when required named UXML elements are missing from <paramref name="rootElement"/>.</exception>
        public DialogViewUIToolkit(VisualElement rootElement, int initialChoicePoolSize = DefaultChoicePoolCapacity)
        {
            if (rootElement == null)
            {
                throw new ArgumentNullException(nameof(rootElement));
            }

            this.dialogContainer = rootElement.Q<VisualElement>(DialogContainerName)
                ?? throw new InvalidOperationException($"Required VisualElement '{DialogContainerName}' was not found in the provided UI hierarchy.");

            this.speakerContainer = rootElement.Q<VisualElement>(SpeakerContainerName);

            this.speakerLabel = rootElement.Q<Label>(SpeakerLabelName)
                ?? throw new InvalidOperationException($"Required Label '{SpeakerLabelName}' was not found in the provided UI hierarchy.");

            this.bodyLabel = rootElement.Q<Label>(BodyLabelName)
                ?? throw new InvalidOperationException($"Required Label '{BodyLabelName}' was not found in the provided UI hierarchy.");

            this.choicesContainer = rootElement.Q<VisualElement>(ChoicesContainerName)
                ?? throw new InvalidOperationException($"Required VisualElement '{ChoicesContainerName}' was not found in the provided UI hierarchy.");

            this.bodyLabel.enableRichText = true;

            int poolCapacity = Mathf.Max(0, initialChoicePoolSize);
            this.choiceButtonPool = new List<Button>(poolCapacity);
            this.choiceClickHandlers = new List<Action>(poolCapacity);

            for (int i = 0; i < poolCapacity; i++)
            {
                this.CreatePooledChoiceButton(i);
            }

            this.Hide();
        }

        /// <inheritdoc />
        public void Show()
        {
            this.dialogContainer.style.display = DisplayStyle.Flex;
        }

        /// <inheritdoc />
        public void Hide()
        {
            this.dialogContainer.style.display = DisplayStyle.None;
            this.HideAllChoices();
        }

        /// <inheritdoc />
        public void BindLine(DialogLine line)
        {
            this.boundLine = line;
            this.HideAllChoices();
        }

        /// <inheritdoc />
        public void RenderStaticLayout()
        {
            bool hasSpeaker = !string.IsNullOrWhiteSpace(this.boundLine.Speaker);
            this.speakerLabel.text = hasSpeaker ? this.boundLine.Speaker : string.Empty;

            if (this.speakerContainer != null)
            {
                this.speakerContainer.style.display = hasSpeaker ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                this.speakerLabel.style.display = hasSpeaker ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <inheritdoc />
        public void RevealCharacters(int visibleCharacters)
        {
            string fullText = this.boundLine.Text ?? string.Empty;

            if (fullText.Length == 0)
            {
                this.bodyLabel.text = string.Empty;
                return;
            }

            int clampedCount = Mathf.Clamp(visibleCharacters, 0, fullText.Length);

            if (clampedCount >= fullText.Length)
            {
                this.bodyLabel.text = fullText;
                return;
            }

            if (clampedCount == 0)
            {
                this.bodyLabel.text = string.Concat(HiddenAlphaTag, fullText);
                return;
            }

            this.bodyLabel.text = fullText.Insert(clampedCount, HiddenAlphaTag);
        }

        /// <inheritdoc />
        public void RenderChoices()
        {
            if (!this.boundLine.HasChoices)
            {
                this.HideAllChoices();
                return;
            }

            IReadOnlyList<string> choices = this.boundLine.Choices;
            int requiredCount = choices.Count;

            while (this.choiceButtonPool.Count < requiredCount)
            {
                this.CreatePooledChoiceButton(this.choiceButtonPool.Count);
            }

            this.choicesContainer.style.display = DisplayStyle.Flex;

            for (int i = 0; i < this.choiceButtonPool.Count; i++)
            {
                Button button = this.choiceButtonPool[i];

                if (i < requiredCount)
                {
                    button.text = choices[i];
                    button.style.display = DisplayStyle.Flex;
                }
                else
                {
                    button.style.display = DisplayStyle.None;
                }
            }

            if (requiredCount > 0)
            {
                this.choiceButtonPool[0].Focus();
            }
        }

        /// <summary>
        /// Unbinds all pooled button click delegates.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            for (int i = 0; i < this.choiceButtonPool.Count; i++)
            {
                this.choiceButtonPool[i].clicked -= this.choiceClickHandlers[i];
            }

            this.choiceButtonPool.Clear();
            this.choiceClickHandlers.Clear();
        }

        private void CreatePooledChoiceButton(int choiceIndex)
        {
            Button button = new Button
            {
                name = $"{ChoiceButtonClassName}-{choiceIndex}"
            };

            button.AddToClassList(ChoiceButtonClassName);
            button.style.display = DisplayStyle.None;

            Action clickHandler = () => this.ChoiceSelected?.Invoke(choiceIndex);

            button.clicked += clickHandler;

            this.choiceButtonPool.Add(button);
            this.choiceClickHandlers.Add(clickHandler);
            this.choicesContainer.Add(button);
        }

        private void HideAllChoices()
        {
            this.choicesContainer.style.display = DisplayStyle.None;

            for (int i = 0; i < this.choiceButtonPool.Count; i++)
            {
                this.choiceButtonPool[i].style.display = DisplayStyle.None;
            }
        }
    }
}