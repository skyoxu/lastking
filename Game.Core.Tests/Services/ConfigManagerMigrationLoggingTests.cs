using FluentAssertions;
using Game.Core.Ports;
using Game.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigManagerMigrationLoggingTests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Errors { get; } = new();

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Error(string message)
        {
            Errors.Add(message);
        }

        public void Error(string message, Exception ex)
        {
            Errors.Add($"{message} | {ex.GetType().Name}");
        }
    }

    // ACC:T40.1
    [Fact]
    public void ShouldRejectAndFallbackToDefault_WhenVersionDoesNotMatchExpectedVersion()
    {
        var logger = new CapturingLogger();
        var manager = new ConfigManager(logger);
        var json = CreateConfigJson(version: "2.0.0", expectedVersion: "1.0.0");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
        logger.Errors.Should().ContainSingle(message =>
            message.Contains("migration required", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("version='2.0.0'", StringComparison.Ordinal) &&
            message.Contains("expectedVersion='1.0.0'", StringComparison.Ordinal));
    }

    // ACC:T40.2
    [Fact]
    public void ShouldAcceptLoadedConfig_WhenVersionMatchesExpectedVersion()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version: "1.0.0", expectedVersion: "1.0.0");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Source.Should().Be("initial");
        result.ReasonCodes.Should().BeEmpty();
    }

    // ACC:T40.5
    [Fact]
    public void ShouldEmitMigrationRequiredReasonCodes_WhenVersionMismatchOccurs()
    {
        var logger = new CapturingLogger();
        var manager = new ConfigManager(logger);
        var json = CreateConfigJson(version: "1.0.1", expectedVersion: "1.0.0");

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain("CFG_VERSION_MISMATCH");
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
        logger.Errors.Should().ContainSingle(message =>
            message.Contains("migration required", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("version='1.0.1'", StringComparison.Ordinal) &&
            message.Contains("expectedVersion='1.0.0'", StringComparison.Ordinal));
    }

    // ACC:T40.9
    [Fact]
    public void ShouldExposeDeterministicReasonOrderAndResult_WhenInputsAreIdentical()
    {
        var firstLogger = new CapturingLogger();
        var secondLogger = new CapturingLogger();
        var firstManager = new ConfigManager(firstLogger);
        var secondManager = new ConfigManager(secondLogger);
        var json = CreateConfigJson(version: "2026.04", expectedVersion: "2026.03");

        var first = firstManager.LoadInitialFromJson(json, "res://Config/balance.json");
        var second = secondManager.LoadInitialFromJson(json, "res://Config/balance.json");

        first.Accepted.Should().BeFalse();
        second.Accepted.Should().BeFalse();
        first.Source.Should().Be("fallback");
        second.Source.Should().Be("fallback");
        first.ReasonCodes.Should().Equal("CFG_VERSION_MISMATCH", "CFG_VERSION_MIGRATION_REQUIRED");
        second.ReasonCodes.Should().Equal(first.ReasonCodes);
        firstLogger.Errors.Should().ContainSingle();
        secondLogger.Errors.Should().ContainSingle();
        firstLogger.Errors.Single().Should().Be(secondLogger.Errors.Single());
    }

    // ACC:T40.11
    [Theory]
    [InlineData("1.0.1", "1.0.0")]
    [InlineData("1.0.0", "1.0.0 ")]
    [InlineData("v1", "V1")]
    public void ShouldRejectAllNonEqualVersions_WhenApplyingExpectedVersionEqualityRule(string version, string expectedVersion)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
    }

    // ACC:T40.16
    [Fact]
    public void ShouldNotBypassVersionRejection_WhenForceMigrationIsEnabledAndVersionsDiffer()
    {
        var logger = new CapturingLogger();
        var manager = new ConfigManager(logger);
        var json = CreateConfigJson(version: "2.1.0", expectedVersion: "2.0.0", forceMigration: true);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
        result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
        logger.Errors.Should().ContainSingle(message =>
            message.Contains("migration required", StringComparison.OrdinalIgnoreCase));
    }

    // ACC:T40.21
    [Theory]
    [InlineData("1.0.0", "1.0.0", true, "initial", false)]
    [InlineData("1.0.1", "1.0.0", false, "fallback", true)]
    [InlineData("2", "2.0", false, "fallback", true)]
    public void ShouldMatchTriggerMatrixAndFailurePolicy_WhenVersionPolicyIsEvaluated(
        string version,
        string expectedVersion,
        bool expectedAccepted,
        string expectedSource,
        bool expectMigrationReason)
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(version, expectedVersion);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().Be(expectedAccepted);
        result.Source.Should().Be(expectedSource);

        if (expectedAccepted)
        {
            result.ReasonCodes.Should().BeEmpty();
        }
        else
        {
            result.Snapshot.Should().Be(BalanceSnapshot.Default);
            if (expectMigrationReason)
            {
                result.ReasonCodes.Should().Contain("CFG_VERSION_MIGRATION_REQUIRED");
            }
        }
    }

    private static string CreateConfigJson(string version, string expectedVersion, bool forceMigration = false)
    {
        var forceMigrationLiteral = forceMigration ? "true" : "false";
        return $@"{{
  ""version"": ""{version}"",
  ""expectedVersion"": ""{expectedVersion}"",
  ""forceMigration"": {forceMigrationLiteral},
  ""time"": {{ ""day_seconds"": 240, ""night_seconds"": 120 }},
  ""waves"": {{ ""normal"": {{ ""day1_budget"": 50, ""daily_growth"": 1.2 }} }},
  ""channels"": {{ ""elite"": ""elite"", ""boss"": ""boss"" }},
  ""spawn"": {{ ""cadence_seconds"": 10 }},
  ""boss"": {{ ""count"": 2 }}
}}";
    }
}
