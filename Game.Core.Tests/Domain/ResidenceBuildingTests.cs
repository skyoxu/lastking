using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Domain.Building;
using Game.Core.Services;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Domain;

public sealed class ResidenceBuildingTests
{
    private const int ConfiguredPopulationCapDelta = 6;

    private sealed class CapturingEventBus : IEventBus
    {
        public List<DomainEvent> Events { get; } = [];

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

    // ACC:T14.4
    [Fact]
    [Trait("acceptance", "ACC:T14.4")]
    public void ShouldIncreasePopulationCapExactlyOnce_WhenResidencePlacementSucceeds()
    {
        var resourceManager = new ResourceManager();
        var baselinePopulationCap = resourceManager.PopulationCap;
        var placementService = new BuildingSubtypePlacementService();
        var state = new BuildingPlacementState(width: 8, height: 8, resources: 300);

        var outcome = placementService.TryPlace(BuildingTypeIds.Residence, new GridPoint(2, 2), state);

        outcome.IsAccepted.Should().BeTrue();
        state.Placements.Should().ContainSingle(record => record.BuildingType == BuildingTypeIds.Residence);
        resourceManager.PopulationCap.Should().Be(baselinePopulationCap + ConfiguredPopulationCapDelta);
    }

    // ACC:T14.10
    [Fact]
    [Trait("acceptance", "ACC:T14.10")]
    public void ShouldKeepGoldPopulationCapAndTaxCadenceUnchanged_WhenResidencePlacementIsRejected()
    {
        var bus = new CapturingEventBus();
        var resourceManager = new ResourceManager(bus, runId: "run-14-reject", dayNumber: 1);
        var baselineSnapshot = resourceManager.GetSnapshot();
        var residenceCost = BuildingCatalog.GetAll()[BuildingTypeIds.Residence].Cost;
        var placementService = new BuildingSubtypePlacementService();
        var state = new BuildingPlacementState(width: 8, height: 8, resources: residenceCost - 1);

        var outcome = placementService.TryPlace(BuildingTypeIds.Residence, new GridPoint(2, 2), state);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
        state.Resources.Should().Be(residenceCost - 1);
        state.Placements.Should().BeEmpty();
        resourceManager.GetSnapshot().Should().Be(baselineSnapshot);
        bus.Events.Should().NotContain(evt => evt.Type == EventTypes.LastkingTaxCollected);
    }

    // ACC:T14.13
    [Fact]
    [Trait("acceptance", "ACC:T14.13")]
    public void ShouldKeepPopulationCapIdempotent_WhenDuplicateResidencePlacementIsProcessed()
    {
        var resourceManager = new ResourceManager();
        var baselinePopulationCap = resourceManager.PopulationCap;
        var placementService = new BuildingSubtypePlacementService();
        var state = new BuildingPlacementState(width: 8, height: 8, resources: 300);

        var firstOutcome = placementService.TryPlace(BuildingTypeIds.Residence, new GridPoint(1, 1), state);
        var duplicateOutcome = placementService.TryPlace(BuildingTypeIds.Residence, new GridPoint(1, 1), state);

        firstOutcome.IsAccepted.Should().BeTrue();
        duplicateOutcome.IsAccepted.Should().BeFalse();
        duplicateOutcome.Reason.Should().Be(BuildingPlacementReasonCodes.Occupied);
        resourceManager.PopulationCap.Should().Be(baselinePopulationCap + ConfiguredPopulationCapDelta);
        resourceManager.PopulationCap.Should().NotBe(baselinePopulationCap + (2 * ConfiguredPopulationCapDelta));
    }

    // ACC:T14.14
    [Fact]
    [Trait("acceptance", "ACC:T14.14")]
    public void ShouldApplyResidenceEconomyOnlyViaSubtypePath_WhenPlacementBypassesResidenceExtension()
    {
        var subtypeResourceManager = new ResourceManager();
        var subtypeBaselinePopulationCap = subtypeResourceManager.PopulationCap;
        var subtypePlacementService = new BuildingSubtypePlacementService();
        var subtypeState = new BuildingPlacementState(width: 8, height: 8, resources: 300);

        var bypassBus = new CapturingEventBus();
        var bypassResourceManager = new ResourceManager(bypassBus, runId: "run-14-bypass", dayNumber: 1);
        var bypassBaselinePopulationCap = bypassResourceManager.PopulationCap;
        var directPlacementService = new BuildingPlacementService();
        var bypassState = new BuildingPlacementState(width: 8, height: 8, resources: 300);

        var subtypeOutcome = subtypePlacementService.TryPlace(BuildingTypeIds.Residence, new GridPoint(3, 3), subtypeState);
        var bypassOutcome = directPlacementService.TryPlace(BuildingTypeIds.Residence, new GridPoint(4, 4), bypassState);

        subtypeOutcome.IsAccepted.Should().BeTrue();
        bypassOutcome.IsAccepted.Should().BeTrue();
        subtypeResourceManager.PopulationCap.Should().Be(subtypeBaselinePopulationCap + ConfiguredPopulationCapDelta);
        bypassResourceManager.PopulationCap.Should().Be(bypassBaselinePopulationCap);
        bypassBus.Events.Should().NotContain(evt => evt.Type == EventTypes.LastkingTaxCollected);
    }
}
