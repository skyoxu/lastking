using System;
using Game.Core.Contracts;
using Game.Core.Services;
using Xunit;
using System.Threading.Tasks;

namespace Game.Core.Tests.Services;

public class EventBusTests
{
    [Fact]
    public async Task ShouldInvokeSubscribersAndRespectUnsubscribe_WhenPublishing()
    {
        var bus = new InMemoryEventBus();
        int called = 0;
        var sub = bus.Subscribe(async e => { called++; await Task.CompletedTask; });

        await bus.PublishAsync(DomainEvent.Create(
            type: "test.evt",
            source: nameof(EventBusTests),
            payload: new EventPayload(true),
            timestamp: DateTime.UtcNow,
            id: Guid.NewGuid().ToString("N")
        ));

        Assert.Equal(1, called);
        sub.Dispose();

        await bus.PublishAsync(DomainEvent.Create(
            type: "test.evt2",
            source: nameof(EventBusTests),
            payload: EmptyPayload.Instance,
            timestamp: DateTime.UtcNow,
            id: Guid.NewGuid().ToString("N")
        ));
        Assert.Equal(1, called);
    }

    [Fact]
    public async Task ShouldContinueCallingOtherSubscribers_WhenOneSubscriberThrows()
    {
        var bus = new InMemoryEventBus();
        int ok = 0;
        bus.Subscribe(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe(_ => { ok++; return Task.CompletedTask; });

        await bus.PublishAsync(DomainEvent.Create(
            type: "evt",
            source: nameof(EventBusTests),
            payload: EmptyPayload.Instance,
            timestamp: DateTime.UtcNow,
            id: Guid.NewGuid().ToString("N")
        ));
        Assert.Equal(1, ok);
    }

    private sealed record EventPayload(bool Ok);

    private sealed class EmptyPayload
    {
        private EmptyPayload()
        {
        }

        public static EmptyPayload Instance { get; } = new();
    }
}
