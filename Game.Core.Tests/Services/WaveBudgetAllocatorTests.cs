using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveBudgetAllocatorTests
{
    // ACC:T4.14
    [Theory]
    [InlineData("normal")]
    [InlineData("elite")]
    [InlineData("boss")]
    [InlineData("accountingVersion")]
    public void ShouldRejectMutationAndKeepStateUnchanged_WhenAnyLockedFieldIsMutatedDuringWaveExecution(string mutationTarget)
    {
        var initialState = new WaveAccountingState(
            Normal: new ChannelAccounting(50, 30, 20, 10),
            Elite: new ChannelAccounting(20, 12, 7, 5),
            Boss: new ChannelAccounting(10, 5, 2, 3),
            AccountingVersion: 4);

        var sut = new WaveBudgetAllocator(initialState);
        var beforeVersion = sut.Current.AccountingVersion;
        var beforeState = sut.Current;

        var attempt = CreateSingleFieldMutationAttempt(mutationTarget);

        var accepted = sut.TryApplyMutationDuringWaveExecution(attempt);

        accepted.Should().BeFalse();
        sut.Current.AccountingVersion.Should().Be(beforeVersion);
        sut.Current.Should().Be(beforeState);
    }

    // ACC:T4.15
    [Fact]
    public void ShouldKeepNormalEliteBossInputAllocatedSpentRemainingBitwiseEquivalent_WhenLockedMutationIsRejected()
    {
        var initialState = new WaveAccountingState(
            Normal: new ChannelAccounting(50, 30, 20, 10),
            Elite: new ChannelAccounting(20, 12, 7, 5),
            Boss: new ChannelAccounting(10, 5, 2, 3),
            AccountingVersion: 4);

        var sut = new WaveBudgetAllocator(initialState);
        var beforeBytes = CaptureChannelAccountingBytes(sut.Current);

        var attempt = new WaveMutationAttempt(
            Normal: new ChannelAccounting(51, 31, 21, 11),
            Elite: new ChannelAccounting(22, 13, 8, 5),
            Boss: new ChannelAccounting(11, 6, 3, 2),
            AccountingVersion: 5);

        var accepted = sut.TryApplyMutationDuringWaveExecution(attempt);
        var afterBytes = CaptureChannelAccountingBytes(sut.Current);

        accepted.Should().BeFalse();
        afterBytes.Should().Equal(beforeBytes);
    }

    private static byte[] CaptureChannelAccountingBytes(WaveAccountingState state)
    {
        var bytes = new List<byte>(sizeof(int) * 12);
        AppendChannel(bytes, state.Normal);
        AppendChannel(bytes, state.Elite);
        AppendChannel(bytes, state.Boss);
        return bytes.ToArray();
    }

    private static void AppendChannel(List<byte> bytes, ChannelAccounting channel)
    {
        bytes.AddRange(BitConverter.GetBytes(channel.InputBudget));
        bytes.AddRange(BitConverter.GetBytes(channel.Allocated));
        bytes.AddRange(BitConverter.GetBytes(channel.Spent));
        bytes.AddRange(BitConverter.GetBytes(channel.Remaining));
    }

    private static WaveMutationAttempt CreateSingleFieldMutationAttempt(string mutationTarget)
    {
        return mutationTarget switch
        {
            "normal" => new WaveMutationAttempt(
                Normal: new ChannelAccounting(999, 30, 20, 10),
                Elite: null,
                Boss: null,
                AccountingVersion: null),
            "elite" => new WaveMutationAttempt(
                Normal: null,
                Elite: new ChannelAccounting(20, 999, 7, 5),
                Boss: null,
                AccountingVersion: null),
            "boss" => new WaveMutationAttempt(
                Normal: null,
                Elite: null,
                Boss: new ChannelAccounting(10, 5, 999, 3),
                AccountingVersion: null),
            "accountingVersion" => new WaveMutationAttempt(
                Normal: null,
                Elite: null,
                Boss: null,
                AccountingVersion: 99),
            _ => throw new ArgumentOutOfRangeException(nameof(mutationTarget), mutationTarget, "Unknown mutation target.")
        };
    }
}
