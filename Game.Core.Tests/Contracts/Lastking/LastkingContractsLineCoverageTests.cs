using System;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Contracts.Guild;
using Game.Core.Contracts.Lastking;
using Xunit;

namespace Game.Core.Tests.Contracts.Lastking;

public class LastkingContractsLineCoverageTests
{
    [Fact]
    public void ShouldExposeAllFieldsAndConstants_WhenEventRecordsConstructed()
    {
        var now = DateTimeOffset.UtcNow;
        var timeScaleChanged = new TimeScaleChanged("run-1", 100, 200, false, now);
        var cloudSaveSyncCompleted = new CloudSaveSyncCompleted("run-1", "slot-1", "upload", "steam-1", true, "OK", "rev-1", now);
        var rewardOffered = new RewardOffered("run-1", 5, true, false, "a", "b", "c", now);
        var waveSpawned = new WaveSpawned("run-1", 5, 5, "left", 50, 1500, now);
        var uiFeedbackRaised = new UiFeedbackRaised("run-1", "CODE", "ui.key", "warning", "details", now);
        var techApplied = new TechApplied("run-1", "tech-1", "damage", 10, 12, now);
        var taxCollected = new TaxCollected("run-1", 5, "res-1", 50, 900, now);
        var resourcesChanged = new ResourcesChanged("run-1", 5, 900, 200, 60, now);
        var perfSampled = new PerfSampled("run-1", "main", 60, 45, 300, now);
        var saveAutosaved = new SaveAutosaved("run-1", 5, "autosave", "hash", now);
        var windowsRuntimeValidated = new WindowsRuntimeValidated("run-1", "480", true, "startup+export", now);
        var configLoaded = new ConfigLoaded("run-1", "1.2.0", "hash", "cfg/path", now);
        var castleHpChanged = new CastleHpChanged("run-1", 5, 1000, 800, now);
        var cameraScrolled = new CameraScrolled("run-1", 128, 64, "edge", now);
        var nightStarted = new NightStarted("run-1", 5, 5, now);
        var guildJoined = new GuildMemberJoined("u-1", "g-1", now, "member");

        timeScaleChanged.RunId.Should().Be("run-1");
        timeScaleChanged.PreviousScalePercent.Should().Be(100);
        timeScaleChanged.CurrentScalePercent.Should().Be(200);
        timeScaleChanged.IsPaused.Should().BeFalse();
        timeScaleChanged.ChangedAt.Should().Be(now);
        TimeScaleChanged.EventType.Should().Be(EventTypes.LastkingTimeScaleChanged);

        cloudSaveSyncCompleted.RunId.Should().Be("run-1");
        cloudSaveSyncCompleted.SlotId.Should().Be("slot-1");
        cloudSaveSyncCompleted.Direction.Should().Be("upload");
        cloudSaveSyncCompleted.SteamAccountId.Should().Be("steam-1");
        cloudSaveSyncCompleted.Success.Should().BeTrue();
        cloudSaveSyncCompleted.ErrorCode.Should().Be("OK");
        cloudSaveSyncCompleted.RemoteRevision.Should().Be("rev-1");
        cloudSaveSyncCompleted.SyncedAt.Should().Be(now);
        CloudSaveSyncCompleted.EventType.Should().Be(EventTypes.LastkingCloudSaveSyncCompleted);

        rewardOffered.RunId.Should().Be("run-1");
        rewardOffered.DayNumber.Should().Be(5);
        rewardOffered.IsEliteNight.Should().BeTrue();
        rewardOffered.IsBossNight.Should().BeFalse();
        rewardOffered.OptionA.Should().Be("a");
        rewardOffered.OptionB.Should().Be("b");
        rewardOffered.OptionC.Should().Be("c");
        rewardOffered.OfferedAt.Should().Be(now);
        RewardOffered.EventType.Should().Be(EventTypes.LastkingRewardOffered);

        waveSpawned.RunId.Should().Be("run-1");
        waveSpawned.DayNumber.Should().Be(5);
        waveSpawned.NightNumber.Should().Be(5);
        waveSpawned.LaneId.Should().Be("left");
        waveSpawned.SpawnCount.Should().Be(50);
        waveSpawned.WaveBudget.Should().Be(1500);
        waveSpawned.SpawnedAt.Should().Be(now);
        WaveSpawned.EventType.Should().Be(EventTypes.LastkingWaveSpawned);

        uiFeedbackRaised.RunId.Should().Be("run-1");
        uiFeedbackRaised.Code.Should().Be("CODE");
        uiFeedbackRaised.MessageKey.Should().Be("ui.key");
        uiFeedbackRaised.Severity.Should().Be("warning");
        uiFeedbackRaised.Details.Should().Be("details");
        uiFeedbackRaised.RaisedAt.Should().Be(now);
        UiFeedbackRaised.EventType.Should().Be(EventTypes.LastkingUiFeedbackRaised);

        techApplied.RunId.Should().Be("run-1");
        techApplied.TechId.Should().Be("tech-1");
        techApplied.StatKey.Should().Be("damage");
        techApplied.PreviousValue.Should().Be(10);
        techApplied.CurrentValue.Should().Be(12);
        techApplied.AppliedAt.Should().Be(now);
        TechApplied.EventType.Should().Be(EventTypes.LastkingTechApplied);

        taxCollected.RunId.Should().Be("run-1");
        taxCollected.DayNumber.Should().Be(5);
        taxCollected.ResidenceId.Should().Be("res-1");
        taxCollected.GoldDelta.Should().Be(50);
        taxCollected.TotalGold.Should().Be(900);
        taxCollected.CollectedAt.Should().Be(now);
        TaxCollected.EventType.Should().Be(EventTypes.LastkingTaxCollected);

        resourcesChanged.RunId.Should().Be("run-1");
        resourcesChanged.DayNumber.Should().Be(5);
        resourcesChanged.Gold.Should().Be(900);
        resourcesChanged.Iron.Should().Be(200);
        resourcesChanged.PopulationCap.Should().Be(60);
        resourcesChanged.ChangedAt.Should().Be(now);
        ResourcesChanged.EventType.Should().Be(EventTypes.LastkingResourcesChanged);

        perfSampled.RunId.Should().Be("run-1");
        perfSampled.SceneId.Should().Be("main");
        perfSampled.AverageFps.Should().Be(60);
        perfSampled.Low1PercentFps.Should().Be(45);
        perfSampled.SampleCount.Should().Be(300);
        perfSampled.SampledAt.Should().Be(now);
        PerfSampled.EventType.Should().Be(EventTypes.LastkingPerfSampled);

        saveAutosaved.RunId.Should().Be("run-1");
        saveAutosaved.DayNumber.Should().Be(5);
        saveAutosaved.SlotId.Should().Be("autosave");
        saveAutosaved.ConfigHash.Should().Be("hash");
        saveAutosaved.SavedAt.Should().Be(now);
        SaveAutosaved.EventType.Should().Be(EventTypes.LastkingSaveAutosaved);

        windowsRuntimeValidated.RunId.Should().Be("run-1");
        windowsRuntimeValidated.SteamAppId.Should().Be("480");
        windowsRuntimeValidated.StartupPassed.Should().BeTrue();
        windowsRuntimeValidated.ValidationScope.Should().Be("startup+export");
        windowsRuntimeValidated.ValidatedAt.Should().Be(now);
        WindowsRuntimeValidated.EventType.Should().Be(EventTypes.LastkingWindowsRuntimeValidated);

        configLoaded.RunId.Should().Be("run-1");
        configLoaded.ConfigVersion.Should().Be("1.2.0");
        configLoaded.ConfigHash.Should().Be("hash");
        configLoaded.SourcePath.Should().Be("cfg/path");
        configLoaded.LoadedAt.Should().Be(now);
        ConfigLoaded.EventType.Should().Be(EventTypes.LastkingConfigLoaded);

        castleHpChanged.RunId.Should().Be("run-1");
        castleHpChanged.DayNumber.Should().Be(5);
        castleHpChanged.PreviousHp.Should().Be(1000);
        castleHpChanged.CurrentHp.Should().Be(800);
        castleHpChanged.ChangedAt.Should().Be(now);
        CastleHpChanged.EventType.Should().Be(EventTypes.LastkingCastleHpChanged);

        cameraScrolled.RunId.Should().Be("run-1");
        cameraScrolled.PositionX.Should().Be(128);
        cameraScrolled.PositionY.Should().Be(64);
        cameraScrolled.InputMode.Should().Be("edge");
        cameraScrolled.ScrolledAt.Should().Be(now);
        CameraScrolled.EventType.Should().Be(EventTypes.LastkingCameraScrolled);

        nightStarted.RunId.Should().Be("run-1");
        nightStarted.DayNumber.Should().Be(5);
        nightStarted.NightNumber.Should().Be(5);
        nightStarted.StartedAt.Should().Be(now);
        NightStarted.EventType.Should().Be(EventTypes.LastkingNightStarted);

        guildJoined.UserId.Should().Be("u-1");
        guildJoined.GuildId.Should().Be("g-1");
        guildJoined.JoinedAt.Should().Be(now);
        guildJoined.Role.Should().Be("member");
        GuildMemberJoined.EventType.Should().Be(EventTypes.GuildMemberJoined);
    }

    [Fact]
    public void ShouldExposeAllFields_WhenDtoRecordsConstructed()
    {
        var now = DateTimeOffset.UtcNow;
        var waveBudget = new WaveBudgetDto(3, 3, 1800, 600, 0, now);
        var rewardOffer = new RewardOfferDto(3, false, false, "artifact", "gold", "tech");
        var feedback = new UiFeedbackDto("ERR", "ui.feedback.err", "error", "none");
        var timeScaleState = new TimeScaleStateDto("run-1", 100, false, now);
        var cloudResult = new CloudSaveSyncResultDto("slot", "upload", true, string.Empty, "rev-2", now);

        waveBudget.DayNumber.Should().Be(3);
        waveBudget.NightNumber.Should().Be(3);
        waveBudget.NormalBudget.Should().Be(1800);
        waveBudget.EliteBudget.Should().Be(600);
        waveBudget.BossBudget.Should().Be(0);
        waveBudget.ComputedAt.Should().Be(now);

        rewardOffer.DayNumber.Should().Be(3);
        rewardOffer.IsEliteNight.Should().BeFalse();
        rewardOffer.IsBossNight.Should().BeFalse();
        rewardOffer.OptionA.Should().Be("artifact");
        rewardOffer.OptionB.Should().Be("gold");
        rewardOffer.OptionC.Should().Be("tech");

        feedback.Code.Should().Be("ERR");
        feedback.MessageKey.Should().Be("ui.feedback.err");
        feedback.Severity.Should().Be("error");
        feedback.Details.Should().Be("none");

        timeScaleState.RunId.Should().Be("run-1");
        timeScaleState.CurrentScalePercent.Should().Be(100);
        timeScaleState.IsPaused.Should().BeFalse();
        timeScaleState.UpdatedAt.Should().Be(now);

        cloudResult.SlotId.Should().Be("slot");
        cloudResult.Direction.Should().Be("upload");
        cloudResult.Success.Should().BeTrue();
        cloudResult.ErrorCode.Should().Be(string.Empty);
        cloudResult.RemoteRevision.Should().Be("rev-2");
        cloudResult.SyncedAt.Should().Be(now);
    }
}
