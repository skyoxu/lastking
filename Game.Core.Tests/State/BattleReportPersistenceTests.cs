using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.State;

public sealed class BattleReportPersistenceTests
{
    // ACC:T39.8
    [Theory]
    [InlineData("config_hash", "sha256:4e9a2c18")]
    [InlineData("config_version", "2026.04.21-build.3")]
    public void ShouldIncludeSelectedConfigMetadataField_WhenPersistingAndLoadingForAuditRetrieval(string selectedKey, string selectedValue)
    {
        var persistence = new BattleReportPersistence();
        var originalReport = new BattleReportPayload(
            new BattleMatchResult("victory", 12, 57, 3480),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [selectedKey] = selectedValue
            });

        var storedPayload = persistence.Persist(originalReport);
        var restoredReport = persistence.Load(storedPayload);

        restoredReport.Metadata.Should().ContainKey(selectedKey);
        restoredReport.Metadata[selectedKey].Should().Be(selectedValue);
    }

    [Fact]
    public void ShouldPreserveMatchResultContent_WhenPersistingAndLoadingBattleReportPayload()
    {
        var persistence = new BattleReportPersistence();
        var originalReport = new BattleReportPayload(
            new BattleMatchResult("defeat", 6, 21, 910),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["config_hash"] = "sha256:af77b901"
            });

        var storedPayload = persistence.Persist(originalReport);
        var restoredReport = persistence.Load(storedPayload);

        restoredReport.MatchResult.Should().BeEquivalentTo(originalReport.MatchResult);
    }

    [Fact]
    public void ShouldRefuseAuditRetrieval_WhenConfigMetadataIsMissing()
    {
        var auditReader = new BattleReportAuditReader();
        var restoredReport = new BattleReportPayload(
            new BattleMatchResult("victory", 8, 30, 1200),
            new Dictionary<string, string>(StringComparer.Ordinal));

        Action act = () => auditReader.GetConfigMetadataForAudit(restoredReport);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*config_hash*config_version*");
    }

}
