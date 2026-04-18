using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class AchievementDeterministicUnlockingTests
{
    // ACC:T27.6
    [Fact]
    public void ShouldUnlockSameAchievementIdsAtSameTriggerPoints_WhenReplayingSameOrderedEvents()
    {
        var orderedEvents = new List<AchievementReplayEvent>
        {
            new("battle_won", 1),
            new("battle_won", 3),
            new("gold_earned", 100),
            new("battle_won", 4)
        };

        var achievementService = new DeterministicAchievementUnlocker();

        var firstRunUnlocks = achievementService.Replay(orderedEvents);
        var secondRunUnlocks = achievementService.Replay(orderedEvents);

        secondRunUnlocks.Should().Equal(
            firstRunUnlocks,
            "deterministic replay must unlock the same achievement IDs at the same trigger points");
    }

    // ACC:T27.6
    [Fact]
    public void ShouldNotUnlockAdditionalAchievements_WhenReplayingSameOrderedEvents()
    {
        var orderedEvents = new List<AchievementReplayEvent>
        {
            new("battle_won", 1),
            new("battle_won", 3),
            new("gold_earned", 100),
            new("battle_won", 4)
        };

        var achievementService = new DeterministicAchievementUnlocker();

        var firstRunUnlockIds = achievementService
            .Replay(orderedEvents)
            .Select(unlock => unlock.AchievementId)
            .ToArray();

        var secondRunUnlockIds = achievementService
            .Replay(orderedEvents)
            .Select(unlock => unlock.AchievementId)
            .ToArray();

        secondRunUnlockIds.Should().Equal(
            firstRunUnlockIds,
            "replaying identical ordered events must not unlock additional achievements");
    }

    // ACC:T27.6
    [Fact]
    public void ShouldNotEmitDuplicateAchievementIdsWithinSingleReplay()
    {
        var orderedEvents = new List<AchievementReplayEvent>
        {
            new("battle_won", 3),
            new("battle_won", 4),
            new("gold_earned", 120),
            new("gold_earned", 200)
        };

        var achievementService = new DeterministicAchievementUnlocker();

        var unlockIds = achievementService
            .Replay(orderedEvents)
            .Select(unlock => unlock.AchievementId)
            .ToArray();

        unlockIds.Should().OnlyHaveUniqueItems(
            "a single deterministic replay should emit each achievement unlock at most once");
    }

}
