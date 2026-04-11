using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.State;

public class GameStateMachineTests
{
    // ACC:T10.3
    // ACC:T10.14
    // ACC:T16.3
    [Fact]
    public void ShouldTransitionThroughCoreFlow_WhenInvokingStartPauseResumeEnd()
    {
        var fsm = new GameStateMachine();
        var calls = 0;
        fsm.OnTransition += (_, _) => calls += 1;

        fsm.Start().Should().BeTrue();
        fsm.Pause().Should().BeTrue();
        fsm.Resume().Should().BeTrue();
        fsm.End().Should().BeTrue();

        fsm.State.Should().Be(GameFlowState.GameOver);
        calls.Should().BeGreaterThanOrEqualTo(3);
    }

    // ACC:T8.7
    // ACC:T8.15
    [Fact]
    public void ShouldRejectInvalidTransitions_WhenCurrentStateDoesNotAllowAction()
    {
        var fsm = new GameStateMachine();

        fsm.Resume().Should().BeFalse();
        fsm.End().Should().BeTrue();
        fsm.End().Should().BeFalse();
        fsm.Start().Should().BeFalse();
        fsm.Pause().Should().BeFalse();
    }

    // ACC:T3.6
    [Fact]
    public void ShouldRejectIllegalTransitions_WhenStateMachineIsInDayOrNightOnly()
    {
        var runtime = new DayNightRuntimeStateMachine(seed: 42);

        runtime.CurrentPhase.Should().Be(DayNightPhase.Day);
        runtime.CurrentDay.Should().Be(1);
        runtime.RequestTransition(DayNightPhase.Day).Should().BeFalse();
        runtime.RequestTransition(DayNightPhase.Terminal).Should().BeFalse();

        runtime.Update(239.9);
        runtime.CurrentPhase.Should().Be(DayNightPhase.Day);

        runtime.Update(0.1);
        runtime.CurrentPhase.Should().Be(DayNightPhase.Night);
        runtime.CurrentDay.Should().Be(1);

        var phaseBeforeIllegalRequest = runtime.CurrentPhase;
        var dayBeforeIllegalRequest = runtime.CurrentDay;
        runtime.RequestTransition(DayNightPhase.Night).Should().BeFalse();
        runtime.RequestTransition(DayNightPhase.Terminal).Should().BeFalse();
        runtime.CurrentPhase.Should().Be(phaseBeforeIllegalRequest);
        runtime.CurrentDay.Should().Be(dayBeforeIllegalRequest);
    }

    // ACC:T9.3
    // ACC:T3.1
    // ACC:T3.2
    [Fact]
    public void ShouldEmitSingleTerminalAndStopProgression_WhenForcedTerminalPathReplayed()
    {
        const int seed = 7;
        var first = new DayNightRuntimeStateMachine(seed);
        var second = new DayNightRuntimeStateMachine(seed);

        var firstCheckpoints = new List<DayNightCheckpoint>();
        var secondCheckpoints = new List<DayNightCheckpoint>();
        var firstBoundaryTransitions = 0;
        var secondBoundaryTransitions = 0;
        var firstPhase = first.CurrentPhase;
        var secondPhase = second.CurrentPhase;
        var firstTerminalEvents = 0;
        var secondTerminalEvents = 0;

        first.OnCheckpoint += firstCheckpoints.Add;
        second.OnCheckpoint += secondCheckpoints.Add;
        first.OnTerminal += _ => firstTerminalEvents += 1;
        second.OnTerminal += _ => secondTerminalEvents += 1;

        first.Update(240);
        second.Update(240);
        if (first.CurrentPhase != firstPhase)
        {
            firstBoundaryTransitions += 1;
            firstPhase = first.CurrentPhase;
        }

        if (second.CurrentPhase != secondPhase)
        {
            secondBoundaryTransitions += 1;
            secondPhase = second.CurrentPhase;
        }
        first.ForceTerminal().Should().BeTrue();
        second.ForceTerminal().Should().BeTrue();

        first.IsTerminal.Should().BeTrue();
        second.IsTerminal.Should().BeTrue();
        firstTerminalEvents.Should().Be(1);
        secondTerminalEvents.Should().Be(1);
        first.CurrentDay.Should().Be(1);
        second.CurrentDay.Should().Be(1);
        first.CurrentPhase.Should().Be(DayNightPhase.Terminal);
        second.CurrentPhase.Should().Be(DayNightPhase.Terminal);
        first.CheckpointCount.Should().Be(second.CheckpointCount);
        first.CheckpointCount.Should().Be(firstBoundaryTransitions);
        second.CheckpointCount.Should().Be(secondBoundaryTransitions);
        first.CheckpointCount.Should().BeGreaterThan(0);
        firstCheckpoints.Select(x => x.RandomToken).Should().Equal(secondCheckpoints.Select(x => x.RandomToken));

        var beforeTick = first.Tick;
        first.Update(600);
        first.Tick.Should().Be(beforeTick);
        firstTerminalEvents.Should().Be(1);
        first.ForceTerminal().Should().BeFalse();
    }
}
