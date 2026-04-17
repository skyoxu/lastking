using System;
using System.Linq;
using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;
using Game.Core.Services;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class SaveManagerWorkflowTests
{
    // ACC:T26.3
    [Fact]
    public void ShouldPreserveDeterministicSimulationLoopOutputs_WhenCloudSyncIsDisabled()
    {
        var cloudSyncGateway = new SpyCloudSyncGateway();
        var sut = new SaveManagerCloudSyncWorkflow(cloudSyncGateway);
        var enemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var beforeSaveOutputs = sut.RunDeterministicLoop(enemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);

        _ = sut.Save(new SaveWorkflowCommand("slot-local", EnableCloudSync: false, SteamAccountId: string.Empty, Payload: "state-a"));

        var afterSaveOutputs = sut.RunDeterministicLoop(enemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);

        afterSaveOutputs.Should().Equal(beforeSaveOutputs);
    }

    // ACC:T26.3
    [Fact]
    public void ShouldNotTriggerCloudSync_WhenSavingWithoutCloudBinding()
    {
        var cloudSyncGateway = new SpyCloudSyncGateway();
        var sut = new SaveManagerCloudSyncWorkflow(cloudSyncGateway);

        _ = sut.Save(new SaveWorkflowCommand("slot-local", EnableCloudSync: false, SteamAccountId: string.Empty, Payload: "state-a"));

        cloudSyncGateway.CallCount.Should().Be(0);
    }

    // ACC:T26.4
    [Fact]
    public void ShouldTriggerUploadSyncWithExpectedArguments_WhenCloudBindingIsEnabled()
    {
        var cloudSyncGateway = new SpyCloudSyncGateway();
        var sut = new SaveManagerCloudSyncWorkflow(cloudSyncGateway);

        var result = sut.Save(new SaveWorkflowCommand(
            SlotId: "slot-cloud",
            EnableCloudSync: true,
            SteamAccountId: "steam-acc-26",
            Payload: "{\"health\":50,\"score\":9,\"level\":2}"));

        result.Should().NotBeNull();
        cloudSyncGateway.CallCount.Should().Be(1);
        cloudSyncGateway.LastSlotId.Should().Be("slot-cloud");
        cloudSyncGateway.LastDirection.Should().Be("upload");
        cloudSyncGateway.LastSteamAccountId.Should().Be("steam-acc-26");
        cloudSyncGateway.LastPayload.Should().Be("{\"health\":50,\"score\":9,\"level\":2}");
    }

    // ACC:T26.4
    [Theory]
    [InlineData("", "steam-acc", "payload")]
    [InlineData("slot", "", "payload")]
    [InlineData("slot", "steam-acc", "")]
    public void ShouldRejectCloudSync_WhenRequiredFieldsAreMissing(string slotId, string steamAccountId, string payload)
    {
        var cloudSyncGateway = new SpyCloudSyncGateway();
        var sut = new SaveManagerCloudSyncWorkflow(cloudSyncGateway);

        var result = sut.Save(new SaveWorkflowCommand(
            SlotId: slotId,
            EnableCloudSync: true,
            SteamAccountId: steamAccountId,
            Payload: payload));

        result.Should().BeNull();
        cloudSyncGateway.CallCount.Should().Be(0);
    }

    private sealed class SpyCloudSyncGateway : ICloudSaveSyncService
    {
        public int CallCount { get; private set; }
        public string LastSlotId { get; private set; } = string.Empty;
        public string LastDirection { get; private set; } = string.Empty;
        public string LastSteamAccountId { get; private set; } = string.Empty;
        public string LastPayload { get; private set; } = string.Empty;

        public CloudSaveSyncResultDto Sync(string runId, string slotId, string direction, string steamAccountId, string payload)
        {
            CallCount++;
            LastSlotId = slotId;
            LastDirection = direction;
            LastSteamAccountId = steamAccountId;
            LastPayload = payload;
            return new CloudSaveSyncResultDto(
                SlotId: slotId,
                Direction: direction,
                Success: true,
                ErrorCode: string.Empty,
                RemoteRevision: "rev-ok",
                SyncedAt: DateTimeOffset.UtcNow);
        }
    }
}
