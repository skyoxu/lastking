using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigApplicationGuardsTests
{
    [Fact]
    public void ShouldRejectForbiddenChanges_WhenRuntimeConfigAttemptsProtectedFieldMutation()
    {
        var initialState = RuntimeConfigState.CreateDefault(
            rulesetVersion: "1.0.0",
            difficultyProfileId: "standard",
            enemySpawnRate: 1.0,
            autosaveIntervalSeconds: 120);
        var candidate = RuntimeConfigPatch.Empty
            .WithRulesetVersion("2.0.0")
            .WithDifficultyProfileId("hard")
            .WithEnemySpawnRate(0.75)
            .WithAutosaveIntervalSeconds(60);

        var result = ConfigApplicationGuards.Apply(initialState, candidate);

        result.IsAccepted.Should().BeFalse();
        result.RejectedFields.Should().Contain(new[] { "rulesetVersion", "difficultyProfileId" });
        result.State.Should().Be(initialState);
    }

    // acceptance: ACC:T37.3
    [Fact]
    public void ShouldLeaveProtectedRuntimeFieldsUnchanged_WhenRejectionOccurs()
    {
        var initialState = RuntimeConfigState.CreateDefault(
            rulesetVersion: "1.0.0",
            difficultyProfileId: "standard",
            enemySpawnRate: 1.0,
            autosaveIntervalSeconds: 120);
        var candidate = RuntimeConfigPatch.Empty
            .WithDifficultyProfileId("expert")
            .WithEnemySpawnRate(0.25);

        var result = ConfigApplicationGuards.Apply(initialState, candidate);

        result.IsAccepted.Should().BeFalse();
        result.State.RulesetVersion.Should().Be("1.0.0");
        result.State.DifficultyProfileId.Should().Be("standard");
        result.State.EnemySpawnRate.Should().Be(1.0);
        result.State.AutosaveIntervalSeconds.Should().Be(120);
    }

    // acceptance: ACC:T37.6
    [Fact]
    public void ShouldNotExposePartialConfigMutation_WhenRejectedPatchMixesAllowedAndForbiddenChanges()
    {
        var initialState = RuntimeConfigState.CreateDefault(
            rulesetVersion: "1.0.0",
            difficultyProfileId: "standard",
            enemySpawnRate: 1.0,
            autosaveIntervalSeconds: 120);
        var candidate = RuntimeConfigPatch.Empty
            .WithRulesetVersion("1.1.0")
            .WithEnemySpawnRate(1.5)
            .WithAutosaveIntervalSeconds(90);

        var result = ConfigApplicationGuards.Apply(initialState, candidate);

        result.IsAccepted.Should().BeFalse();
        result.State.Should().Be(initialState);
        result.AppliedFallbackFields.Should().BeEmpty();
    }

    // acceptance: ACC:T37.9
    [Fact]
    public void ShouldApplyOnlyAllowedFallbackFields_WhenUnsafeMutableValuesAreProvided()
    {
        var initialState = RuntimeConfigState.CreateDefault(
            rulesetVersion: "1.0.0",
            difficultyProfileId: "standard",
            enemySpawnRate: 1.0,
            autosaveIntervalSeconds: 120);
        var candidate = RuntimeConfigPatch.Empty
            .WithEnemySpawnRate(-3.0)
            .WithAutosaveIntervalSeconds(0);

        var result = ConfigApplicationGuards.Apply(initialState, candidate);

        result.IsAccepted.Should().BeTrue();
        result.State.RulesetVersion.Should().Be("1.0.0");
        result.State.DifficultyProfileId.Should().Be("standard");
        result.State.EnemySpawnRate.Should().Be(1.0);
        result.State.AutosaveIntervalSeconds.Should().Be(120);
        result.AppliedFallbackFields.Should().BeEquivalentTo("enemySpawnRate", "autosaveIntervalSeconds");
    }

    // acceptance: ACC:T37.16
    [Fact]
    public void ShouldPreserveCoreLoopDecision_WhenValidationFallbackRunsForIdenticalInputs()
    {
        var initialState = RuntimeConfigState.CreateDefault(
            rulesetVersion: "1.0.0",
            difficultyProfileId: "standard",
            enemySpawnRate: 1.0,
            autosaveIntervalSeconds: 120);
        var candidate = RuntimeConfigPatch.Empty
            .WithEnemySpawnRate(-3.0)
            .WithAutosaveIntervalSeconds(0);
        var firstLoopInput = CoreLoopInput.Create(turnIndex: 7, playerPosition: 12, diceRoll: 4, treasury: 300);
        var secondLoopInput = CoreLoopInput.Create(turnIndex: 7, playerPosition: 12, diceRoll: 4, treasury: 300);

        var firstResult = ConfigApplicationGuards.Apply(initialState, candidate);
        var secondResult = ConfigApplicationGuards.Apply(initialState, candidate);
        var firstDecision = CoreLoopProjection.Project(firstLoopInput, firstResult.State);
        var secondDecision = CoreLoopProjection.Project(secondLoopInput, secondResult.State);

        firstDecision.Should().Be(secondDecision);
    }
}
