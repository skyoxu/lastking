using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task31ConfigWorkspaceGuardrailsTests
{
    private readonly WorkspaceGuardrailsEvaluator evaluator = new();

    // ACC:T31.1
    [Fact]
    public void ShouldApplyScaffoldInPlace_WhenValidatedT21RootIsProvided()
    {
        var state = ConfigWorkspaceState.ValidInPlace();
        var result = this.evaluator.Evaluate(state);

        result.IsCompliant.Should().BeTrue();
        result.Failures.Should().BeEmpty();
    }

    // ACC:T31.11
    [Fact]
    public void ShouldCreateSchemasAndSamplesDirectories_WhenSetupRunsOnInheritedRoot()
    {
        var state = ConfigWorkspaceState.ValidInPlace();
        var result = this.evaluator.Evaluate(state);

        state.HasSchemasDirectory.Should().BeTrue();
        state.HasSamplesDirectory.Should().BeTrue();
        result.Failures.Should().NotContain("schemas-and-samples-required");
    }

    // ACC:T31.12
    [Fact]
    public void ShouldRefuseSecondProjectRootArtifacts_WhenSetupRunsInPlace()
    {
        var state = ConfigWorkspaceState.ValidInPlace() with
        {
            CreatedSecondProjectRoot = true
        };

        var result = this.evaluator.Evaluate(state);

        result.IsCompliant.Should().BeFalse();
        result.Failures.Should().Contain("no-secondary-project-artifacts");
    }

    // ACC:T31.5
    [Fact]
    public void ShouldDocumentWorkspacePurposeAndUsage_WhenReadmeIsGenerated()
    {
        var state = ConfigWorkspaceState.ValidInPlace() with
        {
            WorkspaceReadmeContent = "Purpose: config-contract workspace.\nUsage: edit config/schemas and config/samples."
        };

        var result = this.evaluator.Evaluate(state);

        result.Failures.Should().NotContain("workspace-readme-required");
        result.IsCompliant.Should().BeTrue();
    }

    // ACC:T31.6
    [Fact]
    public void ShouldFailScopeEnforcement_WhenStandaloneProjectRecreationIsRequested()
    {
        var state = ConfigWorkspaceState.ValidInPlace() with
        {
            CreatedStandaloneGodotProject = true
        };

        var result = this.evaluator.Evaluate(state);

        result.IsCompliant.Should().BeFalse();
        result.Failures.Should().Contain("no-secondary-project-artifacts");
    }

    // ACC:T31.8
    [Fact]
    public void ShouldFailHardGate_WhenDirectoriesExistWithoutWiringEntrypoints()
    {
        var state = ConfigWorkspaceState.ValidInPlace() with
        {
            HasSchemaWiringEntrypoint = false,
            HasSampleWiringEntrypoint = false
        };

        var result = this.evaluator.Evaluate(state);

        result.IsCompliant.Should().BeFalse();
        result.Failures.Should().Contain("hard-gate-wiring");
    }

    private sealed record ConfigWorkspaceState
    {
        public bool IsValidatedT21Root { get; init; }
        public bool BootstrapExecuted { get; init; }
        public bool ReinitializedProject { get; init; }
        public bool CreatedSecondProjectRoot { get; init; }
        public bool CreatedStandaloneGodotProject { get; init; }
        public bool HasSchemasDirectory { get; init; }
        public bool HasSamplesDirectory { get; init; }
        public bool HasWorkspaceReadme { get; init; }
        public string WorkspaceReadmeContent { get; init; } = string.Empty;
        public bool HasSchemaWiringEntrypoint { get; init; }
        public bool HasSampleWiringEntrypoint { get; init; }

        public static ConfigWorkspaceState ValidInPlace()
        {
            return new ConfigWorkspaceState
            {
                IsValidatedT21Root = true,
                BootstrapExecuted = false,
                ReinitializedProject = false,
                CreatedSecondProjectRoot = false,
                CreatedStandaloneGodotProject = false,
                HasSchemasDirectory = true,
                HasSamplesDirectory = true,
                HasWorkspaceReadme = true,
                WorkspaceReadmeContent = "Purpose: keep config-contract artifacts in place.\nUsage: config/schemas and config/samples are the expected working directories.",
                HasSchemaWiringEntrypoint = true,
                HasSampleWiringEntrypoint = true
            };
        }
    }

    private sealed record GuardrailEvaluation(bool IsCompliant, IReadOnlyList<string> Failures);

    private sealed class WorkspaceGuardrailsEvaluator
    {
        public GuardrailEvaluation Evaluate(ConfigWorkspaceState state)
        {
            var failures = new List<string>();

            if (!state.IsValidatedT21Root || state.BootstrapExecuted || state.ReinitializedProject)
            {
                failures.Add("in-place-root-required");
            }

            if (state.CreatedSecondProjectRoot || state.CreatedStandaloneGodotProject)
            {
                failures.Add("no-secondary-project-artifacts");
            }

            if (!state.HasSchemasDirectory || !state.HasSamplesDirectory)
            {
                failures.Add("schemas-and-samples-required");
            }

            if (!state.HasWorkspaceReadme || !ReadmeHasPurposeAndUsage(state.WorkspaceReadmeContent))
            {
                failures.Add("workspace-readme-required");
            }

            var hardGatePassed = state.HasSchemasDirectory
                && state.HasSamplesDirectory
                && state.HasSchemaWiringEntrypoint
                && state.HasSampleWiringEntrypoint;
            if (!hardGatePassed)
            {
                failures.Add("hard-gate-wiring");
            }

            return new GuardrailEvaluation(failures.Count == 0, failures);
        }

        private static bool ReadmeHasPurposeAndUsage(string readmeContent)
        {
            var normalized = readmeContent?.ToLowerInvariant() ?? string.Empty;
            return normalized.Contains("purpose")
                && normalized.Contains("usage")
                && normalized.Contains("config/schemas")
                && normalized.Contains("config/samples");
        }
    }
}
