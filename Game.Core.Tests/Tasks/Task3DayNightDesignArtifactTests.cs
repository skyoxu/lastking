using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task3DayNightDesignArtifactTests
{
    private static readonly string[] RequiredSections =
    {
        "Day/Night State Diagram",
        "Transition Conditions",
        "Downstream Event Consumers"
    };

    // ACC:T3.17
    [Fact]
    public void ShouldContainCoreDesignSections_WhenReadingDayNightDesignArtifact()
    {
        var contract = DayNightDesignArtifactContract.CreateDefault();
        var absolutePath = ResolveRepoPath(contract.FilePath);
        var content = File.ReadAllText(absolutePath);

        contract.RequiredSections.Should().BeEquivalentTo(RequiredSections, options => options.WithStrictOrdering());
        content.Should().NotBeNullOrWhiteSpace();
        foreach (var requiredSection in contract.RequiredSections)
        {
            content.Should().Contain(requiredSection);
        }

        content.Should().Contain("review_status: approved");
        content.Should().Contain("reviewed_by:");
        content.Should().Contain("reviewed_at:");
    }

    [Fact]
    public void ShouldMatchRuntimeTransitions_WhenDesignArtifactDeclaresThresholdsAndTerminalRule()
    {
        var contract = DayNightDesignArtifactContract.CreateDefault();
        var absolutePath = ResolveRepoPath(contract.FilePath);
        var content = File.ReadAllText(absolutePath);

        content.Should().Contain("240");
        content.Should().Contain("120");
        content.Should().Contain("Day15");

        var manager = new GameStateManager(
            store: new NoopStore(),
            dayNightSeed: 1,
            dayNightConfig: new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 15));

        manager.UpdateDayNightRuntime(239);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.UpdateDayNightRuntime(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        manager.UpdateDayNightRuntime(120);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(2);

        for (var i = 0; i < 2000; i++)
        {
            manager.UpdateDayNightRuntime(60);
            if (manager.CurrentDayNightPhase == DayNightPhase.Terminal)
            {
                break;
            }
        }

        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Terminal);
        manager.CurrentDayNightDay.Should().Be(15);
    }

    [Fact]
    public void ShouldPointToConcreteOverlayArtifact_WhenResolvingContractPath()
    {
        var contract = DayNightDesignArtifactContract.CreateDefault();
        var absolutePath = ResolveRepoPath(contract.FilePath);

        contract.FilePath.Should().EndWith(".md");
        contract.FilePath.Should().Contain("PRD-lastking-T2");
        File.Exists(absolutePath).Should().BeTrue();
    }

    private sealed record DayNightDesignArtifactContract(string FilePath, IReadOnlyList<string> RequiredSections)
    {
        public static DayNightDesignArtifactContract CreateDefault()
        {
            return new DayNightDesignArtifactContract(
                "docs/architecture/overlays/PRD-lastking-T2/08/08-day-night-runtime-state-machine.md",
                new[]
                {
                    "Day/Night State Diagram",
                    "Transition Conditions",
                    "Downstream Event Consumers"
                });
        }
    }

    private static string ResolveRepoPath(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".taskmaster")))
            {
                return Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test base directory.");
    }

    private sealed class NoopStore : IDataStore
    {
        public Task SaveAsync(string key, string json) => Task.CompletedTask;
        public Task<string?> LoadAsync(string key) => Task.FromResult<string?>(null);
        public Task DeleteAsync(string key) => Task.CompletedTask;
    }
}
