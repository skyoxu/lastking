using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigManagerTests
{
    [Fact]
    public void ShouldAcceptConfigAndPreserveLoadedState_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "1.0.0", expectedVersion: "1.0.0", forceMigration: false, includeFatalMigration: false);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Source.Should().Be("initial");
        result.ReasonCodes.Should().BeEmpty();
        result.Snapshot.DaySeconds.Should().Be(240);
        result.Snapshot.NightSeconds.Should().Be(120);
        manager.ActiveConfigJson.Should().Be(json);
    }

    // ACC:T40.13
    [Theory]
    [InlineData("1.0.1", "1.0.0")]
    [InlineData("1.0.0", "1.0.0 ")]
    [InlineData("v2", "V2")]
    public void ShouldRejectConfigAndKeepDefaultState_WhenVersionDoesNotMatchExpectedVersion(string version, string expectedVersion)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion, forceMigration: false, includeFatalMigration: false);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
        manager.Snapshot.Should().Be(BalanceSnapshot.Default);
        manager.ActiveConfigJson.Should().BeEmpty();
        result.ReasonCodes.Should().Contain("CFG_VERSION_MISMATCH");
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
    }

    // ACC:T40.15
    [Fact]
    public void ShouldRejectConfigAndEmitFatalDiagnostics_WhenForceMigrationFailsUnrecoverably()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "2.0.0", expectedVersion: "1.0.0", forceMigration: true, includeFatalMigration: true);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
        manager.Snapshot.Should().Be(BalanceSnapshot.Default);
        manager.ActiveConfigJson.Should().BeEmpty();
        result.ReasonCodes.Should().Contain("CFG_MIGRATION_FATAL");
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
    }

    private static string CreateConfigJson(string version, string expectedVersion, bool forceMigration, bool includeFatalMigration)
    {
        var forceMigrationLiteral = forceMigration ? "true" : "false";
        var migrationSection = includeFatalMigration
            ? @",
  ""migration"": {
    ""recoverable"": false,
    ""status"": ""failed"",
    ""errorCode"": ""CFG_MIGRATION_FATAL""
  }"
            : string.Empty;

        return $@"{{
  ""version"": ""{version}"",
  ""expectedVersion"": ""{expectedVersion}"",
  ""forceMigration"": {forceMigrationLiteral}{migrationSection},
  ""time"": {{ ""day_seconds"": 240, ""night_seconds"": 120 }},
  ""waves"": {{ ""normal"": {{ ""day1_budget"": 50, ""daily_growth"": 1.2 }} }},
  ""channels"": {{ ""elite"": ""elite"", ""boss"": ""boss"" }},
  ""spawn"": {{ ""cadence_seconds"": 10 }},
  ""boss"": {{ ""count"": 2 }}
}}";
    }
}
