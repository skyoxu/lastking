using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public class Task10BossChannelIsolationTests
{
    // ACC:T10.7
    // ACC:T10.11
    [Fact]
    public void ShouldKeepOtherChannelBudgetsUnchanged_WhenLockingBossCountForBossNight()
    {
        var allocator = new BossChannelAllocator(new Dictionary<string, int>
        {
            ["normal"] = 30,
            ["elite"] = 20,
            ["boss-night"] = 14
        });

        var isLocked = allocator.TryLockForChannel("boss-night", bossCount: 2, costPerBoss: 3);

        isLocked.Should().BeTrue();
        allocator.Budgets["boss-night"].Should().Be(8);
        allocator.Budgets["normal"].Should().Be(30);
        allocator.Budgets["elite"].Should().Be(20);
    }

    [Fact]
    public void ShouldRefuseLockAndLeaveBudgetsUnchanged_WhenChannelBudgetIsInsufficient()
    {
        var allocator = new BossChannelAllocator(new Dictionary<string, int>
        {
            ["normal"] = 5,
            ["elite"] = 20,
            ["boss-night"] = 14
        });

        var isLocked = allocator.TryLockForChannel("normal", bossCount: 2, costPerBoss: 3);

        isLocked.Should().BeFalse();
        allocator.Budgets["normal"].Should().Be(5);
        allocator.Budgets["elite"].Should().Be(20);
        allocator.Budgets["boss-night"].Should().Be(14);
    }

    // ACC:T10.15
    [Fact]
    public void ShouldDeductEqualPerChannelCost_WhenApplyingFixedBossCountAcrossScenarios()
    {
        var allocator = new BossChannelAllocator(new Dictionary<string, int>
        {
            ["normal"] = 30,
            ["elite"] = 20,
            ["boss-night"] = 14
        });

        var isLocked = allocator.TryLockForAllChannels(bossCount: 2, costPerBoss: 3);

        isLocked.Should().BeTrue();
        allocator.Budgets["normal"].Should().Be(24);
        allocator.Budgets["elite"].Should().Be(14);
        allocator.Budgets["boss-night"].Should().Be(8);
    }

    private sealed class BossChannelAllocator
    {
        private readonly Dictionary<string, int> channelBudgets;

        public BossChannelAllocator(IDictionary<string, int> initialBudgets)
        {
            channelBudgets = new Dictionary<string, int>(initialBudgets);
        }

        public IReadOnlyDictionary<string, int> Budgets => channelBudgets;

        public bool TryLockForChannel(string channelName, int bossCount, int costPerBoss)
        {
            if (!channelBudgets.TryGetValue(channelName, out var channelBudget))
            {
                return false;
            }

            var debit = checked(bossCount * costPerBoss);
            if (channelBudget < debit)
            {
                return false;
            }

            channelBudgets[channelName] -= debit;

            return true;
        }

        public bool TryLockForAllChannels(int bossCount, int costPerBoss)
        {
            var debit = checked(bossCount * costPerBoss);
            if (channelBudgets.Values.Any(value => value < debit))
            {
                return false;
            }

            foreach (var key in channelBudgets.Keys.ToList())
            {
                channelBudgets[key] -= debit;
            }

            return true;
        }
    }
}
