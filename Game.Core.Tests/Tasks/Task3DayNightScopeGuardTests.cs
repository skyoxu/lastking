using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Tasks;

public class Task3DayNightScopeGuardTests
{
    [Fact]
    public void ShouldAllowProgressionToDay15_WhenCycleStartsAtDay1()
    {
        var runtime = new DayNightRuntimeStateMachine(seed: 3);

        for (var i = 0; i < 2000; i++)
        {
            runtime.Update(60);
            if (runtime.IsTerminal)
            {
                break;
            }
        }

        runtime.CurrentDay.Should().Be(15);
        runtime.CurrentPhase.Should().Be(DayNightPhase.Terminal);
    }

    // ACC:T3.3
    [Fact]
    public void ShouldKeepDayNightScopeWithoutUnrelatedEventTypes_WhenRuntimeProgresses()
    {
        var manager = new GameStateManager(new NoopStore(), dayNightSeed: 9);
        var emittedEventTypes = new List<string>();
        manager.OnEvent(evt => emittedEventTypes.Add(evt.Type));

        for (var i = 0; i < 2000; i++)
        {
            manager.UpdateDayNightRuntime(60);
            if (manager.CurrentDayNightPhase == DayNightPhase.Terminal)
            {
                break;
            }
        }

        var dayAtTerminal = manager.CurrentDayNightDay;
        manager.UpdateDayNightRuntime(600);

        dayAtTerminal.Should().BeGreaterThanOrEqualTo(1);
        dayAtTerminal.Should().BeLessThanOrEqualTo(15);
        manager.CurrentDayNightDay.Should().Be(dayAtTerminal);
        manager.CurrentDayNightDay.Should().BeLessThanOrEqualTo(15);

        emittedEventTypes.Should().OnlyContain(type =>
            type == EventTypes.LastkingDayStarted ||
            type == EventTypes.LastkingNightStarted ||
            type == "core.lastking.daynight.terminal");
        emittedEventTypes.Should().NotContain(type => type.StartsWith("game.save."));
    }

    private sealed class NoopStore : IDataStore
    {
        public Task SaveAsync(string key, string json) => Task.CompletedTask;
        public Task<string?> LoadAsync(string key) => Task.FromResult<string?>(null);
        public Task DeleteAsync(string key) => Task.CompletedTask;
    }
}
