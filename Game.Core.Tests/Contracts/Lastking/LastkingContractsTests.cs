using System;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;
using Xunit;

namespace Game.Core.Tests.Contracts.Lastking;

public class LastkingContractsTests
{
    [Fact]
    public void Event_types_should_match_eventtypes_ssot()
    {
        DayStarted.EventType.Should().Be(EventTypes.LastkingDayStarted);
        NightStarted.EventType.Should().Be(EventTypes.LastkingNightStarted);
        WaveSpawned.EventType.Should().Be(EventTypes.LastkingWaveSpawned);
        CastleHpChanged.EventType.Should().Be(EventTypes.LastkingCastleHpChanged);
        RewardOffered.EventType.Should().Be(EventTypes.LastkingRewardOffered);
        SaveAutosaved.EventType.Should().Be(EventTypes.LastkingSaveAutosaved);
        TimeScaleChanged.EventType.Should().Be(EventTypes.LastkingTimeScaleChanged);
        UiFeedbackRaised.EventType.Should().Be(EventTypes.LastkingUiFeedbackRaised);
        CloudSaveSyncCompleted.EventType.Should().Be(EventTypes.LastkingCloudSaveSyncCompleted);
    }

    [Fact]
    public void Event_records_should_keep_constructor_values()
    {
        var now = DateTimeOffset.UtcNow;
        var dayStarted = new DayStarted("run-1", 1, now);
        var waveSpawned = new WaveSpawned("run-1", 1, 1, "lane-left", 50, 1200, now);
        var rewardOffered = new RewardOffered("run-1", 5, true, false, "tech+3", "gold+600", "unit+tank", now);

        dayStarted.RunId.Should().Be("run-1");
        dayStarted.DayNumber.Should().Be(1);
        dayStarted.StartedAt.Should().Be(now);

        waveSpawned.LaneId.Should().Be("lane-left");
        waveSpawned.SpawnCount.Should().Be(50);
        waveSpawned.WaveBudget.Should().Be(1200);

        rewardOffered.IsEliteNight.Should().BeTrue();
        rewardOffered.IsBossNight.Should().BeFalse();
        rewardOffered.OptionA.Should().Be("tech+3");
    }

    [Fact]
    public void Dto_records_should_be_constructible()
    {
        var computedAt = DateTimeOffset.UtcNow;
        var waveBudget = new WaveBudgetDto(5, 5, 2500, 600, 800, computedAt);
        var rewardOffer = new RewardOfferDto(5, true, false, "artifact", "gold", "tech");
        var timeScale = new TimeScaleStateDto("run-1", 100, false, computedAt);
        var feedback = new UiFeedbackDto("BUILD_INVALID_TILE", "ui.feedback.build.invalid_tile", "warning", "tile_blocked");
        var syncResult = new CloudSaveSyncResultDto("autosave", "upload", true, string.Empty, "rev-1", computedAt);

        waveBudget.DayNumber.Should().Be(5);
        waveBudget.NormalBudget.Should().Be(2500);
        waveBudget.ComputedAt.Should().Be(computedAt);

        rewardOffer.DayNumber.Should().Be(5);
        rewardOffer.IsEliteNight.Should().BeTrue();
        rewardOffer.OptionC.Should().Be("tech");

        timeScale.CurrentScalePercent.Should().Be(100);
        timeScale.IsPaused.Should().BeFalse();

        feedback.Severity.Should().Be("warning");
        feedback.MessageKey.Should().Be("ui.feedback.build.invalid_tile");

        syncResult.Success.Should().BeTrue();
        syncResult.RemoteRevision.Should().Be("rev-1");
    }

    [Fact]
    public void Interface_contract_should_compile_with_stub_implementation()
    {
        IWaveBudgetPolicy policy = new StubWaveBudgetPolicy();

        var result = policy.Compute(dayNumber: 3, nightNumber: 3, isEliteNight: false, isBossNight: false);

        result.DayNumber.Should().Be(3);
        result.NightNumber.Should().Be(3);
        result.NormalBudget.Should().Be(1800);

        ITimeScaleController timeScaleController = new StubTimeScaleController();
        var scaleState = timeScaleController.SetScale("run-1", 200, isPaused: false);
        scaleState.CurrentScalePercent.Should().Be(200);

        IFeedbackDispatcher feedbackDispatcher = new StubFeedbackDispatcher();
        var payload = new UiFeedbackDto("CODE", "ui.feedback.code", "info", "ok");
        var dispatched = feedbackDispatcher.Publish(payload);
        dispatched.MessageKey.Should().Be("ui.feedback.code");

        ICloudSaveSyncService cloudSaveSyncService = new StubCloudSaveSyncService();
        var syncResult = cloudSaveSyncService.Sync("run-1", "autosave", "upload", "steam-acc-1");
        syncResult.Success.Should().BeTrue();
    }

    private sealed class StubWaveBudgetPolicy : IWaveBudgetPolicy
    {
        public WaveBudgetDto Compute(int dayNumber, int nightNumber, bool isEliteNight, bool isBossNight)
        {
            return new WaveBudgetDto(
                DayNumber: dayNumber,
                NightNumber: nightNumber,
                NormalBudget: 1800,
                EliteBudget: isEliteNight ? 600 : 0,
                BossBudget: isBossNight ? 1200 : 0,
                ComputedAt: DateTimeOffset.UtcNow
            );
        }
    }

    private sealed class StubTimeScaleController : ITimeScaleController
    {
        public TimeScaleStateDto SetScale(string runId, int currentScalePercent, bool isPaused)
        {
            return new TimeScaleStateDto(runId, currentScalePercent, isPaused, DateTimeOffset.UtcNow);
        }
    }

    private sealed class StubFeedbackDispatcher : IFeedbackDispatcher
    {
        public UiFeedbackDto Publish(UiFeedbackDto feedback)
        {
            return feedback;
        }
    }

    private sealed class StubCloudSaveSyncService : ICloudSaveSyncService
    {
        public CloudSaveSyncResultDto Sync(string runId, string slotId, string direction, string steamAccountId)
        {
            return new CloudSaveSyncResultDto(
                slotId,
                direction,
                true,
                string.Empty,
                "rev-1",
                DateTimeOffset.UtcNow
            );
        }
    }
}
