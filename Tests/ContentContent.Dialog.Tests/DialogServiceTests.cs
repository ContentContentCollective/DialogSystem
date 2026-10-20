using System;
using System.Threading;
using System.Threading.Tasks;
using ContentContent.Dialog.Ports;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace ContentContent.Dialog.Tests
{
    /// <summary>
    /// Unit tests verifying conversation lifecycle, typewriter skipping, and choice routing in <see cref="DialogService"/>.
    /// </summary>
    [TestFixture]
    public sealed class DialogServiceTests
    {
        private IDialogView view = null!;
        private IDialogInputProvider inputProvider = null!;
        private IDelayStrategy delayStrategy = null!;
        private IDialogSource source = null!;

        /// <summary>
        /// Initializes fresh NSubstitute mocks prior to each test execution.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.view = Substitute.For<IDialogView>();
            this.inputProvider = Substitute.For<IDialogInputProvider>();
            this.delayStrategy = Substitute.For<IDelayStrategy>();
            this.source = Substitute.For<IDialogSource>();
        }

        /// <summary>
        /// Disposes of any disposable test doubles after each test execution.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.inputProvider.Dispose();
        }

        /// <summary>
        /// Verifies that starting a dialog enables input, shows the view, binds the first line, and reveals characters.
        /// </summary>
        [Test]
        public void StartDialog_WithValidSource_BindsLineAndShowsView()
        {
            this.delayStrategy
                .DelayAsync(Arg.Any<float>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            DialogLine expectedLine = new DialogLine("Speaker", "Hi");
            this.source.CanContinue.Returns(true);
            this.source.GetNextLine().Returns(expectedLine);

            using DialogService service = new DialogService(
                this.view,
                this.inputProvider,
                this.delayStrategy,
                new DialogSettings(secondsPerCharacter: 0f, allowTypewriterSkip: true));

            bool startedEventFired = false;
            service.DialogStarted += () => startedEventFired = true;

            service.StartDialog(this.source);

            service.IsActive.Should().BeTrue();
            startedEventFired.Should().BeTrue();
            this.inputProvider.Received(1).Enable();
            this.view.Received(1).Show();
            this.view.Received(1).BindLine(expectedLine);
            this.view.Received(1).RenderStaticLayout();
            this.view.Received().RevealCharacters(expectedLine.Text.Length);
        }

        /// <summary>
        /// Verifies that requesting an advance while the typewriter is active and skipping is enabled
        /// immediately cancels the delay and reveals the full line without advancing to the next node.
        /// </summary>
        [Test]
        public void AdvanceRequested_WhileTypewritingAndSkipAllowed_RevealsFullLineImmediately()
        {
            TaskCompletionSource<bool> delayBlocker = new TaskCompletionSource<bool>();

            this.delayStrategy
                .DelayAsync(Arg.Any<float>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    CancellationToken token = callInfo.Arg<CancellationToken>();
                    token.Register(() => delayBlocker.TrySetCanceled(token));
                    return delayBlocker.Task;
                });

            DialogLine line = new DialogLine("Speaker", "Hello World");
            this.source.CanContinue.Returns(true);
            this.source.GetNextLine().Returns(line);

            using DialogService service = new DialogService(
                this.view,
                this.inputProvider,
                this.delayStrategy,
                new DialogSettings(secondsPerCharacter: 0.05f, allowTypewriterSkip: true));

            service.StartDialog(this.source);

            // Trigger advance while stuck on the first character delay
            this.inputProvider.AdvanceRequested += Raise.Event<Action>();

            this.view.Received().RevealCharacters(line.Text.Length);
            this.source.Received(1).GetNextLine();
        }

        /// <summary>
        /// Verifies that selecting a choice routes the index to the dialog source and advances to the target node.
        /// </summary>
        [Test]
        public void ChoiceSelected_WhenChoicesAreRendered_RoutesChoiceAndAdvancesNode()
        {
            this.delayStrategy
                .DelayAsync(Arg.Any<float>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            DialogLine choiceLine = new DialogLine("Speaker", "Pick one", new[] { "Option A", "Option B" });
            DialogLine nextLine = new DialogLine("Speaker", "You picked Option B");

            this.source.CanContinue.Returns(true);
            this.source.GetNextLine().Returns(choiceLine, nextLine);

            using DialogService service = new DialogService(
                this.view,
                this.inputProvider,
                this.delayStrategy,
                new DialogSettings(secondsPerCharacter: 0f, allowTypewriterSkip: true));

            service.StartDialog(this.source);

            this.view.Received(1).RenderChoices();

            this.view.ChoiceSelected += Raise.Event<Action<int>>(1);

            this.source.Received(1).SelectChoice(1);
            this.view.Received(1).BindLine(nextLine);
        }
    }
}