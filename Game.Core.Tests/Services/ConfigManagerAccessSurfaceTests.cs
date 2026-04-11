using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigManagerAccessSurfaceTests
{
    // ACC:T2.7
    [Fact]
    public void ShouldExposeSingleConfigManagerSurface_WhenScanningProductionSource()
    {
        var repoRoot = FindRepositoryRoot();

        repoRoot.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(repoRoot!).Should().BeTrue();

        var productionCsFiles = Directory
            .GetFiles(repoRoot!, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile)
            .ToArray();

        productionCsFiles.Should().NotBeNull();

        var configManagerDefinitions = productionCsFiles
            .Where(file => File.ReadAllText(file, Encoding.UTF8).Contains("class ConfigManager", StringComparison.Ordinal))
            .ToArray();

        configManagerDefinitions.Length.Should().BeLessOrEqualTo(1, "balancing should be accessed through a single global ConfigManager surface");
    }

    // ACC:T2.13
    [Fact]
    public void ShouldReadLoopWaveSpawnBalanceViaConfigManager_WhenInspectingGameplayPaths()
    {
        var repoRoot = FindRepositoryRoot();

        repoRoot.Should().NotBeNullOrWhiteSpace();
        File.Exists(Path.Combine(repoRoot!, "project.godot")).Should().BeTrue();

        var candidateFiles = Directory
            .GetFiles(repoRoot!, "*.*", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile)
            .Where(IsGameplayLoopWaveSpawnPath)
            .Where(path => !IsConfigDefinitionFile(path))
            .ToArray();

        candidateFiles.Should().NotBeNull();
        candidateFiles.Should().NotBeEmpty("loop/wave/spawn coverage must fail when no production candidates are found");

        var filesWithoutConfigAccess = candidateFiles
            .Where(file => !HasConfigAccessToken(File.ReadAllText(file, Encoding.UTF8)))
            .ToArray();

        filesWithoutConfigAccess.Should().BeEmpty("loop, wave, and spawn gameplay paths should consume balancing data through ConfigManager access tokens");
    }

    [Fact]
    public void ShouldDetectConfigAccessToken_WhenSourceContainsKnownConfigManagerCalls()
    {
        HasConfigAccessToken("var x = ConfigManager.GetValue<int>(\"spawn.rate\");").Should().BeTrue();
        HasConfigAccessToken("var x = IConfigReader.GetConfig();").Should().BeTrue();
        HasConfigAccessToken("const int SpawnRate = 5;").Should().BeFalse();
    }

    private static bool HasConfigAccessToken(string source)
    {
        return source.Contains("ConfigManager", StringComparison.Ordinal)
               || source.Contains("IConfig", StringComparison.Ordinal)
               || source.Contains("GetConfig", StringComparison.Ordinal)
               || source.Contains("GetValue", StringComparison.Ordinal)
               || source.Contains("TryGet", StringComparison.Ordinal)
               || source.Contains("ConfigFile", StringComparison.Ordinal);
    }

    private static bool IsGameplayLoopWaveSpawnPath(string path)
    {
        var normalized = path.Replace('\\', '/');

        return !normalized.Contains("/Game.Core/Contracts/", StringComparison.OrdinalIgnoreCase)
               && (normalized.Contains("loop", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("wave", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("spawn", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("config", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsProductionSourceFile(string path)
    {
        var normalized = path.Replace('\\', '/');

        var isSourceFile = normalized.EndsWith(".cs", StringComparison.Ordinal)
                           || normalized.EndsWith(".gd", StringComparison.Ordinal);

        return isSourceFile
               && !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/logs/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/backup/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Game.Core.Tests/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Tests.Godot/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConfigDefinitionFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.EndsWith("Config.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("ConfigLoadResult.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("BalanceSnapshot.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var projectFile = Path.Combine(current.FullName, "project.godot");
            if (File.Exists(projectFile))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
