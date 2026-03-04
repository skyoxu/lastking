using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Domain.Entities;
using Xunit;

namespace Game.Core.Tests.Domain;

public class DomainEntitiesCoverageTests
{
    [Fact]
    public void ShouldKeepValues_WhenCreatingGameResultAndStatistics()
    {
        var stats = new GameStatistics(3, 4, 5, 6.5, 0.35);
        var result = new GameResult(1000, 9, 120.5, new[] { "ACH_WIN" }, stats);

        result.FinalScore.Should().Be(1000);
        result.LevelReached.Should().Be(9);
        result.PlayTimeSeconds.Should().Be(120.5);
        result.Statistics.EnemiesDefeated.Should().Be(5);
    }

    [Fact]
    public void ShouldAllowPropertyRoundtrip_WhenUsingEntityModels()
    {
        var save = new SaveGame
        {
            Id = "s-1",
            UserId = "u-1",
            SlotNumber = 2,
            Data = "{\"gold\":800}",
            CreatedAt = 1000,
            UpdatedAt = 1100
        };
        var achievement = new Achievement
        {
            Id = "a-1",
            UserId = "u-1",
            AchievementKey = "survive_day_1",
            UnlockedAt = 1200,
            Progress = 1.0
        };
        var user = new User
        {
            Id = "u-1",
            Username = "hero",
            CreatedAt = 900,
            LastLogin = 1300
        };

        save.SlotNumber.Should().Be(2);
        save.Data.Should().Contain("gold");
        achievement.AchievementKey.Should().Be("survive_day_1");
        achievement.Progress.Should().Be(1.0);
        user.Username.Should().Be("hero");
        user.LastLogin.Should().Be(1300);
    }
}
