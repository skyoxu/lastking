using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RewardManagerConstructionTests
{
    // ACC:T18.17
    [Fact]
    [Trait("acceptance", "ACC:T18.17")]
    public void ShouldExposeSingleProductionRewardManagerClass_WhenScanningCoreServicesSource()
    {
        var repoRoot = FindRepositoryRoot();

        repoRoot.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(repoRoot!).Should().BeTrue();

        var rewardManagerFiles = FindRewardManagerSourceFiles(repoRoot!);

        rewardManagerFiles.Should().ContainSingle(
            "Task 18 hard acceptance requires exactly one production RewardManager class in non-test source files.");

        var rewardManagerSource = File.ReadAllText(rewardManagerFiles.Single(), Encoding.UTF8);
        rewardManagerSource.Should().Contain("namespace Game.Core.Services", "RewardManager must live in the core services namespace.");
        rewardManagerSource.Should().Contain("class RewardManager", "hard acceptance requires a concrete RewardManager class.");
    }

    [Fact]
    public void ShouldContainFailFastConstructionGuards_WhenRewardManagerConstructionIsDefined()
    {
        var repoRoot = FindRepositoryRoot();

        repoRoot.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(repoRoot!).Should().BeTrue();

        var rewardManagerFiles = FindRewardManagerSourceFiles(repoRoot!);
        rewardManagerFiles.Should().ContainSingle(
            "construction guard validation depends on a unique production RewardManager source file.");

        var rewardManagerSource = File.ReadAllText(rewardManagerFiles.Single(), Encoding.UTF8);

        rewardManagerSource.Should().MatchRegex(
            "(?s)(ThrowIfNull|==\\s*null)",
            "RewardManager construction should refuse null reference inputs.");
        rewardManagerSource.Should().MatchRegex(
            "(?s)(ThrowIfNegative|<\\s*0)",
            "RewardManager construction should refuse negative fallback-gold inputs.");
    }

    private static string[] FindRewardManagerSourceFiles(string repoRoot)
    {
        return Directory
            .GetFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile)
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                if (fileName.Equals("RewardManager.cs", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var source = File.ReadAllText(path, Encoding.UTF8);
                return source.Contains("class RewardManager", StringComparison.Ordinal);
            })
            .ToArray();
    }

    private static bool IsProductionSourceFile(string path)
    {
        var normalized = path.Replace('\\', '/');

        return normalized.EndsWith(".cs", StringComparison.Ordinal)
               && !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/logs/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/backup/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Game.Core.Tests/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Tests.Godot/", StringComparison.OrdinalIgnoreCase);
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
