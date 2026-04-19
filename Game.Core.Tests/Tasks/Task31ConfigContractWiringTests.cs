using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task31ConfigContractWiringTests
{
    // ACC:T31.10
    [Fact]
    public void ShouldRejectWorkspace_WhenEntrypointsAppearCompleteButContainPathDrift()
    {
        var workspace = new ConfigWorkspace(
            new[]
            {
                "config/schemas/gameplay.schema.json",
                "config/samples/gameplay.sample.json"
            },
            new[]
            {
                new ConfigEntrypoint("LoadGameplaySchema", "config/schemas/gameplay.schema.json", true),
                new ConfigEntrypoint("LoadGameplaySample", "config/samplez/gameplay.sample.json", true)
            });

        var result = EvaluateConfigWorkspaceWiring(workspace);

        result.IsAccepted.Should().BeFalse(
            "acceptance must fail when path-binding drift exists between declared config paths and wiring entrypoints");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("unreachable")]
    [InlineData("unbound")]
    public void ShouldRejectWorkspace_WhenAnyEntrypointIsMissingUnreachableOrUnbound(string failureMode)
    {
        var workspace = BuildWorkspaceForFailureMode(failureMode);

        var result = EvaluateConfigWorkspaceWiring(workspace);

        result.IsAccepted.Should().BeFalse(
            "acceptance must fail when any config wiring entrypoint is missing, unreachable, or not bound");
    }

    // ACC:T31.12
    [Fact]
    public void ShouldRejectHardGate_WhenOnlyDirectoriesExistWithoutFunctionalWiringEntrypoints()
    {
        var workspace = new ConfigWorkspace(
            new[]
            {
                "config/schemas/",
                "config/samples/"
            },
            Array.Empty<ConfigEntrypoint>());

        var result = EvaluateConfigWorkspaceWiring(workspace);

        result.IsAccepted.Should().BeFalse(
            "directory-only scaffolding is insufficient without functional wiring entrypoints");
        result.Reasons.Should().Contain(reason =>
            reason.Contains("functional wiring entrypoint", StringComparison.OrdinalIgnoreCase));
    }

    private static ConfigWorkspace BuildWorkspaceForFailureMode(string failureMode)
    {
        var declaredConfigPaths = new[]
        {
            "config/schemas/gameplay.schema.json",
            "config/samples/gameplay.sample.json"
        };

        var entrypoints = failureMode switch
        {
            "missing" => new[]
            {
                new ConfigEntrypoint("LoadGameplaySchema", "config/schemas/gameplay.schema.json", true)
            },
            "unreachable" => new[]
            {
                new ConfigEntrypoint("LoadGameplaySchema", "config/schemas/gameplay.schema.json", true),
                new ConfigEntrypoint("LoadGameplaySample", "config/samples/gameplay.sample.json", false)
            },
            "unbound" => new[]
            {
                new ConfigEntrypoint("LoadGameplaySchema", "config/schemas/gameplay.schema.json", true),
                new ConfigEntrypoint("LoadGameplaySample", "config/samples/gameplay.sample.v2.json", true)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(failureMode), failureMode, "Unsupported failure mode.")
        };

        return new ConfigWorkspace(declaredConfigPaths, entrypoints);
    }

    private static WiringEvaluation EvaluateConfigWorkspaceWiring(ConfigWorkspace workspace)
    {
        var reasons = new List<string>();
        var declaredPaths = workspace.DeclaredConfigPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasSchemas = declaredPaths.Any(path =>
            path.StartsWith("config/schemas/", StringComparison.OrdinalIgnoreCase));
        var hasSamples = declaredPaths.Any(path =>
            path.StartsWith("config/samples/", StringComparison.OrdinalIgnoreCase));

        if (!hasSchemas || !hasSamples)
        {
            reasons.Add("Hard gate requires both config/schemas and config/samples.");
        }

        if (workspace.Entrypoints.Count == 0)
        {
            reasons.Add("At least one functional wiring entrypoint is required.");
        }

        foreach (var declaredPath in declaredPaths)
        {
            var boundEntrypoints = workspace.Entrypoints
                .Where(entrypoint => string.Equals(
                    NormalizePath(entrypoint.BoundConfigPath),
                    declaredPath,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (boundEntrypoints.Length == 0)
            {
                reasons.Add($"No wiring entrypoint is bound to declared config path '{declaredPath}'.");
                continue;
            }

            if (boundEntrypoints.Any(entrypoint => !entrypoint.IsReachable))
            {
                reasons.Add($"A wiring entrypoint bound to '{declaredPath}' is unreachable.");
            }
        }

        if (workspace.Entrypoints.Any(entrypoint =>
            !declaredPaths.Contains(NormalizePath(entrypoint.BoundConfigPath))))
        {
            reasons.Add("Entrypoint binding must target declared config paths.");
        }

        return new WiringEvaluation(reasons.Count == 0, reasons);
    }

    private static string NormalizePath(string path)
    {
        return (path ?? string.Empty).Replace('\\', '/').Trim();
    }

    private sealed record ConfigWorkspace(
        IReadOnlyList<string> DeclaredConfigPaths,
        IReadOnlyList<ConfigEntrypoint> Entrypoints);

    private sealed record ConfigEntrypoint(string Name, string BoundConfigPath, bool IsReachable);

    private sealed record WiringEvaluation(bool IsAccepted, IReadOnlyList<string> Reasons);
}
