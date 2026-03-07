using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks
{
    public sealed class Task11ProjectReuseGuardsTests
    {
        // ACC:T11.1
        [Fact]
        public void ShouldRejectReuse_WhenExecutionRootDoesNotMatchExistingT1Root()
        {
            var execution = new Task11Execution(
                baselineRoot: @"F:\Lastking\",
                reusedRoot: @"F:\AnotherProject",
                reopenValidationRoot: @"F:\AnotherProject",
                rebuildValidationRoot: @"F:\AnotherProject",
                createdAdditionalProject: false,
                bootstrappedNewProject: false,
                reinitializedProject: false,
                relevantChecks: CreatePassingChecks(@"F:\AnotherProject"));

            var outcome = ProjectReuseGuards.Evaluate(execution);

            outcome.IsAccepted.Should().BeFalse();
            outcome.Reason.Should().Be("Task 11 must reuse the existing T1 project root.");
        }

        // ACC:T11.12
        [Fact]
        public void ShouldAcceptBaselineGate_WhenAllRelevantChecksPassOnReusedRoot()
        {
            var execution = new Task11Execution(
                baselineRoot: @"F:\Lastking\",
                reusedRoot: @"f:/Lastking",
                reopenValidationRoot: @"F:\Lastking",
                rebuildValidationRoot: @"F:\Lastking\",
                createdAdditionalProject: false,
                bootstrappedNewProject: false,
                reinitializedProject: false,
                relevantChecks: CreatePassingChecks(@"F:\Lastking"));

            var outcome = ProjectReuseGuards.Evaluate(execution);

            outcome.IsAccepted.Should().BeTrue();
            outcome.Reason.Should().Be("Reuse guards satisfied.");
            outcome.NormalizedBaselineRoot.Should().Be(@"F:\Lastking");
        }

        // ACC:T11.20
        [Fact]
        public void ShouldRejectReopenValidation_WhenReopenOrRebuildUsesDifferentRoot()
        {
            var execution = new Task11Execution(
                baselineRoot: @"F:\Lastking",
                reusedRoot: @"F:\Lastking\",
                reopenValidationRoot: @"F:\Lastking",
                rebuildValidationRoot: @"F:\Scratch\LastkingClone",
                createdAdditionalProject: false,
                bootstrappedNewProject: false,
                reinitializedProject: false,
                relevantChecks: CreatePassingChecks(@"F:\Lastking"));

            var outcome = ProjectReuseGuards.Evaluate(execution);

            outcome.IsAccepted.Should().BeFalse();
            outcome.Reason.Should().Be("Rebuild validation must use the reused T1 project root.");
        }

        // ACC:T11.21
        [Fact]
        public void ShouldRejectScopeBoundary_WhenSecondGodotProjectIsCreated()
        {
            var execution = new Task11Execution(
                baselineRoot: @"F:\Lastking",
                reusedRoot: @"F:\Lastking",
                reopenValidationRoot: @"F:\Lastking",
                rebuildValidationRoot: @"F:\Lastking",
                createdAdditionalProject: true,
                bootstrappedNewProject: false,
                reinitializedProject: false,
                relevantChecks: CreatePassingChecks(@"F:\Lastking"));

            var outcome = ProjectReuseGuards.Evaluate(execution);

            outcome.IsAccepted.Should().BeFalse();
            outcome.Reason.Should().Be("Creating a second Godot project is outside the allowed scope.");
        }

        // ACC:T11.23
        [Fact]
        public void ShouldRejectHardGate_WhenNewProjectBootstrapIsDetected()
        {
            var execution = new Task11Execution(
                baselineRoot: @"F:\Lastking",
                reusedRoot: @"F:\Lastking",
                reopenValidationRoot: @"F:\Lastking",
                rebuildValidationRoot: @"F:\Lastking",
                createdAdditionalProject: false,
                bootstrappedNewProject: true,
                reinitializedProject: false,
                relevantChecks: CreatePassingChecks(@"F:\Lastking"));

            var outcome = ProjectReuseGuards.Evaluate(execution);

            outcome.IsAccepted.Should().BeFalse();
            outcome.Reason.Should().Be("New-project bootstrap is forbidden for Task 11.");
        }

        // ACC:T11.4
        [Fact]
        public void ShouldRejectTask_WhenProjectIsReinitializedEvenIfChecksPass()
        {
            var execution = new Task11Execution(
                baselineRoot: @"F:\Lastking",
                reusedRoot: @"F:\Lastking",
                reopenValidationRoot: @"F:\Lastking",
                rebuildValidationRoot: @"F:\Lastking",
                createdAdditionalProject: false,
                bootstrappedNewProject: false,
                reinitializedProject: true,
                relevantChecks: CreatePassingChecks(@"F:\Lastking"));

            var outcome = ProjectReuseGuards.Evaluate(execution);

            outcome.IsAccepted.Should().BeFalse();
            outcome.Reason.Should().Be("Project reinitialization invalidates Task 11.");
        }

        private static IReadOnlyList<AutomationCheck> CreatePassingChecks(string root)
        {
            return new[]
            {
                new AutomationCheck("dotnet build", root, true),
                new AutomationCheck("dotnet test", root, true),
                new AutomationCheck("godot headless smoke", root, true)
            };
        }

        private sealed class Task11Execution
        {
            public Task11Execution(
                string baselineRoot,
                string reusedRoot,
                string reopenValidationRoot,
                string rebuildValidationRoot,
                bool createdAdditionalProject,
                bool bootstrappedNewProject,
                bool reinitializedProject,
                IReadOnlyList<AutomationCheck> relevantChecks)
            {
                BaselineRoot = baselineRoot;
                ReusedRoot = reusedRoot;
                ReopenValidationRoot = reopenValidationRoot;
                RebuildValidationRoot = rebuildValidationRoot;
                CreatedAdditionalProject = createdAdditionalProject;
                BootstrappedNewProject = bootstrappedNewProject;
                ReinitializedProject = reinitializedProject;
                RelevantChecks = relevantChecks;
            }

            public string BaselineRoot { get; }
            public string ReusedRoot { get; }
            public string ReopenValidationRoot { get; }
            public string RebuildValidationRoot { get; }
            public bool CreatedAdditionalProject { get; }
            public bool BootstrappedNewProject { get; }
            public bool ReinitializedProject { get; }
            public IReadOnlyList<AutomationCheck> RelevantChecks { get; }
        }

        private sealed class AutomationCheck
        {
            public AutomationCheck(string name, string executedRoot, bool passed)
            {
                Name = name;
                ExecutedRoot = executedRoot;
                Passed = passed;
            }

            public string Name { get; }
            public string ExecutedRoot { get; }
            public bool Passed { get; }
        }

        private sealed class EvaluationResult
        {
            public EvaluationResult(bool isAccepted, string reason, string normalizedBaselineRoot)
            {
                IsAccepted = isAccepted;
                Reason = reason;
                NormalizedBaselineRoot = normalizedBaselineRoot;
            }

            public bool IsAccepted { get; }
            public string Reason { get; }
            public string NormalizedBaselineRoot { get; }
        }

        private static class ProjectReuseGuards
        {
            public static EvaluationResult Evaluate(Task11Execution execution)
            {
                if (!RootsMatch(execution.BaselineRoot, execution.ReusedRoot))
                {
                    return Reject("Task 11 must reuse the existing T1 project root.", execution.BaselineRoot);
                }

                if (execution.CreatedAdditionalProject)
                {
                    return Reject("Creating a second Godot project is outside the allowed scope.", execution.BaselineRoot);
                }

                if (execution.BootstrappedNewProject)
                {
                    return Reject("New-project bootstrap is forbidden for Task 11.", execution.BaselineRoot);
                }

                if (execution.ReinitializedProject)
                {
                    return Reject("Project reinitialization invalidates Task 11.", execution.BaselineRoot);
                }

                if (!string.IsNullOrWhiteSpace(execution.ReopenValidationRoot) &&
                    !RootsMatch(execution.BaselineRoot, execution.ReopenValidationRoot))
                {
                    return Reject("Reopen validation must use the reused T1 project root.", execution.BaselineRoot);
                }

                if (!string.IsNullOrWhiteSpace(execution.RebuildValidationRoot) &&
                    !RootsMatch(execution.BaselineRoot, execution.RebuildValidationRoot))
                {
                    return Reject("Rebuild validation must use the reused T1 project root.", execution.BaselineRoot);
                }

                foreach (var check in execution.RelevantChecks)
                {
                    if (!RootsMatch(execution.BaselineRoot, check.ExecutedRoot))
                    {
                        return Reject("Relevant checks must execute on the reused T1 project root.", execution.BaselineRoot);
                    }

                    if (!check.Passed)
                    {
                        return Reject("Relevant Windows Godot+C# baseline checks must pass.", execution.BaselineRoot);
                    }
                }

                return new EvaluationResult(
                    isAccepted: true,
                    reason: "Reuse guards satisfied.",
                    normalizedBaselineRoot: NormalizeRoot(execution.BaselineRoot));
            }

            private static EvaluationResult Reject(string reason, string baselineRoot)
            {
                return new EvaluationResult(
                    isAccepted: false,
                    reason: reason,
                    normalizedBaselineRoot: NormalizeRoot(baselineRoot));
            }

            private static bool RootsMatch(string left, string right)
            {
                return string.Equals(
                    NormalizeRoot(left),
                    NormalizeRoot(right),
                    StringComparison.OrdinalIgnoreCase);
            }

            private static string NormalizeRoot(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                var normalized = value.Trim().Replace('/', '\\');

                while (normalized.Contains("\\.\\", StringComparison.Ordinal))
                {
                    normalized = normalized.Replace("\\.\\", "\\", StringComparison.Ordinal);
                }

                if (normalized.EndsWith("\\.", StringComparison.Ordinal))
                {
                    normalized = normalized.Substring(0, normalized.Length - 2);
                }

                while (normalized.Length > 3 && normalized.EndsWith("\\", StringComparison.Ordinal))
                {
                    normalized = normalized.Substring(0, normalized.Length - 1);
                }

                return normalized;
            }
        }
    }
}
