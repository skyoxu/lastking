using System;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigManagerFingerprintTests
{
    // ACC:T39.2
    [Fact]
    public void ShouldIncludeNonEmptyConfigHash_WhenHashModeIsUsed()
    {
        var factory = new BattleReportFactory();
        var snapshot = BuildSnapshot("{\"difficulty\":\"hard\"}", null);

        var report = factory.Generate(snapshot, BattleReportMetadataMode.Hash);

        report.Metadata.Should().ContainKey("config_hash");
        report.Metadata["config_hash"].Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T39.4
    [Fact]
    public void ShouldNotChangeConfigHash_WhenPayloadIsByteIdentical()
    {
        var factory = new BattleReportFactory();
        var payload = "{\"difficulty\":\"normal\"}";
        var firstSnapshot = BuildSnapshot(payload, null);
        var secondSnapshot = BuildSnapshot(payload, null);

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Hash);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Hash);

        secondReport.Metadata["config_hash"].Should().Be(firstReport.Metadata["config_hash"]);
    }

    // ACC:T39.5
    [Fact]
    public void ShouldChangeConfigHash_WhenPayloadChangesBeforeNextReportGeneration()
    {
        var factory = new BattleReportFactory();
        var firstSnapshot = BuildSnapshot("{\"difficulty\":\"normal\"}", null);
        var secondSnapshot = BuildSnapshot("{\"difficulty\":\"hard\"}", null);

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Hash);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Hash);

        secondReport.Metadata["config_hash"].Should().NotBe(firstReport.Metadata["config_hash"]);
    }

    // ACC:T39.6
    // ACC:T39.10
    [Fact]
    public void ShouldMatchExactPayloadHash_WhenConfigHashIsComputed()
    {
        var factory = new BattleReportFactory();
        var payload = "{\"seed\":12345,\"map\":\"islands\"}";
        var snapshot = BuildSnapshot(payload, null);
        var expectedHash = ComputeSha256Hex(Encoding.UTF8.GetBytes(payload));

        var report = factory.Generate(snapshot, BattleReportMetadataMode.Hash);

        report.Metadata.Should().ContainKey("config_hash");
        report.Metadata["config_hash"].Should().Be(expectedHash);
    }

    // ACC:T39.9
    [Fact]
    public void ShouldStoreDeclaredConfigVersion_WhenVersionModeIsUsed()
    {
        var factory = new BattleReportFactory();
        var snapshot = BuildSnapshot("{\"difficulty\":\"hard\"}", "2026.04.21");

        var report = factory.Generate(snapshot, BattleReportMetadataMode.Version);

        report.Metadata.Should().ContainKey("config_version");
        report.Metadata["config_version"].Should().Be("2026.04.21");
    }

    // ACC:T39.11
    [Fact]
    public void ShouldNotChangeConfigVersion_WhenDeclaredVersionIsUnchanged()
    {
        var factory = new BattleReportFactory();
        var firstSnapshot = BuildSnapshot("{\"a\":1}", "v2");
        var secondSnapshot = BuildSnapshot("{\"a\":1,\"b\":2}", "v2");

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Version);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Version);

        secondReport.Metadata["config_version"].Should().Be(firstReport.Metadata["config_version"]);
    }

    // ACC:T39.12
    [Fact]
    public void ShouldChangeConfigVersion_WhenDeclaredVersionChangesBeforeNextReportGeneration()
    {
        var factory = new BattleReportFactory();
        var firstSnapshot = BuildSnapshot("{\"a\":1}", "v2");
        var secondSnapshot = BuildSnapshot("{\"a\":1}", "v3");

        var firstReport = factory.Generate(firstSnapshot, BattleReportMetadataMode.Version);
        var secondReport = factory.Generate(secondSnapshot, BattleReportMetadataMode.Version);

        secondReport.Metadata["config_version"].Should().NotBe(firstReport.Metadata["config_version"]);
    }

    [Fact]
    public void ShouldRejectVersionMode_WhenDeclaredVersionIsBlank()
    {
        var factory = new BattleReportFactory();
        var snapshot = BuildSnapshot("{\"a\":1}", "   ");

        Action act = () => factory.Generate(snapshot, BattleReportMetadataMode.Version);

        act.Should().Throw<InvalidOperationException>();
    }

    private static BattleConfigSnapshot BuildSnapshot(string jsonPayload, string? declaredVersion)
    {
        return new BattleConfigSnapshot(
            Encoding.UTF8.GetBytes(jsonPayload),
            declaredVersion,
            new BattleMatchResult("win", 4, 12, 450));
    }

    private static string ComputeSha256Hex(byte[] payload)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
