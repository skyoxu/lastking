using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services.Reward;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RewardChoiceSelectionTests
{
    // ACC:T18.13
    [Fact]
    [Trait("acceptance", "ACC:T18.13")]
    public void ShouldApplyExactlyOneCorrespondingRewardEffect_WhenSelectingOneChoice()
    {
        var initialState = new RewardState
        {
            Gold = 100,
            Tech = 3,
            Units = 1,
        };

        var rewardEffects = new Dictionary<RewardChoice, System.Action<RewardState>>
        {
            [RewardChoice.OptionA] = state => state.Tech += 2,
            [RewardChoice.OptionB] = state => state.Gold += 50,
            [RewardChoice.OptionC] = state => state.Units += 1,
        };

        var sut = new RewardChoiceSelectionService(rewardEffects);

        var resultState = sut.ApplySelection(initialState, RewardChoice.OptionB);

        resultState.Gold.Should().Be(150);
        resultState.Tech.Should().Be(3);
        resultState.Units.Should().Be(1);
    }

    [Fact]
    public void ShouldKeepStateUnchanged_WhenSelectingChoiceThatIsNotOffered()
    {
        var initialState = new RewardState
        {
            Gold = 25,
            Tech = 7,
            Units = 4,
        };

        var rewardEffects = new Dictionary<RewardChoice, System.Action<RewardState>>
        {
            [RewardChoice.OptionB] = state => state.Gold += 10,
        };

        var sut = new RewardChoiceSelectionService(rewardEffects);

        var resultState = sut.ApplySelection(initialState, RewardChoice.OptionC);

        resultState.Should().BeEquivalentTo(initialState);
    }

}
