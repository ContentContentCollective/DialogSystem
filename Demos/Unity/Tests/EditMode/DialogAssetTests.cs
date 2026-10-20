using ContentContent.Dialog;
using ContentContent.Dialog.Ports;
using ContentContent.Dialog.Unity;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace ContentContent.Dialog.Unity.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests verifying that <see cref="DialogAsset"/> produces isolated, stateful <see cref="IDialogSource"/> instances.
    /// </summary>
    [TestFixture]
    public sealed class DialogAssetTests
    {
        private DialogAsset asset = null!;

        /// <summary>
        /// Creates an in-memory <see cref="DialogAsset"/> instance prior to each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.asset = ScriptableObject.CreateInstance<DialogAsset>();
        }

        /// <summary>
        /// Destroys the temporary <see cref="DialogAsset"/> instance after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(this.asset);
        }

        /// <summary>
        /// Verifies that calling CreateSource multiple times returns distinct cursor instances that do not share state.
        /// </summary>
        [Test]
        public void CreateSource_WhenCalledMultipleTimes_ReturnsIndependentSources()
        {
            IDialogSource firstSource = this.asset.CreateSource();
            IDialogSource secondSource = this.asset.CreateSource();

            firstSource.Should().NotBeNull();
            secondSource.Should().NotBeNull();
            firstSource.Should().NotBeSameAs(secondSource);
            firstSource.CanContinue.Should().BeFalse();
        }
    }
}