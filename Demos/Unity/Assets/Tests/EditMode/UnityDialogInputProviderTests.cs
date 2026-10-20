using ContentContent.Dialog.Unity;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace ContentContent.Dialog.Unity.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests verifying input lifecycle management and event propagation in <see cref="UnityDialogInputProvider"/>.
    /// </summary>
    [TestFixture]
    public sealed class UnityDialogInputProviderTests
    {
        private InputAction testAction = null!;
        private UnityDialogInputProvider inputProvider = null!;

        /// <summary>
        /// Sets up a test input action and provider instance.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.testAction = new InputAction("Advance", InputActionType.Button, "<Keyboard>/space");
            this.inputProvider = new UnityDialogInputProvider(this.testAction, manageActionLifecycle: true);
        }

        /// <summary>
        /// Disposes of the provider and test action after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.inputProvider.Dispose();
            this.testAction.Dispose();
        }

        /// <summary>
        /// Verifies that enabling and disabling the provider toggles the underlying InputAction state when lifecycle management is enabled.
        /// </summary>
        [Test]
        public void EnableAndDisable_WhenManagingLifecycle_TogglesInputActionEnabledState()
        {
            this.testAction.enabled.Should().BeFalse();

            this.inputProvider.Enable();
            this.testAction.enabled.Should().BeTrue();

            this.inputProvider.Disable();
            this.testAction.enabled.Should().BeFalse();
        }
    }
}