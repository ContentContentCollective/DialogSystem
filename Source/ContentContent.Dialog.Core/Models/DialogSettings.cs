using System;

namespace ContentContent.Dialog.Core.Models
{
    /// <summary>
    /// Immutable domain configuration defining typewriter cadence and input skip rules.
    /// Free of engine-specific types so the core orchestrator remains portable across runtimes.
    /// </summary>
    [Serializable]
    public readonly struct DialogSettings
    {
        /// <summary>
        /// The default number of seconds to delay before revealing the next character in the typewriter sequence.
        /// </summary>
        public const float DefaultSecondsPerCharacter = 0.03f;

        /// <summary>
        /// The default behavior for allowing the user to immediately skip the active typewriter sequence via input.
        /// </summary>
        public const bool DefaultAllowTypewriterSkip = true;

        /// <summary>
        /// Gets the time interval in seconds to wait before rendering each successive character in a line.
        /// </summary>
        public float SecondsPerCharacter { get; }

        /// <summary>
        /// Gets a value indicating whether an advancement input immediately reveals the full line when the typewriter is active.
        /// </summary>
        public bool AllowTypewriterSkip { get; }

        /// <summary>
        /// Gets a standard preconfigured instance of <see cref="DialogSettings"/>.
        /// </summary>
        public static DialogSettings Default
        {
            get
            {
                return new DialogSettings(DefaultSecondsPerCharacter, DefaultAllowTypewriterSkip);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogSettings"/> struct.
        /// </summary>
        /// <param name="secondsPerCharacter">The delay in seconds per character reveal. Negative values are clamped to 0f.</param>
        /// <param name="allowTypewriterSkip">True to permit users to immediately skip the typewriter sequence; otherwise false.</param>
        public DialogSettings(
            float secondsPerCharacter = DefaultSecondsPerCharacter,
            bool allowTypewriterSkip = DefaultAllowTypewriterSkip)
        {
            SecondsPerCharacter = Math.Max(0f, secondsPerCharacter);
            AllowTypewriterSkip = allowTypewriterSkip;
        }
    }
}