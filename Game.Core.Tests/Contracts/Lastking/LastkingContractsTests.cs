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
        BootstrapReady.EventType.Should().Be(EventTypes.LastkingBootstrapReady);
        ConfigLoaded.EventType.Should().Be(EventTypes.LastkingConfigLoaded);
        ResourcesChanged.EventType.Should().Be(EventTypes.LastkingResourcesChanged);
        TaxCollected.EventType.Should().Be(EventTypes.LastkingTaxCollected);
        TechApplied.EventType.Should().Be(EventTypes.LastkingTechApplied);
        WindowsRuntimeValidated.EventType.Should().Be(EventTypes.LastkingWindowsRuntimeValidated);
        CameraScrolled.EventType.Should().Be(EventTypes.LastkingCameraScrolled);
        AudioSettingsChanged.EventType.Should().Be(EventTypes.LastkingAudioSettingsChanged);
        PerfSampled.EventType.Should().Be(EventTypes.LastkingPerfSampled);
    }

    [Fact]
    public void Event_records_should_keep_constructor_values()
    {
        var now = DateTimeOffset.UtcNow;
        var dayStarted = new DayStarted("run-1", 1, now);
        var waveSpawned = new WaveSpawned("run-1", 1, 1, "lane-left", 50, 1200, now);
        var rewardOffered = new RewardOffered("run-1", 5, true, false, "tech+3", "gold+600", "unit+tank", now);
        var bootstrapReady = new BootstrapReady("run-1", "F:/Lastking", true, now);
        var configLoaded = new ConfigLoaded("run-1", "1.2.0", "hash-1", "config/samples", now);
        var resourcesChanged = new ResourcesChanged("run-1", 1, 800, 150, 50, now);
        var taxCollected = new TaxCollected("run-1", 1, "residence-01", 50, 850, now);
        var techApplied = new TechApplied("run-1", "tech-rate-1", "attack_speed_percent", 100, 110, now);
        var windowsValidated = new WindowsRuntimeValidated("run-1", "480", true, "startup+export", now);
        var cameraScrolled = new CameraScrolled("run-1", 320, 128, "edge", now);
        var audioSettingsChanged = new AudioSettingsChanged("run-1", 60, 75, now);
        var perfSampled = new PerfSampled("run-1", "main", 60, 45, 300, now);

        dayStarted.RunId.Should().Be("run-1");
        dayStarted.DayNumber.Should().Be(1);
        dayStarted.StartedAt.Should().Be(now);

        waveSpawned.LaneId.Should().Be("lane-left");
        waveSpawned.SpawnCount.Should().Be(50);
        waveSpawned.WaveBudget.Should().Be(1200);

        rewardOffered.IsEliteNight.Should().BeTrue();
        rewardOffered.IsBossNight.Should().BeFalse();
        rewardOffered.OptionA.Should().Be("tech+3");

        bootstrapReady.ProjectRoot.Should().Be("F:/Lastking");
        bootstrapReady.ExportProfileReady.Should().BeTrue();

        configLoaded.ConfigVersion.Should().Be("1.2.0");
        configLoaded.ConfigHash.Should().Be("hash-1");

        resourcesChanged.Gold.Should().Be(800);
        resourcesChanged.Iron.Should().Be(150);

        taxCollected.GoldDelta.Should().Be(50);
        taxCollected.TotalGold.Should().Be(850);

        techApplied.StatKey.Should().Be("attack_speed_percent");
        techApplied.CurrentValue.Should().Be(110);

        windowsValidated.StartupPassed.Should().BeTrue();
        windowsValidated.ValidationScope.Should().Be("startup+export");

        cameraScrolled.InputMode.Should().Be("edge");
        cameraScrolled.PositionX.Should().Be(320);

        audioSettingsChanged.MusicVolumePercent.Should().Be(60);
        audioSettingsChanged.SfxVolumePercent.Should().Be(75);

        perfSampled.AverageFps.Should().Be(60);
        perfSampled.Low1PercentFps.Should().Be(45);
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
