using System;
using ContentContent.Dialog.Ports;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace ContentContent.Dialog.Tests
{
    /// <summary>
    /// Unit tests verifying dependency validation and construction behavior for <see cref="DialogServiceBuilder"/>.
    /// </summary>
    [TestFixture]
    public sealed class DialogServiceBuilderTests
    {
        /// <summary>
        /// Verifies that building without configuring a view throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        [Test]
        public void Build_WithoutView_ThrowsInvalidOperationException()
        {
            DialogServiceBuilder builder = new DialogServiceBuilder()
                .WithInputProvider(Substitute.For<IDialogInputProvider>())
                .WithDelayStrategy(Substitute.For<IDelayStrategy>());

            Action act = () => builder.Build();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{nameof(IDialogView)}*");
        }

        /// <summary>
        /// Verifies that building without configuring an input provider throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        [Test]
        public void Build_WithoutInputProvider_ThrowsInvalidOperationException()
        {
            DialogServiceBuilder builder = new DialogServiceBuilder()
                .WithView(Substitute.For<IDialogView>())
                .WithDelayStrategy(Substitute.For<IDelayStrategy>());

            Action act = () => builder.Build();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{nameof(IDialogInputProvider)}*");
        }

        /// <summary>
        /// Verifies that building without configuring a delay strategy throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        [Test]
        public void Build_WithoutDelayStrategy_ThrowsInvalidOperationException()
        {
            DialogServiceBuilder builder = new DialogServiceBuilder()
                .WithView(Substitute.For<IDialogView>())
                .WithInputProvider(Substitute.For<IDialogInputProvider>());

            Action act = () => builder.Build();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{nameof(IDelayStrategy)}*");
        }

        /// <summary>
        /// Verifies that providing all required ports constructs a valid <see cref="IDialogService"/> instance.
        /// </summary>
        [Test]
        public void Build_WithAllRequiredPorts_ReturnsDialogServiceInstance()
        {
            using IDialogService service = new DialogServiceBuilder()
                .WithView(Substitute.For<IDialogView>())
                .WithInputProvider(Substitute.For<IDialogInputProvider>())
                .WithDelayStrategy(Substitute.For<IDelayStrategy>())
                .WithSettings(DialogSettings.Default)
                .Build();

            service.Should().NotBeNull();
            service.IsActive.Should().BeFalse();
        }
    }
}