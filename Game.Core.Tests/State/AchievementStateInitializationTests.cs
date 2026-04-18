using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.State;

public sealed class AchievementStateInitializationTests
{
    // ACC:T27.5
    [Fact]
    public void ShouldShowEveryConfiguredAchievementAsVisible_WhenNoUnlockConditionIsSatisfied()
    {
        var configuredAchievements = new[]
        {
            new AchievementVisibilityDefinition("first_blood", IsHiddenByDefault: false),
            new AchievementVisibilityDefinition("secret_pathfinder", IsHiddenByDefault: true),
            new AchievementVisibilityDefinition("collector", IsHiddenByDefault: false)
        };

        var initializer = new AchievementStateInitializer();

        var stateById = initializer.Initialize(configuredAchievements);

        stateById.Should().HaveCount(
            configuredAchievements.Length,
            "every configured achievement should appear in the visible achievements list before unlock conditions are met");

        stateById.Values.Should().OnlyContain(
            state => state.IsHidden == false,
            "configured achievements must be visible (non-hidden) before any unlock condition is satisfied");
    }

    // ACC:T27.9
    [Fact]
    public void ShouldInitializeEveryConfiguredAchievementAsLocked_WhenConfigurationIsLoaded()
    {
        var configuredAchievements = new[]
        {
            new AchievementVisibilityDefinition("first_blood", IsHiddenByDefault: false),
            new AchievementVisibilityDefinition("treasure_hunter", IsHiddenByDefault: false),
            new AchievementVisibilityDefinition("long_run", IsHiddenByDefault: false)
        };

        var initializer = new AchievementStateInitializer();

        var stateById = initializer.Initialize(configuredAchievements);

        stateById.Keys.Should().BeEquivalentTo(
            configuredAchievements.Select(definition => definition.Id),
            "tracked state must be created for every configured achievement id after configuration load");

        stateById.Values.Should().OnlyContain(
            state => state.IsLocked,
            "all achievement states must start as locked until unlock conditions are satisfied");
    }

    [Fact]
    public void ShouldKeepAllAchievementsLocked_WhenNoUnlockConditionIsSatisfied()
    {
        var configuredAchievements = new[]
        {
            new AchievementVisibilityDefinition("first_blood", IsHiddenByDefault: false),
            new AchievementVisibilityDefinition("collector", IsHiddenByDefault: false)
        };

        var initializer = new AchievementStateInitializer();

        var stateById = initializer.Initialize(configuredAchievements);

        stateById.Values.Should().NotContain(
            state => state.IsLocked == false,
            "no achievement should be unlocked before unlock conditions are met");
    }

}
