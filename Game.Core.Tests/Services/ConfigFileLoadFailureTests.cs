using System.IO;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigFileLoadFailureTests
{
    [Fact]
    public void ShouldReturnFileNotFoundReason_WhenInitialConfigFileDoesNotExist()
    {
        var manager = new ConfigManager();
        var missingPath = Path.Combine(Path.GetTempPath(), "lastking-not-exists-balance.json");

        var result = manager.LoadInitialFromFile(missingPath);

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.ReasonCodes.Should().Contain(ConfigManager.FileNotFoundReason);
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    [Fact]
    public void ShouldReturnFileUnreadableReason_WhenInitialConfigPathIsDirectory()
    {
        var manager = new ConfigManager();
        var directoryPath = Path.Combine(Path.GetTempPath(), "lastking-balance-dir");
        Directory.CreateDirectory(directoryPath);

        var result = manager.LoadInitialFromFile(directoryPath);

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.ReasonCodes.Should().Contain(ConfigManager.FileUnreadableReason);
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }
}
