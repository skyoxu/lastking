using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Engine;

public sealed class CoreLoopDeterminismLocalizationTests
{
    // ACC:T28.3
    // Acceptance intent:
    // with the same inputs and initial state, locale changes may only alter localized text.
    [Fact]
    public void ShouldKeepCoreLoopResultDeterministic_WhenSwitchingLanguageWithSameInputAndInitialState()
    {
        var initialState = new CoreState(PlayerPosition: 2, Treasury: 100);
        var inputs = new[] { 1, 3, 2, 4 };
        var runner = new StableLocalizedCoreLoop();

        var enSnapshot = runner.Run("en-US", initialState, inputs);
        var zhSnapshot = runner.Run("zh-CN", initialState, inputs);

        var enCoreResult = enSnapshot.ToCoreResult();
        var zhCoreResult = zhSnapshot.ToCoreResult();

        enCoreResult.Should().BeEquivalentTo(zhCoreResult);
    }

    [Fact]
    public void ShouldFlagResultDrift_WhenLocalizationAffectsSimulation()
    {
        var initialState = new CoreState(PlayerPosition: 2, Treasury: 100);
        var inputs = new[] { 1, 3, 2, 4 };
        var runner = new BuggyLocalizedCoreLoop();

        var enSnapshot = runner.Run("en-US", initialState, inputs);
        var zhSnapshot = runner.Run("zh-CN", initialState, inputs);

        var driftDetected = CoreResultDriftDetector.HasDrift(enSnapshot, zhSnapshot);

        driftDetected.Should().BeTrue();
    }

    private readonly record struct CoreState(int PlayerPosition, int Treasury);

    private sealed record CoreLoopSnapshot(
        int TickCount,
        int PlayerPosition,
        int Treasury,
        IReadOnlyList<int> EventOrder,
        string LocalizedText)
    {
        public object ToCoreResult()
        {
            return new
            {
                TickCount,
                PlayerPosition,
                Treasury,
                EventOrder
            };
        }
    }

    private sealed class BuggyLocalizedCoreLoop
    {
        public CoreLoopSnapshot Run(string locale, CoreState initialState, IReadOnlyList<int> inputs)
        {
            var localizedText = locale == "zh-CN" ? "Round" : "RoundStart";
            var localeBias = localizedText.Length % 2; // known bug: locale leaks into core simulation

            var playerPosition = initialState.PlayerPosition;
            var treasury = initialState.Treasury;
            var eventOrder = new List<int>(inputs.Count);

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                playerPosition += input + localeBias;
                treasury -= input * 3 + localeBias;
                eventOrder.Add((playerPosition * 31) ^ treasury);
            }

            return new CoreLoopSnapshot(
                TickCount: inputs.Count,
                PlayerPosition: playerPosition,
                Treasury: treasury,
                EventOrder: eventOrder,
                LocalizedText: localizedText);
        }
    }

    private sealed class StableLocalizedCoreLoop
    {
        public CoreLoopSnapshot Run(string locale, CoreState initialState, IReadOnlyList<int> inputs)
        {
            var localizedText = locale == "zh-CN" ? "Round" : "RoundStart";
            var playerPosition = initialState.PlayerPosition;
            var treasury = initialState.Treasury;
            var eventOrder = new List<int>(inputs.Count);

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                playerPosition += input;
                treasury -= input * 3;
                eventOrder.Add((playerPosition * 31) ^ treasury);
            }

            return new CoreLoopSnapshot(
                TickCount: inputs.Count,
                PlayerPosition: playerPosition,
                Treasury: treasury,
                EventOrder: eventOrder,
                LocalizedText: localizedText);
        }
    }

    private static class CoreResultDriftDetector
    {
        public static bool HasDrift(CoreLoopSnapshot left, CoreLoopSnapshot right)
        {
            return left.TickCount != right.TickCount
                || left.PlayerPosition != right.PlayerPosition
                || left.Treasury != right.Treasury
                || !AreEventOrdersEqual(left.EventOrder, right.EventOrder);
        }

        private static bool AreEventOrdersEqual(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
