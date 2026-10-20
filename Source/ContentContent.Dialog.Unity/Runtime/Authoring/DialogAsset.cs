using System;
using System.Collections.Generic;
using ContentContent.Dialog.Ports;
using UnityEngine;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Immutable <see cref="ScriptableObject"/> asset for authoring branching dialog graphs in the Unity Inspector.
    /// Acts as a factory producing isolated, stateful <see cref="IDialogSource"/> cursors per conversation session.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogAsset", menuName = "ContentContent/Dialog/Dialog Asset", order = 100)]
    public sealed class DialogAsset : ScriptableObject
    {
        [Tooltip("Ordered list of conversation nodes comprising this dialog graph.")]
        [SerializeField]
        private List<DialogNodeData> nodes = new List<DialogNodeData>();

        [NonSerialized]
        private DialogNode[]? cachedDomainNodes;

        /// <summary>
        /// Creates a new stateful <see cref="IDialogSource"/> cursor initialized at the start of this asset's dialog graph.
        /// </summary>
        /// <param name="textProcessor">Optional callback to evaluate variables or localization tags per line.</param>
        /// <returns>A fresh <see cref="IDialogSource"/> instance ready for <see cref="IDialogService.StartDialog"/>.</returns>
        public IDialogSource CreateSource(Func<string, string>? textProcessor = null)
        {
            if (this.cachedDomainNodes == null)
            {
                this.RebuildDomainCache();
            }

            return new DialogGraphSource(this.cachedDomainNodes!, startNodeIndex: 0, textProcessor: textProcessor);
        }

        private void OnValidate()
        {
            this.cachedDomainNodes = null;
        }

        private void RebuildDomainCache()
        {
            if (this.nodes == null || this.nodes.Count == 0)
            {
                this.cachedDomainNodes = Array.Empty<DialogNode>();
                return;
            }

            this.cachedDomainNodes = new DialogNode[this.nodes.Count];
            for (int i = 0; i < this.nodes.Count; i++)
            {
                this.cachedDomainNodes[i] = this.nodes[i].ToDomainModel();
            }
        }
    }
}