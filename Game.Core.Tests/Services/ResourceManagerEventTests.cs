using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResourceManagerEventTests
{
    private sealed class CapturingEventBus : IEventBus
    {
        public List<DomainEvent> Events { get; } = new();

        public Task PublishAsync(DomainEvent evt)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }

        public IDisposable Subscribe(Func<DomainEvent, Task> handler) => new NoopSubscription();

        private sealed class NoopSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    // ACC:T12.12
    // ACC:T12.10
    // ACC:T12.16
    [Fact]
    [Trait("acceptance", "ACC:T12.12")]
    [Trait("acceptance", "ACC:T12.10")]
    [Trait("acceptance", "ACC:T12.16")]
    public void ShouldEmitStructuredResourceChangedEvent_WhenMutationSucceeds()
    {
        var bus = new CapturingEventBus();
        var sut = new ResourceManager(bus, runId: "run-12", dayNumber: 4);

        var result = sut.TryAdd(30, 2, 1, "battle-reward");

        result.Succeeded.Should().BeTrue();
        var matchingEvents = bus.Events
            .Where(candidate => candidate.Type == EventTypes.LastkingResourcesChanged)
            .Where(candidate => candidate.DataElement.HasValue)
            .Where(candidate =>
            {
                var payload = candidate.DataElement!.Value;
                return payload.TryGetProperty("reason", out var reasonElement) &&
                       reasonElement.GetString() == "battle-reward";
            })
            .ToList();

        matchingEvents.Should().ContainSingle();
        var evt = matchingEvents[0];
        evt.Type.Should().Be(EventTypes.LastkingResourcesChanged);

        evt.DataElement.HasValue.Should().BeTrue();
        var payload = evt.DataElement!.Value;
        payload.GetProperty("gold").GetInt32().Should().Be(830);
        payload.GetProperty("iron").GetInt32().Should().Be(152);
        payload.GetProperty("populationCap").GetInt32().Should().Be(51);
        payload.GetProperty("runId").GetString().Should().Be("run-12");
        payload.GetProperty("dayNumber").GetInt32().Should().Be(4);
        payload.GetProperty("changedAt").ValueKind.Should().Be(JsonValueKind.String);
        payload.GetProperty("reason").GetString().Should().Be("battle-reward");

        var delta = payload.GetProperty("delta");
        delta.GetProperty("gold").GetInt32().Should().Be(30);
        delta.GetProperty("iron").GetInt32().Should().Be(2);
        delta.GetProperty("populationCap").GetInt32().Should().Be(1);
    }

    // ACC:T12.14
    // ACC:T12.15
    [Fact]
    [Trait("acceptance", "ACC:T12.14")]
    [Trait("acceptance", "ACC:T12.15")]
    public void ShouldNotEmitEvent_WhenMutationIsRejected()
    {
        var bus = new CapturingEventBus();
        var sut = new ResourceManager(bus);
        var baseline = sut.GetSnapshot();

        var rejected = sut.TrySubtract(1000, 0, 0, "invalid");

        rejected.Succeeded.Should().BeFalse();
        sut.GetSnapshot().Should().Be(baseline);
        bus.Events.Should().BeEmpty();
    }

    // ACC:T12.17
    [Fact]
    [Trait("acceptance", "ACC:T12.17")]
    public void ShouldKeepEventOrderAndPayloadIntegrity_WhenUpdatesAreRapid()
    {
        var bus = new CapturingEventBus();
        var sut = new ResourceManager(bus, runId: "run-rapid", dayNumber: 9);

        sut.TryAdd(10, 0, 0, "gain-1").Succeeded.Should().BeTrue();
        sut.TrySpend(5, 1, "spend-1").Succeeded.Should().BeTrue();
        sut.TryAdd(0, 3, 0, "gain-2").Succeeded.Should().BeTrue();

        bus.Events.Select(evt => evt.Type).Should().ContainInOrder(
            EventTypes.LastkingResourcesChanged,
            EventTypes.LastkingResourcesChanged,
            EventTypes.LastkingResourcesChanged);

        var payloads = bus.Events
            .Select(evt => evt.DataElement!.Value)
            .ToArray();

        payloads[0].GetProperty("reason").GetString().Should().Be("gain-1");
        payloads[1].GetProperty("reason").GetString().Should().Be("spend-1");
        payloads[2].GetProperty("reason").GetString().Should().Be("gain-2");

        payloads[2].GetProperty("gold").GetInt32().Should().Be(805);
        payloads[2].GetProperty("iron").GetInt32().Should().Be(152);
        payloads[2].GetProperty("populationCap").GetInt32().Should().Be(50);
    }
}
