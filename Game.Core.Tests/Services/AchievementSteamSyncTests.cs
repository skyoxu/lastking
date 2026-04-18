using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Game.Core.Services;

namespace Game.Core.Tests.Services;

public sealed class AchievementSteamSyncTests
{
    // ACC:T27.8
    [Fact]
    public void ShouldSyncEachFirstTimeUnlock_WhenSteamIntegrationIsActiveInSameSession()
    {
        var coordinator = new AchievementSteamSyncCoordinator(isSteamIntegrationActive: true);
        var sessionId = "session-001";

        coordinator.OnAchievementUnlocked(sessionId, "first_blood");
        coordinator.OnAchievementUnlocked(sessionId, "tower_defender");

        coordinator.SteamSyncCalls.Should().HaveCount(
            2,
            "every first-time unlock must be synchronized to Steam in the same gameplay session when integration is active");

        coordinator.SteamSyncCalls
            .Select(call => call.AchievementId)
            .Should()
            .Equal("first_blood", "tower_defender");

        coordinator.SteamSyncCalls
            .Should()
            .OnlyContain(call => call.SessionId == sessionId, "sync calls must happen in the same session context");
    }

    [Fact]
    public void ShouldNotSyncDuplicateUnlock_WhenAchievementAlreadyUnlockedInSameSession()
    {
        var coordinator = new AchievementSteamSyncCoordinator(isSteamIntegrationActive: true);
        var sessionId = "session-002";

        coordinator.OnAchievementUnlocked(sessionId, "first_blood");
        coordinator.OnAchievementUnlocked(sessionId, "first_blood");

        coordinator.SteamSyncCalls
            .Where(call => call.AchievementId == "first_blood")
            .Should()
            .HaveCount(1, "duplicate unlock attempts in the same session must not trigger additional Steam sync");
    }

    // ACC:T27.8
    [Fact]
    public void ShouldSkipSteamSyncAndKeepLocalUnlock_WhenIntegrationIsInactive()
    {
        var coordinator = new AchievementSteamSyncCoordinator(isSteamIntegrationActive: false);
        var sessionId = "session-003";

        var firstUnlock = coordinator.OnAchievementUnlocked(sessionId, "first_blood");
        var duplicateUnlock = coordinator.OnAchievementUnlocked(sessionId, "first_blood");

        firstUnlock.Should().BeTrue("local unlock should still transition on first unlock attempt");
        duplicateUnlock.Should().BeFalse("duplicate unlock should remain idempotent even when Steam sync is disabled");
        coordinator.SteamSyncCalls.Should().BeEmpty("disabled Steam integration must not emit sync calls");
    }
}
