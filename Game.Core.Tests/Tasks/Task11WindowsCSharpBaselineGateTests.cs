using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task11WindowsCSharpBaselineGateTests
{
    // ACC:T11.15
    [Fact]
    public void ShouldPreserveWindowsGodotCSharpBaseline_WhenInspectingRepositoryScaffold()
    {
        var root = FindRepoRoot();
        var projectFile = Path.Combine(root, "project.godot");

        File.Exists(projectFile).Should().BeTrue();
        var projectText = File.ReadAllText(projectFile);

        projectText.Should().Contain("config/features");
        projectText.Should().Contain("4.5");
        projectText.Should().Contain("CompositionRoot=\"*res://Game.Godot/Autoloads/CompositionRoot.cs\"");
        GetTrackedFiles(root, "*.csproj").Should().NotBeEmpty();
        GetTrackedFiles(root, "*.cs").Should().Contain(path =>
            path.EndsWith(Path.Combine("Game.Godot", "Scripts", "Bootstrap", "InputMapper.cs"), StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(Path.Combine("Game.Godot", "Autoloads", "CompositionRoot.cs"), StringComparison.OrdinalIgnoreCase));
    }

    // ACC:T11.7
    [Fact]
    public void ShouldExposeReusableAutomationEntryPoints_WhenCheckingTaskRelevantGuards()
    {
        var root = FindRepoRoot();
        var requiredEntries = BuildAllowedAutomationEntryPoints(root);
        var evaluation = EvaluateAutomationEntryPoints(requiredEntries, BuildAllowedAutomationEntryPoints(root));

        evaluation.IsAccepted.Should().BeTrue();
        evaluation.Reasons.Should().BeEmpty();
        Directory.Exists(Path.Combine(root, "Game.Core.Tests")).Should().BeTrue();
        Directory.Exists(Path.Combine(root, "Tests.Godot", "tests")).Should().BeTrue();
    }

    // ACC:T11.3
    [Fact]
    public void ShouldKeepManagedBuildInputsPresent_WhenValidatingCSharpCompilerBaseline()
    {
        var root = FindRepoRoot();
        var csprojFiles = GetTrackedFiles(root, "*.csproj");
        var csFiles = GetTrackedFiles(root, "*.cs");

        csprojFiles.Should().NotBeEmpty();
        csFiles.Should().NotBeEmpty();
        var firstProjectText = File.ReadAllText(csprojFiles.First());

        firstProjectText.Should().Contain("<TargetFramework");
        typeof(Task11WindowsCSharpBaselineGateTests).Assembly.GetName().Name.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T11.4
    [Fact]
    public void ShouldRequireMainSceneAndValidationSurfaces_WhenEvaluatingBootstrapAcceptanceArtifacts()
    {
        var root = FindRepoRoot();
        var projectText = File.ReadAllText(Path.Combine(root, "project.godot"));
        var mainScenePath = GetQuotedProjectSetting(projectText, "run/main_scene");
        var mainSceneAbsolutePath = ResolveResPath(root, mainScenePath);

        mainScenePath.StartsWith("res://", StringComparison.Ordinal).Should().BeTrue();
        File.Exists(mainSceneAbsolutePath).Should().BeTrue();
        Directory.Exists(Path.Combine(root, "Tests.Godot", "tests", "Integration")).Should().BeTrue();
        File.Exists(Path.Combine(root, "scripts", "sc", "test.py")).Should().BeTrue();
        File.Exists(Path.Combine(root, "scripts", "python", "smoke_headless.py")).Should().BeTrue();
        File.Exists(Path.Combine(root, "scripts", "python", "run_gdunit.py")).Should().BeTrue();
    }

    // ACC:T11.4
    [Fact]
    public void ShouldRejectLegacyOrMissingAutomationEntryPoints_WhenEvaluatingBootstrapAcceptanceArtifacts()
    {
        var root = FindRepoRoot();
        var evaluation = EvaluateAutomationEntryPoints(
            new[]
            {
                Path.Combine(root, "scripts", "sc", "test.py"),
                Path.Combine(root, "scripts", "python", "legacy_bootstrap.py"),
                Path.Combine(root, "scripts", "python", "missing_runner.py")
            },
            BuildAllowedAutomationEntryPoints(root));

        evaluation.IsAccepted.Should().BeFalse();
        evaluation.Reasons.Should().Contain(reason => reason.Contains("Legacy or unsupported automation entrypoint", StringComparison.Ordinal));
        evaluation.Reasons.Should().Contain(reason => reason.Contains("Missing automation entrypoint", StringComparison.Ordinal));
    }

    // ACC:T11.16
    [Fact]
    public void ShouldDetectWindowsGodotAndDotNetSignals_WhenEvaluatingHostReadiness()
    {
        var root = FindRepoRoot();

        OperatingSystem.IsWindows().Should().BeTrue();
        HasDotNetHostSignal().Should().BeTrue();
        HasDotNetSdkSignal().Should().BeTrue();
        HasGodot451Signal(root).Should().BeTrue();
    }

    private static bool ExistsAsFileOrDirectory(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "project.godot")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static string[] BuildAllowedAutomationEntryPoints(string root)
    {
        return new[]
        {
            Path.Combine(root, "docs", "testing-framework.md"),
            Path.Combine(root, "scripts", "sc", "test.py"),
            Path.Combine(root, "scripts", "python", "run_dotnet.py"),
            Path.Combine(root, "scripts", "python", "smoke_headless.py"),
            Path.Combine(root, "scripts", "python", "run_gdunit.py"),
            Path.Combine(root, "scripts", "python", "validate_acceptance_anchors.py"),
            Path.Combine(root, "scripts", "python", "validate_acceptance_refs.py"),
            Path.Combine(root, "scripts", "python", "validate_task_test_refs.py")
        };
    }

    private static AutomationEntryEvaluation EvaluateAutomationEntryPoints(
        IEnumerable<string> requiredEntries,
        IEnumerable<string> allowedEntries)
    {
        var allowed = new HashSet<string>(
            allowedEntries.Select(NormalizeFullPath),
            StringComparer.OrdinalIgnoreCase);
        var reasons = new List<string>();

        foreach (var entry in requiredEntries)
        {
            var normalized = NormalizeFullPath(entry);
            if (!allowed.Contains(normalized))
            {
                reasons.Add($"Legacy or unsupported automation entrypoint: {normalized}");
            }

            if (!ExistsAsFileOrDirectory(entry))
            {
                reasons.Add($"Missing automation entrypoint: {normalized}");
            }
        }

        return new AutomationEntryEvaluation(reasons.Count == 0, reasons);
    }

    private static string[] GetTrackedFiles(string root, string searchPattern)
    {
        return Directory
            .EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(root, path))
            .ToArray();
    }

    private static string GetQuotedProjectSetting(string projectText, string key)
    {
        var prefix = key + "=";
        using var reader = new StringReader(projectText);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var rawValue = trimmed[prefix.Length..].Trim();
            rawValue.Length.Should().BeGreaterThan(1);
            (rawValue[0] == '"' && rawValue[^1] == '"').Should().BeTrue();
            return rawValue.Trim('"');
        }

        throw new InvalidOperationException("Could not locate the expected project setting.");
    }

    private static string ResolveResPath(string root, string resPath)
    {
        resPath.StartsWith("res://", StringComparison.Ordinal).Should().BeTrue();
        var relativePath = resPath["res://".Length..].Replace('/', Path.DirectorySeparatorChar);

        return Path.Combine(root, relativePath);
    }

    private static bool IsIgnoredPath(string root, string path)
    {
        var relativePath = Path.GetRelativePath(root, path);
        var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(static segment =>
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals(".godot", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDotNetHostSignal()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) &&
            Path.GetFileName(processPath).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return HasExecutableOnPath("dotnet.exe");
    }

    private static bool HasDotNetSdkSignal()
    {
        if (!TryRunProcess("dotnet", "--list-sdks", out var output))
        {
            return false;
        }

        return output
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains('[', StringComparison.Ordinal) && line.Contains(']', StringComparison.Ordinal));
    }

    private static bool HasGodot451Signal(string root)
    {
        foreach (var candidate in EnumerateGodotCandidates(root))
        {
            if (TryRunProcess(candidate, "--version", out var output) &&
                output.IndexOf("4.5.1", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (candidate.IndexOf("4.5.1", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateGodotCandidates(string root)
    {
        var configuredPath = Environment.GetEnvironmentVariable("GODOT_BIN");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            yield return configuredPath;
        }

        var fixedCandidates = new[]
        {
            Path.Combine(root, "Godot_v4.5.1-stable_win64.exe"),
            Path.Combine(root, "Godot_v4.5.1-stable_win64_console.exe"),
            Path.Combine(root, "Godot_v4.5.1-stable_mono_win64", "Godot_v4.5.1-stable_mono_win64_console.exe")
        };

        foreach (var candidate in fixedCandidates.Where(File.Exists))
        {
            yield return candidate;
        }

        foreach (var fileName in new[]
                 {
                     "Godot_v4.5.1-stable_win64.exe",
                     "Godot_v4.5.1-stable_win64_console.exe",
                     "Godot_v4.5.1-stable_mono_win64_console.exe"
                 })
        {
            if (HasExecutableOnPath(fileName))
            {
                yield return fileName;
            }
        }
    }

    private static bool HasExecutableOnPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        var searchRoots = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return searchRoots.Any(path =>
        {
            var candidate = Path.Combine(path.Trim().Trim('"'), fileName);
            return File.Exists(candidate);
        });
    }

    private static bool TryRunProcess(string fileName, string arguments, out string output)
    {
        output = string.Empty;

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            if (!process.WaitForExit(15_000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                return false;
            }

            output = (process.StandardOutput.ReadToEnd() + Environment.NewLine + process.StandardError.ReadToEnd()).Trim();
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeFullPath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private sealed record AutomationEntryEvaluation(bool IsAccepted, IReadOnlyCollection<string> Reasons);
}
