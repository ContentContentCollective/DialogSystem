using System;
using ContentContent.Dialog.Ports;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Standard, extensible <see cref="MonoBehaviour"/> adapter connecting a <see cref="PanelRenderer"/>,
    /// the Unity Input System, and the pure C# <see cref="IDialogService"/> orchestrator.
    /// Works out of the box via Inspector configuration or via programmatic C# <see cref="InputAction"/> injection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public class DialogBehavior : MonoBehaviour
    {
        [Header("UI Configuration")]
        [Tooltip("The UI Toolkit PanelRenderer hosting the dialog UXML visual tree.")]
        [SerializeField]
        private PanelRenderer? panelRenderer;

        [Header("Input Configuration")]
        [Tooltip("Default input action used to advance or skip dialog when not overridden via code injection.")]
        [SerializeField]
        private InputActionProperty advanceActionProperty = new InputActionProperty(
            new InputAction(name: "Advance", type: InputActionType.Button));

        [Tooltip("Whether the input provider should automatically Enable and Disable the underlying InputAction.")]
        [SerializeField]
        private bool manageActionLifecycle = true;

        [Header("Typewriter Settings")]
        [Tooltip("Delay in seconds between each revealed character.")]
        [Min(0f)]
        [SerializeField]
        private float secondsPerCharacter = DialogSettings.DefaultSecondsPerCharacter;

        [Tooltip("Whether the advance input immediately reveals the full line while typewriting.")]
        [SerializeField]
        private bool allowTypewriterSkip = DialogSettings.DefaultAllowTypewriterSkip;

        [Header("Optional Initial Content")]
        [Tooltip("Optional DialogAsset to play automatically when StartOnReady is enabled or StartInitialDialog() is invoked.")]
        [SerializeField]
        private DialogAsset? initialDialogAsset;

        [Tooltip("If true, automatically starts the assigned Initial Dialog Asset as soon as the UI PanelRenderer loads.")]
        [SerializeField]
        private bool startOnReady;

        private VisualElement? activeRootElement;
        private InputAction? injectedAdvanceAction;
        private IDialogView? activeView;
        private IDialogInputProvider? activeInputProvider;
        private IDialogService? dialogService;
        private IDialogSource? pendingSource;

        /// <summary>
        /// Occurs when a dialog session starts.
        /// </summary>
        public event Action? DialogStarted;

        /// <summary>
        /// Occurs when a dialog session ends.
        /// </summary>
        public event Action? DialogEnded;

        /// <summary>
        /// Gets a value indicating whether the underlying <see cref="IDialogService"/> has been constructed and bound to a live UI tree.
        /// </summary>
        public bool IsReady
        {
            get
            {
                return this.dialogService != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a dialog session is currently active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return this.dialogService != null && this.dialogService.IsActive;
            }
        }

        /// <summary>
        /// Gets the active <see cref="IDialogService"/> instance, or <c>null</c> if the UI panel has not finished loading.
        /// </summary>
        protected IDialogService? Service
        {
            get
            {
                return this.dialogService;
            }
        }

        /// <summary>
        /// Overrides the serialized Inspector input action with an injected <see cref="InputAction"/> from a generated C# input class.
        /// If the UI is already loaded, the internal service is immediately rebuilt with the new input action.
        /// </summary>
        /// <param name="advanceAction">The strongly-typed input action used to advance dialog.</param>
        /// <param name="manageLifecycle">Whether the provider should enable and disable the action automatically.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="advanceAction"/> is <c>null</c>.</exception>
        public void ConfigureInput(InputAction advanceAction, bool manageLifecycle = true)
        {
            this.injectedAdvanceAction = advanceAction ?? throw new ArgumentNullException(nameof(advanceAction));
            this.manageActionLifecycle = manageLifecycle;

            if (this.activeRootElement != null)
            {
                this.RebuildService(this.activeRootElement);
            }
        }

        /// <summary>
        /// Starts a dialog session using the serialized <see cref="initialDialogAsset"/>.
        /// If the <see cref="PanelRenderer"/> has not completed its initial UI load, the source is queued and started automatically on load.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no <see cref="initialDialogAsset"/> is assigned.</exception>
        public void StartInitialDialog()
        {
            if (this.initialDialogAsset == null)
            {
                throw new InvalidOperationException($"No {nameof(this.initialDialogAsset)} is assigned on '{this.gameObject.name}'.");
            }

            this.StartDialog(this.initialDialogAsset);
        }

        /// <summary>
        /// Starts a dialog session from the specified <see cref="DialogAsset"/>.
        /// </summary>
        /// <param name="asset">The dialog asset to play.</param>
        /// <param name="textProcessor">Optional text variable processor.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="asset"/> is <c>null</c>.</exception>
        public void StartDialog(DialogAsset asset, Func<string, string>? textProcessor = null)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            this.StartDialog(asset.CreateSource(textProcessor));
        }

        /// <summary>
        /// Starts a dialog session from any custom <see cref="IDialogSource"/>.
        /// If the <see cref="PanelRenderer"/> visual tree is not yet ready, the source is queued until the reload callback fires.
        /// </summary>
        /// <param name="source">The dialog source driving the conversation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
        public void StartDialog(IDialogSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (this.dialogService == null)
            {
                this.pendingSource = source;
                return;
            }

            this.pendingSource = null;
            this.dialogService.StartDialog(source);
        }

        /// <summary>
        /// Stops the currently active dialog session and clears any pending queued source.
        /// </summary>
        public void StopDialog()
        {
            this.pendingSource = null;
            this.dialogService?.StopDialog();
        }

        /// <summary>
        /// Factory method for constructing the <see cref="IDialogView"/> implementation when the UI visual tree reloads.
        /// Override in subclasses to provide a custom view implementation or custom choice pool size.
        /// </summary>
        /// <param name="rootElement">The root visual element supplied by the <see cref="PanelRenderer"/>.</param>
        /// <returns>The initialized <see cref="IDialogView"/> adapter.</returns>
        protected virtual IDialogView CreateView(VisualElement rootElement)
        {
            return new DialogViewUIToolkit(rootElement);
        }

        /// <summary>
        /// Factory method for constructing the <see cref="IDialogInputProvider"/> implementation.
        /// Override in subclasses to provide a custom input adapter.
        /// </summary>
        /// <param name="advanceAction">The resolved <see cref="InputAction"/> for dialog advancement.</param>
        /// <param name="manageLifecycle">Whether the provider manages enabling/disabling the action.</param>
        /// <returns>The initialized <see cref="IDialogInputProvider"/> adapter.</returns>
        protected virtual IDialogInputProvider CreateInputProvider(InputAction advanceAction, bool manageLifecycle)
        {
            return new UnityDialogInputProvider(advanceAction, manageLifecycle);
        }

        /// <summary>
        /// Factory method for constructing the <see cref="IDelayStrategy"/> implementation.
        /// Override in subclasses to inject custom timing (e.g., UniTask).
        /// </summary>
        /// <returns>The initialized <see cref="IDelayStrategy"/> adapter.</returns>
        protected virtual IDelayStrategy CreateDelayStrategy()
        {
            return new AwaitableDelayStrategy();
        }

        /// <summary>
        /// Invoked when a dialog session starts. Override in subclasses to add custom presentation or audio hooks.
        /// </summary>
        protected virtual void OnDialogStarted()
        {
            this.DialogStarted?.Invoke();
        }

        /// <summary>
        /// Invoked when a dialog session ends. Override in subclasses to add custom cleanup or gameplay state transitions.
        /// </summary>
        protected virtual void OnDialogEnded()
        {
            this.DialogEnded?.Invoke();
        }

        /// <summary>
        /// Populates default Inspector references and default keyboard/gamepad bindings when the component is first attached.
        /// </summary>
        protected virtual void Reset()
        {
            this.panelRenderer = this.GetComponent<PanelRenderer>();

            InputAction defaultAction = new InputAction(name: "Advance", type: InputActionType.Button);
            defaultAction.AddBinding("<Keyboard>/space");
            defaultAction.AddBinding("<Keyboard>/enter");
            defaultAction.AddBinding("<Gamepad>/buttonSouth");

            this.advanceActionProperty = new InputActionProperty(defaultAction);
        }

        /// <summary>
        /// Resolves the <see cref="PanelRenderer"/> reference on initialization.
        /// </summary>
        protected virtual void Awake()
        {
            if (this.panelRenderer == null)
            {
                this.panelRenderer = this.GetComponent<PanelRenderer>();
            }
        }

        /// <summary>
        /// Registers the UI Toolkit reload callback and queues the initial dialog asset if <see cref="startOnReady"/> is enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (this.panelRenderer == null)
            {
                this.panelRenderer = this.GetComponent<PanelRenderer>();
            }

            if (this.startOnReady && this.initialDialogAsset != null && this.pendingSource == null && !this.IsActive)
            {
                this.pendingSource = this.initialDialogAsset.CreateSource();
            }

            if (this.panelRenderer != null)
            {
                this.panelRenderer.RegisterUIReloadCallback(this.HandleUIReload);
            }
        }

        /// <summary>
        /// Unregisters the UI Toolkit reload callback and tears down the active service instance when disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (this.panelRenderer != null)
            {
                this.panelRenderer.UnregisterUIReloadCallback(this.HandleUIReload);
            }

            this.activeRootElement = null;
            this.CleanUpService();
        }

        /// <summary>
        /// Ensures all disposable adapters and event subscriptions are released when the component is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            this.CleanUpService();
        }

        private void HandleUIReload(PanelRenderer renderer, VisualElement rootElement, int version)
        {
            this.activeRootElement = rootElement;

            if (rootElement == null)
            {
                this.CleanUpService();
                return;
            }

            this.RebuildService(rootElement);
        }

        private void RebuildService(VisualElement rootElement)
        {
            this.CleanUpService();

            InputAction? resolvedAction = this.injectedAdvanceAction ?? this.advanceActionProperty.action;
            if (resolvedAction == null)
            {
                Debug.LogWarning(
                    $"[{nameof(DialogBehavior)}] No Advance InputAction is configured on '{this.gameObject.name}'. Assign an action in the Inspector or call {nameof(this.ConfigureInput)}.",
                    this);
                return;
            }

            this.activeView = this.CreateView(rootElement);
            this.activeInputProvider = this.CreateInputProvider(resolvedAction, this.manageActionLifecycle);
            IDelayStrategy delayStrategy = this.CreateDelayStrategy();
            DialogSettings settings = new DialogSettings(this.secondsPerCharacter, this.allowTypewriterSkip);

            this.dialogService = new DialogServiceBuilder()
                .WithView(this.activeView)
                .WithInputProvider(this.activeInputProvider)
                .WithDelayStrategy(delayStrategy)
                .WithSettings(settings)
                .Build();

            this.dialogService.DialogStarted += this.OnDialogStarted;
            this.dialogService.DialogEnded += this.OnDialogEnded;

            if (this.pendingSource != null)
            {
                IDialogSource sourceToStart = this.pendingSource;
                this.pendingSource = null;
                this.dialogService.StartDialog(sourceToStart);
            }
        }

        private void CleanUpService()
        {
            if (this.dialogService != null)
            {
                this.dialogService.DialogStarted -= this.OnDialogStarted;
                this.dialogService.DialogEnded -= this.OnDialogEnded;
                this.dialogService.Dispose();
                this.dialogService = null;
            }

            this.activeInputProvider?.Dispose();
            this.activeInputProvider = null;

            if (this.activeView is IDisposable disposableView)
            {
                disposableView.Dispose();
            }

            this.activeView = null;
        }
    }
}