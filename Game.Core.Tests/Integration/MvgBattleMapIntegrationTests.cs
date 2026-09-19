using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Domain.Building;
using Game.Core.Services;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Integration;

public sealed class MvgBattleMapIntegrationTests
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

    [Fact]
    public void PlacementAtExactCost_CommitsOnceAndConsumesTheExactCost()
    {
        var service = new BuildingPlacementService();
        var wall = BuildingCatalog.GetAll()[BuildingTypeIds.Wall];
        var state = new BuildingPlacementState(width: 8, height: 8, resources: wall.Cost);

        var result = service.TryPlace(BuildingTypeIds.Wall, new GridPoint(2, 2), state);

        result.IsAccepted.Should().BeTrue();
        state.Resources.Should().Be(0);
        state.Placements.Should().ContainSingle();
        state.Grid.Occupied.Should().BeEquivalentTo(result.CommittedCells);
    }

    [Fact]
    public void PlacementOneBelowCost_IsRejectedWithoutStateMutation()
    {
        var service = new BuildingPlacementService();
        var wall = BuildingCatalog.GetAll()[BuildingTypeIds.Wall];
        var state = new BuildingPlacementState(width: 8, height: 8, resources: wall.Cost - 1);
        var beforeResources = state.Resources;

        var result = service.TryPlace(BuildingTypeIds.Wall, new GridPoint(2, 2), state);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
        state.Resources.Should().Be(beforeResources);
        state.Placements.Should().BeEmpty();
        state.Grid.Occupied.Should().BeEmpty();
    }

    [Fact]
    public void ResourceMutation_PublishesOneStructuredChangeForTheBattleMapBoundary()
    {
        var bus = new CapturingEventBus();
        var resources = new ResourceManager(bus, runId: "mvg-battlemap", dayNumber: 3);

        var result = resources.TryAdd(25, 2, 0, "mvg-battlemap-pilot");

        result.Succeeded.Should().BeTrue();
        var events = bus.Events
            .Where(candidate => candidate.Type == EventTypes.LastkingResourcesChanged)
            .Where(candidate => candidate.DataElement.HasValue)
            .Where(candidate =>
                candidate.DataElement!.Value.TryGetProperty("reason", out var reason) &&
                reason.GetString() == "mvg-battlemap-pilot")
            .ToArray();
        events.Should().ContainSingle();
        var payload = events[0].DataElement!.Value;
        payload.GetProperty("runId").GetString().Should().Be("mvg-battlemap");
        payload.GetProperty("dayNumber").GetInt32().Should().Be(3);
        payload.GetProperty("delta").GetProperty("gold").GetInt32().Should().Be(25);
        payload.GetProperty("delta").GetProperty("iron").GetInt32().Should().Be(2);
    }
}
