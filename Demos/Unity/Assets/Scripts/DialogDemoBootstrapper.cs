using ContentContent.Dialog.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.Dialog.Demo
{
    /// <summary>
    /// Sample scene composition root demonstrating programmatic C# <see cref="InputAction"/> injection
    /// and variable processing with <see cref="DialogBehavior"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogDemoBootstrapper : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("The DialogBehavior adapter component in the scene.")]
        [SerializeField]
        private DialogBehavior? dialogBehavior;

        [Tooltip("The sample DialogAsset to launch on start.")]
        [SerializeField]
        private DialogAsset? demoDialogAsset;

        [Header("Demo Configuration")]
        [Tooltip("If true, overrides the DialogBehavior Inspector input action with a programmatically injected InputAction.")]
        [SerializeField]
        private bool overrideInspectorInput;

        [Tooltip("Sample player name substituted into {PlayerName} tags.")]
        [SerializeField]
        private string playerName = "Adventurer";

        private InputAction? injectedAdvanceAction;

        private void Awake()
        {
            if (this.dialogBehavior == null)
            {
                Debug.LogError($"[{nameof(DialogDemoBootstrapper)}] {nameof(this.dialogBehavior)} reference is missing on '{this.gameObject.name}'.", this);
                return;
            }

            if (this.overrideInspectorInput)
            {
                // Simulates injecting an InputAction from a generated C# Input Action wrapper class
                this.injectedAdvanceAction = new InputAction(name: "Advance", type: InputActionType.Button);
                this.injectedAdvanceAction.AddBinding("<Keyboard>/space");
                this.injectedAdvanceAction.AddBinding("<Keyboard>/enter");

                this.dialogBehavior.ConfigureInput(this.injectedAdvanceAction, manageLifecycle: true);
            }
        }

        private void Start()
        {
            if (this.dialogBehavior != null && this.demoDialogAsset != null)
            {
                this.dialogBehavior.StartDialog(
                    this.demoDialogAsset,
                    textProcessor: rawText => rawText.Replace("{PlayerName}", this.playerName));
            }
        }

        private void OnDestroy()
        {
            this.injectedAdvanceAction?.Dispose();
            this.injectedAdvanceAction = null;
        }
    }
}