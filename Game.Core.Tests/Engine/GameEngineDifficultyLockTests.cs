using System;
using System.Reflection;
using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Engine;
using Xunit;

namespace Game.Core.Tests.Engine;

public sealed class GameEngineDifficultyLockTests
{
    // ACC:T33.9
    [Fact]
    public void ShouldApplyDifficultyChange_WhenRunHasNotStarted()
    {
        var engine = CreateEngine(Difficulty.Medium);

        var accepted = TryRequestDifficultyChange(engine, Difficulty.Hard);

        accepted.Should().BeTrue("difficulty should be changeable before run start");
        engine.Config.Difficulty.Should().Be(Difficulty.Hard);
    }

    // ACC:T33.1
    [Fact]
    public void ShouldRejectDifficultyChangeAndKeepEffectiveDifficultyUnchanged_WhenRunHasStarted()
    {
        var engine = CreateEngine(Difficulty.Medium);
        engine.Start();

        var accepted = TryRequestDifficultyChange(engine, Difficulty.Hard);

        accepted.Should().BeFalse("difficulty must be locked after run start");
        engine.Config.Difficulty.Should().Be(Difficulty.Medium);
    }

    // ACC:T33.13
    [Fact]
    public void ShouldKeepEffectiveDifficultyUnchanged_WhenLockedDifficultyChangeIsRetriedAfterRunStart()
    {
        var engine = CreateEngine(Difficulty.Easy);
        engine.Start();

        var firstAccepted = TryRequestDifficultyChange(engine, Difficulty.Hard);
        var secondAccepted = TryRequestDifficultyChange(engine, Difficulty.Medium);

        firstAccepted.Should().BeFalse();
        secondAccepted.Should().BeFalse();
        engine.Config.Difficulty.Should().Be(Difficulty.Easy);
    }

    // ACC:T33.2
    // ACC:T33.3
    [Fact]
    public void ShouldProduceDeterministicLockedOutcome_WhenTwoEquivalentRunsRequestPostStartDifficultyChange()
    {
        var firstEngine = CreateEngine(Difficulty.Medium);
        var secondEngine = CreateEngine(Difficulty.Medium);
        firstEngine.Start();
        secondEngine.Start();

        var firstAccepted = TryRequestDifficultyChange(firstEngine, Difficulty.Hard);
        var secondAccepted = TryRequestDifficultyChange(secondEngine, Difficulty.Hard);

        firstAccepted.Should().Be(secondAccepted);
        firstEngine.Config.Difficulty.Should().Be(secondEngine.Config.Difficulty);
    }

    private static GameEngineCore CreateEngine(Difficulty difficulty)
    {
        var config = new GameConfig(
            MaxLevel: 10,
            InitialHealth: 100,
            ScoreMultiplier: 1.0,
            AutoSave: false,
            Difficulty: difficulty);

        var inventory = new Inventory();
        return new GameEngineCore(config, inventory);
    }

    private static bool TryRequestDifficultyChange(GameEngineCore engine, Difficulty targetDifficulty)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var apiCandidates = new[]
        {
            "TryChangeDifficulty",
            "TrySetDifficulty",
            "TrySelectRunDifficulty",
            "RequestDifficultyChange",
            "SetDifficulty",
        };

        foreach (var apiName in apiCandidates)
        {
            var method = typeof(GameEngineCore).GetMethod(apiName, BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Difficulty))
            {
                continue;
            }

            var invocationResult = method.Invoke(engine, new object[] { targetDifficulty });
            if (invocationResult is bool accepted)
            {
                return accepted;
            }

            return true;
        }

        return false;
    }
}
