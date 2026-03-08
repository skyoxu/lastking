using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public class ConfigDrivenRuntimeLoopTests
{
    // ACC:T2.13
    [Fact]
    public void ShouldReferenceConfigAccess_WhenInspectingRuntimeLoopSourceFiles()
    {
        var repoRoot = FindRepositoryRoot();

        repoRoot.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(repoRoot!).Should().BeTrue();

        var runtimeLoopFiles = Directory
            .GetFiles(repoRoot!, "*.*", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile)
            .Where(IsRuntimeBalancePath)
            .Where(path => !IsConfigDefinitionFile(path))
            .ToArray();

        runtimeLoopFiles.Should().NotBeNull();
        runtimeLoopFiles.Should().NotBeEmpty("runtime loop coverage must not pass on empty source set");

        var filesWithoutConfigAccess = runtimeLoopFiles
            .Where(file => !HasConfigAccessToken(File.ReadAllText(file, Encoding.UTF8)))
            .ToArray();

        filesWithoutConfigAccess.Should().BeEmpty("runtime loop gameplay paths should read balancing values through config access tokens");
    }

    [Fact]
    public void ShouldResolveRepositoryRoot_WhenRunningUnderTestHost()
    {
        var repoRoot = FindRepositoryRoot();

        repoRoot.Should().NotBeNullOrWhiteSpace();
        File.Exists(Path.Combine(repoRoot!, "project.godot")).Should().BeTrue();
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

    private static bool IsProductionSourceFile(string path)
    {
        var normalized = path.Replace('\\', '/');

        var isSourceFile = normalized.EndsWith(".cs", StringComparison.Ordinal)
                           || normalized.EndsWith(".gd", StringComparison.Ordinal);

        return isSourceFile
               && !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Game.Core/Contracts/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Game.Core.Tests/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Tests.Godot/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRuntimeBalancePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("loop", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("wave", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("spawn", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("config", StringComparison.OrdinalIgnoreCase);
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
