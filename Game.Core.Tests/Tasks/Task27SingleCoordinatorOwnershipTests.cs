using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task27SingleCoordinatorOwnershipTests
{
    // ACC:T27.11
    [Fact]
    public void ShouldKeepProgressUnchanged_WhenNonOwnerProcessesEvent()
    {
        var achievementDefinition = new AchievementOwnershipDefinition("slayer", RequiredDefeats: 1);
        var sharedState = new SharedAchievementState(new[] { achievementDefinition });
        var ownerCoordinator = new AchievementCoordinator("owner", sharedState);
        var nonOwnerCoordinator = new AchievementCoordinator("non-owner", sharedState);

        ownerCoordinator.ClaimOwnership();

        nonOwnerCoordinator.ProcessEvent(new AchievementOwnershipEvent("enemy_defeated"));

        sharedState.GetProgress("slayer").Should().Be(0,
            "only the owner coordinator should track achievement progress");
    }

    // ACC:T27.11
    [Fact]
    public void ShouldRefuseUnlockOrchestration_WhenNonOwnerReachesThreshold()
    {
        var achievementDefinition = new AchievementOwnershipDefinition("slayer", RequiredDefeats: 2);
        var sharedState = new SharedAchievementState(new[] { achievementDefinition });
        var ownerCoordinator = new AchievementCoordinator("owner", sharedState);
        var nonOwnerCoordinator = new AchievementCoordinator("non-owner", sharedState);

        ownerCoordinator.ClaimOwnership();

        nonOwnerCoordinator.ProcessEvent(new AchievementOwnershipEvent("enemy_defeated"));
        nonOwnerCoordinator.ProcessEvent(new AchievementOwnershipEvent("enemy_defeated"));

        sharedState.IsUnlocked("slayer").Should().BeFalse(
            "unlock orchestration must be rejected when attempted by a non-owner coordinator");
        sharedState.GetUnlockedBy("slayer").Should().BeNull(
            "non-owner attempts must leave unlock ownership unchanged");
    }

}
