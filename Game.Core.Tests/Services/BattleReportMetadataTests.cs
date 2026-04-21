using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BattleReportMetadataTests
{
    // ACC:T39.1
    [Theory]
    [InlineData(BattleReportMetadataMode.Hash, "config_hash", "config_version")]
    [InlineData(BattleReportMetadataMode.Version, "config_version", "config_hash")]
    public void ShouldRecordSelectedConfigMetadata_WhenGeneratingBattleReport(
        BattleReportMetadataMode mode,
        string expectedKey,
        string unexpectedKey)
    {
        var factory = new BattleReportFactory();
        var snapshot = BuildSnapshot(mode, "2026.04.21", "{\"difficulty\":\"hard\",\"seed\":42}");

        var report = factory.Generate(snapshot, mode);

        report.Metadata.Should().ContainKey(expectedKey);
        report.Metadata[expectedKey].Should().NotBeNullOrWhiteSpace();
        report.Metadata.Should().NotContainKey(unexpectedKey);
    }

    // ACC:T39.2
    [Fact]
    public void ShouldIncludeNonEmptyConfigHash_WhenHashModeUsesPayloadFingerprint()
    {
        var payload = Encoding.UTF8.GetBytes("{\"difficulty\":\"normal\"}");
        var expectedHash = ComputeSha256Hex(payload);
        var factory = new BattleReportFactory();
        var snapshot = new BattleConfigSnapshot(payload, null, new BattleMatchResult("loss", 6, 20, 640));

        var report = factory.Generate(snapshot, BattleReportMetadataMode.Hash);

        report.Metadata.Should().ContainKey("config_hash");
        report.Metadata["config_hash"].Should().Be(expectedHash);
    }

    // ACC:T39.4
    [Fact]
    public void ShouldKeepConfigHashStable_WhenPayloadIsByteIdentical()
    {
        var payload = Encoding.UTF8.GetBytes("{\"difficulty\":\"normal\",\"mode\":\"story\"}");
        var factory = new BattleReportFactory();
        var firstSnapshot = new BattleConfigSnapshot((byte[])payload.Clone(), null, new BattleMatchResult("win", 7, 22, 900));
        var secondSnapshot = new BattleConfigSnapshot((byte[])payload.Clone(), null, new BattleMatchResult("win", 7, 22, 900));

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Hash);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Hash);

        secondReport.Metadata["config_hash"].Should().Be(firstReport.Metadata["config_hash"]);
    }

    [Fact]
    public void ShouldChangeConfigHash_WhenPayloadChangesBetweenReports()
    {
        var factory = new BattleReportFactory();
        var firstSnapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"difficulty\":\"normal\"}"),
            null,
            new BattleMatchResult("win", 9, 40, 1300));
        var secondSnapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"difficulty\":\"hard\"}"),
            null,
            new BattleMatchResult("win", 9, 40, 1300));

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Hash);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Hash);

        secondReport.Metadata["config_hash"].Should().NotBe(firstReport.Metadata["config_hash"]);
    }

    // ACC:T39.7
    [Fact]
    public void ShouldRejectAuditCompletion_WhenMetadataContainsNeitherHashNorVersion()
    {
        var validator = new BattleReportAuditValidator();
        var report = new BattleReportPayload(
            new BattleMatchResult("loss", 3, 8, 220),
            new Dictionary<string, string>(StringComparer.Ordinal));

        Action act = () => validator.EnsureAuditComplete(report);

        act.Should().Throw<InvalidOperationException>();
    }

    // ACC:T39.8
    [Fact]
    public void ShouldPreserveMatchResult_WhenAddingSelectedMetadataToReportPayload()
    {
        var factory = new BattleReportFactory();
        var matchResult = new BattleMatchResult("win", 11, 48, 1500);
        var snapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"difficulty\":\"normal\",\"map\":\"islands\"}"),
            "v1",
            matchResult);

        var report = factory.Generate(snapshot, BattleReportMetadataMode.Hash);

        report.MatchResult.Should().BeEquivalentTo(matchResult);
        report.Metadata.Should().ContainKey("config_hash");
        report.Metadata["config_hash"].Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T39.9
    [Fact]
    public void ShouldStoreDeclaredConfigVersion_WhenVersionModeUsesProvidedVersion()
    {
        var factory = new BattleReportFactory();
        var snapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"spawn_rate\":1.25}"),
            "2026.04.21-build.3",
            new BattleMatchResult("win", 10, 30, 1100));

        var report = factory.Generate(snapshot, BattleReportMetadataMode.Version);

        report.Metadata.Should().ContainKey("config_version");
        report.Metadata["config_version"].Should().Be("2026.04.21-build.3");
    }

    [Fact]
    public void ShouldKeepConfigVersionStable_WhenDeclaredVersionIsUnchanged()
    {
        var factory = new BattleReportFactory();
        var firstSnapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"a\":1}"),
            "v2",
            new BattleMatchResult("win", 4, 12, 450));
        var secondSnapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"a\":1,\"b\":2}"),
            "v2",
            new BattleMatchResult("win", 4, 12, 450));

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Version);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Version);

        secondReport.Metadata["config_version"].Should().Be(firstReport.Metadata["config_version"]);
    }

    [Fact]
    public void ShouldChangeConfigVersion_WhenDeclaredVersionChangesBetweenReports()
    {
        var factory = new BattleReportFactory();
        var firstSnapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"a\":1}"),
            "v2",
            new BattleMatchResult("win", 4, 12, 450));
        var secondSnapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"a\":1}"),
            "v3",
            new BattleMatchResult("win", 4, 12, 450));

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Version);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Version);

        secondReport.Metadata["config_version"].Should().NotBe(firstReport.Metadata["config_version"]);
    }

    [Fact]
    public void ShouldRejectVersionMode_WhenDeclaredVersionIsMissing()
    {
        var factory = new BattleReportFactory();
        var snapshot = new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes("{\"spawn_rate\":1.25}"),
            null,
            new BattleMatchResult("win", 10, 30, 1100));

        Action act = () => factory.Generate(snapshot, BattleReportMetadataMode.Version);

        act.Should().Throw<InvalidOperationException>();
    }

    private static BattleConfigSnapshot BuildSnapshot(BattleReportMetadataMode mode, string version, string jsonPayload)
    {
        return new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes(jsonPayload),
            mode == BattleReportMetadataMode.Version ? version : null,
            new BattleMatchResult("win", 12, 54, 1800));
    }

    private static string ComputeSha256Hex(byte[] payload)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

}
