using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class AchievementInvalidTriggerTests
{
    // ACC:T27.10
    [Fact]
    public void ShouldKeepUnlockStateUnchangedAndEmitNoSideEffects_WhenUnlockRequestDoesNotMatchAnyUnlockableAchievement()
    {
        var initialStates = new Dictionary<string, bool>
        {
            ["survive_day_15"] = false,
            ["first_blood"] = true
        };

        var coordinator = new AchievementTriggerCoordinator(initialStates);

        var stateBefore = coordinator.GetSnapshot();
        var result = coordinator.TryUnlockById("missing_achievement");

        result.WasUnlocked.Should().BeFalse();
        coordinator.GetSnapshot().Should().Equal(stateBefore, "invalid unlock requests must not change unlock state");
        coordinator.Notifications.Should().BeEmpty("invalid unlock requests must not emit notifications");
        coordinator.ExternalSyncCalls.Should().BeEmpty("invalid unlock requests must not trigger external sync");
    }

    // ACC:T27.10
    [Fact]
    public void ShouldKeepUnlockStateUnchangedAndEmitNoSideEffects_WhenEventDoesNotMapToAnyUnlockableAchievement()
    {
        var initialStates = new Dictionary<string, bool>
        {
            ["survive_day_15"] = false,
            ["first_blood"] = true
        };

        var coordinator = new AchievementTriggerCoordinator(initialStates);

        var stateBefore = coordinator.GetSnapshot();
        var result = coordinator.HandleEvent("event_without_mapping");

        result.WasUnlocked.Should().BeFalse();
        coordinator.GetSnapshot().Should().Equal(stateBefore, "events without valid mapping must not change unlock state");
        coordinator.Notifications.Should().BeEmpty("events without valid mapping must not emit notifications");
        coordinator.ExternalSyncCalls.Should().BeEmpty("events without valid mapping must not trigger external sync");
    }

}
