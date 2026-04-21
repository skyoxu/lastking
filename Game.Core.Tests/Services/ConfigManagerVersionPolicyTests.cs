using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigManagerVersionPolicyTests
{
    // ACC:T40.1
    [Fact]
    public void ShouldRejectLoadedConfigAndFallbackToDefault_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "2.0.0", expectedVersion: "1.0.0");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    // ACC:T40.2
    [Fact]
    public void ShouldAcceptLoadedConfig_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "1.0.0", expectedVersion: "1.0.0", daySeconds: 360);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Source.Should().Be("initial");
        result.Snapshot.DaySeconds.Should().Be(360);
    }

    // ACC:T40.3
    [Fact]
    public void ShouldUseLoadedSnapshot_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "3.1.0", expectedVersion: "3.1.0", daySeconds: 420);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().NotBe(BalanceSnapshot.Default);
        result.Snapshot.DaySeconds.Should().Be(420);
    }

    // ACC:T40.4
    [Fact]
    public void ShouldUseDefaultSnapshot_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "3.1.1", expectedVersion: "3.1.0", daySeconds: 420);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    // ACC:T40.6
    [Theory]
    [InlineData("1.0.0", "1.0.0", "initial")]
    [InlineData("1.0.1", "1.0.0", "fallback")]
    public void ShouldFallbackOnlyForMismatchInput_WhenEvaluatingEqualAndNonEqualVersions(string version, string expectedVersion, string expectedSource)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion, daySeconds: 512);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Source.Should().Be(expectedSource);
        if (expectedSource == "fallback")
        {
            result.Snapshot.Should().Be(BalanceSnapshot.Default);
        }
        else
        {
            result.Snapshot.DaySeconds.Should().Be(512);
        }
    }

    // ACC:T40.7
    [Fact]
    public void ShouldKeepLoadedSnapshotWithoutFallback_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "9.0.0", expectedVersion: "9.0.0", daySeconds: 777);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Source.Should().Be("initial");
        result.Snapshot.DaySeconds.Should().Be(777);
    }

    // ACC:T40.8
    [Fact]
    public void ShouldRejectAndRequireMigrationReason_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "5.0.0", expectedVersion: "4.9.0");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain("CFG_VERSION_MISMATCH");
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
    }

    // ACC:T40.9
    [Fact]
    public void ShouldProduceDeterministicOutcome_WhenApplyingSameMismatchInputTwice()
    {
        var firstManager = new ConfigManager();
        var secondManager = new ConfigManager();
        var json = CreateConfigJson(version: "2026.04", expectedVersion: "2026.03");

        var firstResult = firstManager.LoadInitialFromJson(json, "res://Config/balance.json");
        var secondResult = secondManager.LoadInitialFromJson(json, "res://Config/balance.json");

        firstResult.Accepted.Should().BeFalse();
        secondResult.Accepted.Should().BeFalse();
        firstResult.Source.Should().Be("fallback");
        secondResult.Source.Should().Be("fallback");
        firstResult.ReasonCodes.Should().Equal(secondResult.ReasonCodes);
    }

    // ACC:T40.10
    [Fact]
    public void ShouldNotPartiallyAcceptLoadedSnapshot_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "6.0.1", expectedVersion: "6.0.0", daySeconds: 999);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Snapshot.DaySeconds.Should().NotBe(999);
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    // ACC:T40.11
    [Theory]
    [InlineData("1.0.1", "1.0.0")]
    [InlineData("1.0.0", "1.0.0 ")]
    [InlineData("v1", "V1")]
    public void ShouldRejectNonEqualVersionTokens_WhenApplyingStrictEqualityRule(string version, string expectedVersion)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    // ACC:T40.12
    [Fact]
    public void ShouldRejectConfigEvenWithForceMigration_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "2.1.0", expectedVersion: "2.0.0", forceMigration: true);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
    }

    // ACC:T40.17
    [Fact]
    public void ShouldRejectConfigWithMigrationSection_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(
            version: "10.0.0",
            expectedVersion: "9.0.0",
            daySeconds: 600,
            forceMigration: true,
            includeMigrationSection: true);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    // ACC:T40.18
    [Fact]
    public void ShouldRejectCaseVariantToken_WhenVersionTokenDiffersByCase()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "v2", expectedVersion: "V2");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
    }

    // ACC:T40.19
    [Fact]
    public void ShouldMarkInitialSource_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "7.0.0", expectedVersion: "7.0.0");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Source.Should().Be("initial");
        result.Source.Should().NotBe("fallback");
    }

    // ACC:T40.20
    [Theory]
    [InlineData("1.0.0", "1.0.0", false, true, "initial")]
    [InlineData("1.0.1", "1.0.0", false, false, "fallback")]
    [InlineData("2.1.0", "2.0.0", true, false, "fallback")]
    public void ShouldExposeAcceptedAndRejectedPaths_WhenRunningVersionPolicyMatrix(string version, string expectedVersion, bool forceMigration, bool expectedAccepted, string expectedSource)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion, forceMigration: forceMigration);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().Be(expectedAccepted);
        result.Source.Should().Be(expectedSource);
    }

    // ACC:T40.21
    [Fact]
    public void ShouldKeepActiveConfigJson_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "11.0.0", expectedVersion: "11.0.0");

        manager.LoadInitialFromJson(json, "res://Config/balance.json");

        manager.ActiveConfigJson.Should().Be(json);
    }

    // ACC:T40.22
    [Fact]
    public void ShouldKeepActiveConfigJsonEmpty_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "11.0.1", expectedVersion: "11.0.0");

        manager.LoadInitialFromJson(json, "res://Config/balance.json");

        manager.ActiveConfigJson.Should().BeEmpty();
    }

    // ACC:T40.23
    [Fact]
    public void ShouldEmitVersionMismatchReasons_WhenVersionDoesNotMatchExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "8.5.0", expectedVersion: "8.4.9");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain("CFG_VERSION_MISMATCH");
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
    }

    // ACC:T40.24
    [Theory]
    [InlineData("A", "a")]
    [InlineData("1.0.0", "1.0.0\t")]
    [InlineData("2026.04", "2026.04.0")]
    public void ShouldRejectAllNonEqualVersions_WhenExpectedVersionEqualityRuleIsApplied(string version, string expectedVersion)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    private static string CreateConfigJson(
        string version,
        string expectedVersion,
        int daySeconds = 240,
        bool forceMigration = false,
        bool includeMigrationSection = false)
    {
        var forceMigrationLiteral = forceMigration ? "true" : "false";
        var migrationSection = includeMigrationSection
            ? @",
  ""migration"": {
    ""recoverable"": true,
    ""status"": ""succeeded"",
    ""errorCode"": ""CFG_MIGRATION_FATAL""
  }"
            : string.Empty;

        return $@"{{
  ""version"": ""{version}"",
  ""expectedVersion"": ""{expectedVersion}"",
  ""forceMigration"": {forceMigrationLiteral}{migrationSection},
  ""time"": {{ ""day_seconds"": {daySeconds}, ""night_seconds"": 120 }},
  ""waves"": {{ ""normal"": {{ ""day1_budget"": 50, ""daily_growth"": 1.2 }} }},
  ""channels"": {{ ""elite"": ""elite"", ""boss"": ""boss"" }},
  ""spawn"": {{ ""cadence_seconds"": 10 }},
  ""boss"": {{ ""count"": 2 }}
}}";
    }
}
