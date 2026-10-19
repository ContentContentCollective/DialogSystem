using System;

namespace ContentContent.Dialog.Core.Models
{
  /// <summary>
  /// Immutable routing entry pairing a localized choice label with its target node index in a dialog graph.
  /// Use <see cref="DialogNode.EndConversationIndex"/> as the target index to terminate the conversation upon selection.
  /// </summary>
  [Serializable]
  public readonly struct DialogChoice
  {
    /// <summary>
    /// Gets the localized display text for this choice. Guaranteed non-null.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the integer index of the target node to transition to when this choice is selected.
    /// </summary>
    public int TargetNodeIndex { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogChoice"/> struct.
    /// </summary>
    /// <param name="text">The localized display text for the choice. Cannot be <c>null</c>.</param>
    /// <param name="targetNodeIndex">The zero-based target node index in the dialog graph, or <see cref="DialogNode.EndConversationIndex"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <c>null</c>.</exception>
    public DialogChoice(string text, int targetNodeIndex)
    {
      Text = text ?? throw new ArgumentNullException(nameof(text));
      TargetNodeIndex = targetNodeIndex;
    }
  }
}