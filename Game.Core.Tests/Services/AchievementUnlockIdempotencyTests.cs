using System;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class AchievementUnlockIdempotencyTests
{
    // ACC:T27.7
    [Fact]
    public void ShouldAvoidDuplicateUnlockSideEffects_WhenReprocessingAlreadyUnlockedAchievementId()
    {
        var processor = new AchievementUnlockProcessor();

        var firstResult = processor.ProcessUnlock("first_blood");
        var secondResult = processor.ProcessUnlock("first_blood");

        firstResult.WasUnlocked.Should().BeTrue();
        secondResult.WasUnlocked.Should().BeFalse();
        secondResult.WasAlreadyUnlocked.Should().BeTrue();

        processor.UnlockTransitions.Should().HaveCount(
            1,
            "reprocessing an already unlocked achievement must not create another unlock transition");

        processor.Notifications.Should().HaveCount(
            1,
            "reprocessing an already unlocked achievement must not emit duplicate notifications");

        processor.PersistenceWrites.Should().HaveCount(
            1,
            "reprocessing an already unlocked achievement must not write duplicate persistence records");

        processor.SyncWrites.Should().HaveCount(
            1,
            "reprocessing an already unlocked achievement must not write duplicate sync records");
    }

    // ACC:T27.7
    [Fact]
    public void ShouldKeepUnlockedIdsUnchanged_WhenReprocessingAlreadyUnlockedAchievementId()
    {
        var processor = new AchievementUnlockProcessor();

        processor.ProcessUnlock("first_blood");
        var unlockedIdsBefore = processor.GetUnlockedIdsSnapshot().OrderBy(id => id).ToArray();

        processor.ProcessUnlock("first_blood");
        var unlockedIdsAfter = processor.GetUnlockedIdsSnapshot().OrderBy(id => id).ToArray();

        unlockedIdsAfter.Should().Equal(
            unlockedIdsBefore,
            "reprocessing an already unlocked achievement must keep unlock state unchanged");
    }

}
