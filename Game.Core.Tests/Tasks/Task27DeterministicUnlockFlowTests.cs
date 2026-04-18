using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task27DeterministicUnlockFlowTests
{
    // ACC:T27.1
    [Fact]
    public void ShouldKeepAchievementLocked_WhenDeclaredConditionIsNotMet()
    {
        var configuredAchievements = new[]
        {
            new AchievementConditionDefinition("first_blood", RequiredEnemyDefeats: 3, RequiredGold: 0, IsHidden: false)
        };

        var orderedEvents = new List<AchievementSignalEvent>
        {
            new("enemy_defeated", 1),
            new("enemy_defeated", 1)
        };

        var unlockFlow = new DeterministicAchievementUnlockFlow();

        var statesById = unlockFlow.Evaluate(configuredAchievements, orderedEvents);

        statesById["first_blood"].IsUnlocked.Should().BeFalse(
            "configured achievements must remain locked until their declared deterministic conditions are fully satisfied");
    }

    [Fact]
    public void ShouldUnlockAchievement_WhenDeclaredConditionIsMet()
    {
        var configuredAchievements = new[]
        {
            new AchievementConditionDefinition("first_blood", RequiredEnemyDefeats: 3, RequiredGold: 0, IsHidden: false)
        };

        var orderedEvents = new List<AchievementSignalEvent>
        {
            new("enemy_defeated", 1),
            new("enemy_defeated", 1),
            new("enemy_defeated", 1)
        };

        var unlockFlow = new DeterministicAchievementUnlockFlow();

        var statesById = unlockFlow.Evaluate(configuredAchievements, orderedEvents);

        statesById["first_blood"].IsUnlocked.Should().BeTrue(
            "configured achievements should unlock when their declared deterministic conditions are met");
    }

    // ACC:T27.1
    [Fact]
    public void ShouldRejectHiddenOrRandomUnlockBehavior_WhenReplayingSameEvents()
    {
        var configuredAchievements = new[]
        {
            new AchievementConditionDefinition("first_blood", RequiredEnemyDefeats: 3, RequiredGold: 0, IsHidden: false),
            new AchievementConditionDefinition("hidden_lucky_drop", RequiredEnemyDefeats: 999, RequiredGold: 9999, IsHidden: true)
        };

        var orderedEvents = new List<AchievementSignalEvent>
        {
            new("enemy_defeated", 1),
            new("enemy_defeated", 1),
            new("enemy_defeated", 1)
        };

        var unlockFlow = new DeterministicAchievementUnlockFlow();

        var firstRun = unlockFlow.Evaluate(configuredAchievements, orderedEvents);
        var secondRun = unlockFlow.Evaluate(configuredAchievements, orderedEvents);

        var firstRunUnlockedIds = firstRun.Values
            .Where(state => state.IsUnlocked)
            .Select(state => state.Id)
            .OrderBy(id => id)
            .ToArray();

        var secondRunUnlockedIds = secondRun.Values
            .Where(state => state.IsUnlocked)
            .Select(state => state.Id)
            .OrderBy(id => id)
            .ToArray();

        secondRunUnlockedIds.Should().Equal(
            firstRunUnlockedIds,
            "deterministic achievement evaluation must produce identical unlock outcomes for identical ordered inputs");

        secondRunUnlockedIds.Should().NotContain(
            "hidden_lucky_drop",
            "hidden or random unlock behavior must be rejected unless declared deterministic conditions are satisfied");
    }

}
