using System;
using FluentAssertions;
using Game.Core.Contracts.Guild;
using Game.Core.Contracts.Lastking;
using Xunit;

namespace Game.Core.Tests.Contracts.Lastking;

public class LastkingContractsCoverageTests
{
    [Fact]
    public void ShouldPreserveConstructorValues_WhenCreatingAdditionalEventContracts()
    {
        var now = DateTimeOffset.UtcNow;
        var cloudSave = new CloudSaveSyncCompleted("run-1", "slot-1", "upload", "steam-1", true, string.Empty, "rev-9", now);
        var feedback = new UiFeedbackRaised("run-1", "BUILD_FAIL", "ui.feedback.build.fail", "warning", "blocked", now);
        var autosaved = new SaveAutosaved("run-1", 1, "autosave", "cfg-hash", now);
        var castleHp = new CastleHpChanged("run-1", 2, 1000, 850, now);
        var nightStarted = new NightStarted("run-1", 2, 2, now);
        var guildJoined = new GuildMemberJoined("u-1", "g-1", now, "member");
        var syncResult = new CloudSaveSyncResultDto("slot-1", "download", false, "NETWORK", "rev-10", now);

        cloudSave.Success.Should().BeTrue();
        cloudSave.RemoteRevision.Should().Be("rev-9");
        feedback.Severity.Should().Be("warning");
        feedback.Details.Should().Be("blocked");
        autosaved.SlotId.Should().Be("autosave");
        castleHp.PreviousHp.Should().Be(1000);
        castleHp.CurrentHp.Should().Be(850);
        nightStarted.NightNumber.Should().Be(2);
        guildJoined.Role.Should().Be("member");
        syncResult.ErrorCode.Should().Be("NETWORK");
    }
}
