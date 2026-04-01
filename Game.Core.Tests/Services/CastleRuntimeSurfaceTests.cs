using System.Reflection;
using FluentAssertions;
using Game.Core.Services;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class CastleRuntimeSurfaceTests
{
    // ACC:T7.13
    [Fact]
    public void ShouldExposeReadableCurrentHpAndHpChangedPayload_WhenRuntimeConsumersQueryIt()
    {
        var runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(15),
            new GameStateMachine(),
            "run-7",
            1);

        var result = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            4);

        runtime.CurrentHp.Should().Be(11);
        result.CastleHpChangedEvent.Should().NotBeNull();
        result.CastleHpChangedEvent!.PreviousHp.Should().Be(15);
        result.CastleHpChangedEvent.CurrentHp.Should().Be(11);
    }

    // ACC:T7.15
    [Fact]
    public void ShouldProvideConcreteCurrentHpProperty_WhenGameplayAndUiBindRuntimeState()
    {
        var property = typeof(CastleBattleRuntime).GetProperty(
            nameof(CastleBattleRuntime.CurrentHp),
            BindingFlags.Instance | BindingFlags.Public);
        var runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(20),
            new GameStateMachine(),
            "run-7",
            1);

        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.PropertyType.Should().Be(typeof(int));
        property.GetValue(runtime).Should().Be(20);
    }
}
