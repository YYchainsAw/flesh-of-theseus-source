using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace CollisionJam.Core.Tests
{
    public sealed class CodePuzzleSessionTests
    {
        private static readonly ViewId Room = new ViewId("test.code-room");
        private static readonly PuzzleId Lock = new PuzzleId("test.code-lock");

        [Test]
        public void CodeSuccessCommitsOnlyProgressAndPublishesOneCompleteWorldNotification()
        {
            var rule = new ProbeRule();
            IGameSession session = Create(rule);
            var notifications = new List<StateChange>();
            session.StateChanged += change =>
            {
                Assert.That(session.State.IsSolved(Lock), Is.True);
                Assert.That(session.State.GetOwnedItemsSnapshot(), Is.Empty);
                Assert.That(session.State.CurrentView, Is.EqualTo(Room));
                notifications.Add(change);
            };
            Assert.That(session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }).Changed, Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.World }));
            Assert.That(rule.Calls, Is.EqualTo(1));
            Assert.That(session.TrySubmitCode(Lock, null).Code, Is.EqualTo(ResultCode.AlreadyCompleted));
            Assert.That(session.TrySolve(Lock).Code, Is.EqualTo(ResultCode.AlreadyCompleted));
            Assert.That(notifications.Count, Is.EqualTo(1));
            Assert.That(rule.Calls, Is.EqualTo(1));
        }

        [Test]
        public void OrdinarySolveCannotBypassCodeRuleAndCodeSubmitCannotUseAnOrdinaryRule()
        {
            var codeRule = new ProbeRule();
            var codeSession = Create(codeRule);
            Assert.That(codeSession.TrySolve(Lock).Code, Is.EqualTo(ResultCode.RuleUnavailable));
            Assert.That(codeSession.State.IsSolved(Lock), Is.False);
            Assert.That(codeRule.Calls, Is.Zero);
            var ordinary = new TestPuzzleRule();
            var ordinarySession = new GameSession(Room, puzzleRules: new[] {
                new KeyValuePair<PuzzleId, IPuzzleRule>(Lock, ordinary) });
            Assert.That(ordinarySession.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }).Code, Is.EqualTo(ResultCode.RuleUnavailable));
            Assert.That(ordinary.CallCount, Is.Zero);
            Assert.That(ordinarySession.State.IsSolved(Lock), Is.False);
        }

        [TestCase(ResultCode.CodeMismatch)]
        [TestCase(ResultCode.InvalidCodeInput)]
        [TestCase(ResultCode.WrongView)]
        [TestCase(ResultCode.Uninitialized)]
        public void RejectedCodeRuleDoesNotWriteOrNotify(ResultCode rejection)
        {
            var rule = new ProbeRule { Callback = (state, input) => RuleResult.Reject(rejection) };
            var session = Create(rule);
            var changes = Observe(session);
            var result = session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 });
            Assert.That(result.Code, Is.EqualTo(rejection));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Changed, Is.False);
            Assert.That(session.State.IsSolved(Lock), Is.False);
            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void InvalidPuzzleMissingRuleAndNullInputRejectBeforeRuleExecution()
        {
            var rule = new ProbeRule();
            var session = Create(rule);
            var changes = Observe(session);
            Assert.That(session.TrySubmitCode(default, new[] { 0, 1, 2, 3 }).Code, Is.EqualTo(ResultCode.InvalidPuzzle));
            Assert.That(session.TrySubmitCode(new PuzzleId("test.unknown-code"), new[] { 0, 1, 2, 3 }).Code,
                Is.EqualTo(ResultCode.RuleUnavailable));
            Assert.That(session.TrySubmitCode(Lock, null).Code, Is.EqualTo(ResultCode.InvalidCodeInput));
            Assert.That(rule.Calls, Is.Zero);
            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void RuleReceivesAnIndependentReadOnlySnapshotRatherThanTheUiSelectionArray()
        {
            var selections = new[] { 0, 1, 2, 3 };
            var rule = new ProbeRule { Callback = (state, input) =>
            {
                Assert.That(input, Is.Not.SameAs(selections));
                selections[0] = 5;
                Assert.That(input, Is.EqualTo(new[] { 0, 1, 2, 3 }));
                Assert.Throws<NotSupportedException>(() => ((IList<int>)input)[0] = 4);
                return RuleResult.Reject(ResultCode.CodeMismatch);
            } };
            var session = Create(rule);
            Assert.That(session.TrySubmitCode(Lock, selections).Code, Is.EqualTo(ResultCode.CodeMismatch));
            selections[1] = 5;
            Assert.That(rule.LastInput, Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(session.State.IsSolved(Lock), Is.False);
        }

        [Test]
        public void CodeRegistryIsCopiedAndEnumeratedOnce()
        {
            var original = new ProbeRule();
            var registry = new List<KeyValuePair<PuzzleId, ICodePuzzleRule>> { Entry(Lock, original) };
            var once = new SingleUseRegistry(registry);
            var session = new GameSession(Room, codePuzzleRules: once);
            registry.Clear();
            registry.Add(Entry(Lock, new ProbeRule { Callback = (state, input) => RuleResult.Reject(ResultCode.CodeMismatch) }));
            Assert.That(session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }).Changed, Is.True);
            Assert.That(original.Calls, Is.EqualTo(1));
            Assert.That(once.Enumerations, Is.EqualTo(1));
        }

        [TestCase("invalid-id")]
        [TestCase("null-rule")]
        [TestCase("duplicate-id")]
        [TestCase("ordinary-and-code")]
        public void InvalidOrAmbiguousCodeRegistrationsFailAtConstruction(string variant)
        {
            var rule = new ProbeRule();
            var entries = new[] { Entry(Lock, rule) };
            KeyValuePair<PuzzleId, IPuzzleRule>[] ordinary = null;
            switch (variant)
            {
                case "invalid-id": entries = new[] { Entry(default, rule) }; break;
                case "null-rule": entries = new[] { Entry(Lock, null) }; break;
                case "duplicate-id": entries = new[] { Entry(Lock, rule), Entry(Lock, rule) }; break;
                case "ordinary-and-code": ordinary = new[] { new KeyValuePair<PuzzleId, IPuzzleRule>(Lock, new TestPuzzleRule()) }; break;
            }
            var error = Assert.Throws<ArgumentException>(() => new GameSession(Room, puzzleRules: ordinary, codePuzzleRules: entries));
            Assert.That(error.ParamName, Is.EqualTo("codePuzzleRules"));
        }

        [Test]
        public void RuleAndNotificationReentryAreRejectedDuringCodeSubmission()
        {
            GameSession session = null;
            var nested = new List<CommandResult>();
            var rule = new ProbeRule { Callback = (state, input) =>
            {
                nested.Add(session.TrySubmitCode(Lock, input));
                nested.Add(session.TrySolve(Lock));
                return RuleResult.Allow();
            } };
            session = Create(rule);
            session.StateChanged += change => nested.Add(session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }));
            Assert.That(session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }).Changed, Is.True);
            Assert.That(nested.Count, Is.EqualTo(3));
            foreach (var result in nested) Assert.That(result.Code, Is.EqualTo(ResultCode.CommandInProgress));
        }

        [Test]
        public void RuleExceptionLeavesNoProgressAndReleasesTheCommandGuard()
        {
            var failure = new InvalidOperationException("test code rule failure");
            var rule = new ProbeRule { Callback = (state, input) => throw failure };
            var session = Create(rule);
            var changes = Observe(session);
            Assert.That(Assert.Throws<InvalidOperationException>(() => session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 })), Is.SameAs(failure));
            Assert.That(session.State.IsSolved(Lock), Is.False);
            Assert.That(changes, Is.Empty);
            rule.Callback = null;
            Assert.That(session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }).Changed, Is.True);
            Assert.That(changes, Is.EqualTo(new[] { StateChange.World }));
        }

        [Test]
        public void SubscriberExceptionOccursAfterCodeCommitAndDoesNotAllowASecondCompletion()
        {
            var session = Create(new ProbeRule());
            Action<StateChange> subscriber = change => throw new InvalidOperationException("test subscriber failure");
            session.StateChanged += subscriber;
            Assert.Throws<InvalidOperationException>(() => session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }));
            Assert.That(session.State.IsSolved(Lock), Is.True);
            session.StateChanged -= subscriber;
            Assert.That(session.TrySubmitCode(Lock, new[] { 0, 1, 2, 3 }).Code, Is.EqualTo(ResultCode.AlreadyCompleted));
        }

        private static GameSession Create(ICodePuzzleRule rule) => new GameSession(Room, codePuzzleRules: new[] { Entry(Lock, rule) });
        private static KeyValuePair<PuzzleId, ICodePuzzleRule> Entry(PuzzleId id, ICodePuzzleRule rule)
            => new KeyValuePair<PuzzleId, ICodePuzzleRule>(id, rule);
        private static List<StateChange> Observe(IGameSession session)
        { var changes = new List<StateChange>(); session.StateChanged += changes.Add; return changes; }

        private sealed class ProbeRule : ICodePuzzleRule
        {
            internal Func<IGameStateView, IReadOnlyList<int>, RuleResult> Callback;
            internal int Calls;
            internal IReadOnlyList<int> LastInput;
            public RuleResult Check(IGameStateView state, IReadOnlyList<int> input)
            { Calls++; LastInput = input; return Callback?.Invoke(state, input) ?? RuleResult.Allow(); }
        }

        private sealed class SingleUseRegistry : IEnumerable<KeyValuePair<PuzzleId, ICodePuzzleRule>>
        {
            private readonly IEnumerable<KeyValuePair<PuzzleId, ICodePuzzleRule>> values;
            internal int Enumerations;
            internal SingleUseRegistry(IEnumerable<KeyValuePair<PuzzleId, ICodePuzzleRule>> values) => this.values = values;
            public IEnumerator<KeyValuePair<PuzzleId, ICodePuzzleRule>> GetEnumerator()
            {
                if (++Enumerations > 1) throw new InvalidOperationException("Registry enumerated twice.");
                return values.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
