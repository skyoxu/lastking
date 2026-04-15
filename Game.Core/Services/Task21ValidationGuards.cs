using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed record Task21BaselineValidationRequest(
    string ProjectRootName,
    bool ReusesExistingT11Root,
    bool CreatesNewProject,
    bool InitializesProjectScaffold,
    bool RebootstrapsProject,
    bool ValidatesWindowsExport,
    bool ValidatesSteamRuntime,
    string[] ExistingFolders);

public sealed record Task21ValidationResult(bool IsAccepted, string? FailureReason);

public static class T11BaselineReuseGuard
{
    public static Task21ValidationResult Validate(Task21BaselineValidationRequest request)
    {
        if (!request.ReusesExistingT11Root || !string.Equals(request.ProjectRootName, "T11", StringComparison.Ordinal))
        {
            return new Task21ValidationResult(false, "validation must run on the existing T11 project root");
        }

        if (request.CreatesNewProject || request.InitializesProjectScaffold)
        {
            return new Task21ValidationResult(false, "project re-initialization is forbidden for Task 21 validation");
        }

        if (request.RebootstrapsProject)
        {
            return new Task21ValidationResult(false, "scope violation: project bootstrap is out of Task 21 validation scope");
        }

        if (!request.ValidatesWindowsExport || !request.ValidatesSteamRuntime)
        {
            return new Task21ValidationResult(false, "scope violation: Task 21 validates Windows export and Steam runtime only");
        }

        var requiredFolders = new[] { "scripts", "scenes", "configs", "saves", "assets", "ui", "audio" };
        var existingFolders = new HashSet<string>(request.ExistingFolders, StringComparer.OrdinalIgnoreCase);
        var missingFolder = requiredFolders.FirstOrDefault(requiredFolder => !existingFolders.Contains(requiredFolder));
        if (missingFolder is not null)
        {
            return new Task21ValidationResult(false, $"missing required baseline folder: {missingFolder}");
        }

        return new Task21ValidationResult(true, null);
    }
}

public sealed record Task21StartupLaunchEvidence(
    string RunId,
    bool IsWindowsExport,
    bool IsSteamRuntimeContext,
    string SteamAppId,
    bool StartupReachedReadyState,
    bool HasSteamBlockingIntegrationErrors,
    bool SteamRuntimeChecksExecuted)
{
    public static Task21StartupLaunchEvidence WindowsSteamRuntimeStub(
        string runId,
        bool startupReachedReadyState,
        bool hasSteamBlockingIntegrationErrors,
        bool steamRuntimeChecksExecuted)
    {
        return new Task21StartupLaunchEvidence(
            RunId: runId,
            IsWindowsExport: true,
            IsSteamRuntimeContext: true,
            SteamAppId: "steam-launch-stub",
            StartupReachedReadyState: startupReachedReadyState,
            HasSteamBlockingIntegrationErrors: hasSteamBlockingIntegrationErrors,
            SteamRuntimeChecksExecuted: steamRuntimeChecksExecuted);
    }

    public static Task21StartupLaunchEvidence WindowsSteamRuntimeRealAppId(
        string runId,
        string steamAppId,
        bool startupReachedReadyState,
        bool hasSteamBlockingIntegrationErrors,
        bool steamRuntimeChecksExecuted)
    {
        return new Task21StartupLaunchEvidence(
            RunId: runId,
            IsWindowsExport: true,
            IsSteamRuntimeContext: true,
            SteamAppId: steamAppId,
            StartupReachedReadyState: startupReachedReadyState,
            HasSteamBlockingIntegrationErrors: hasSteamBlockingIntegrationErrors,
            SteamRuntimeChecksExecuted: steamRuntimeChecksExecuted);
    }
}

public sealed record Task21StartupValidationResult(bool IsAccepted, string? FailureReason);

public static class SteamRuntimeStartupValidator
{
    public static Task21StartupValidationResult Validate(IReadOnlyList<Task21StartupLaunchEvidence> launches)
    {
        if (launches.Count == 0)
        {
            return new Task21StartupValidationResult(false, "no launch evidence was provided");
        }

        if (launches.All(launch => !launch.SteamRuntimeChecksExecuted))
        {
            return new Task21StartupValidationResult(false, "steam runtime launch checks were skipped");
        }

        var windowsSteamRuntimeLaunches = launches
            .Where(launch => launch.IsWindowsExport && launch.IsSteamRuntimeContext && launch.SteamRuntimeChecksExecuted)
            .ToArray();
        if (windowsSteamRuntimeLaunches.Length == 0)
        {
            return new Task21StartupValidationResult(false, "at least one Windows Steam runtime launch is required");
        }

        var hasAcceptedLaunch = windowsSteamRuntimeLaunches.Any(launch =>
            IsAllowedSteamRuntimeId(launch.SteamAppId) &&
            launch.StartupReachedReadyState &&
            !launch.HasSteamBlockingIntegrationErrors);
        if (!hasAcceptedLaunch)
        {
            return new Task21StartupValidationResult(false, "no qualifying Steam runtime startup launch was found");
        }

        return new Task21StartupValidationResult(true, null);
    }

    private static bool IsAllowedSteamRuntimeId(string steamAppId)
    {
        if (string.IsNullOrWhiteSpace(steamAppId))
        {
            return false;
        }

        if (string.Equals(steamAppId, "steam-launch-stub", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return steamAppId.All(char.IsDigit);
    }
}

public sealed record ProjectSettingsBaselineSnapshot(
    string ApplicationConfigName,
    string ApplicationRunMainScene,
    int WindowWidth,
    int WindowHeight);

public static class ProjectSettingsBaselineValidator
{
    public static bool IsNameAndMainSceneLocked(ProjectSettingsBaselineSnapshot baseline)
    {
        return baseline.ApplicationConfigName == "LASTKING" &&
               baseline.ApplicationRunMainScene == "res://scenes/Main.tscn";
    }

    public static bool IsWindowResolutionLocked(ProjectSettingsBaselineSnapshot baseline)
    {
        return baseline.WindowWidth == 1920 &&
               baseline.WindowHeight == 1080;
    }
}

public sealed record WindowsExportProfileContract(
    bool WindowsDesktop64Enabled,
    string SteamAppId,
    bool LinuxEnabled,
    bool MacOsEnabled,
    bool WebEnabled,
    bool AndroidEnabled,
    bool IosEnabled);

public sealed record Task21ContractValidationResult(bool IsAccepted, IReadOnlyList<string> Errors);

public sealed class WindowsExportProfileLockValidator
{
    public Task21ContractValidationResult Validate(WindowsExportProfileContract profile, IReadOnlyList<string> changedScopes)
    {
        var errors = new List<string>();

        if (!profile.WindowsDesktop64Enabled)
        {
            errors.Add("Windows Desktop (64-bit) must be enabled.");
        }

        if (string.IsNullOrWhiteSpace(profile.SteamAppId))
        {
            errors.Add("Steam App ID is required.");
        }

        var hasOnlyWindowsScope = changedScopes.Count == 1 &&
                                  string.Equals(changedScopes[0], "windows.export.profile", StringComparison.OrdinalIgnoreCase);
        if (!hasOnlyWindowsScope)
        {
            errors.Add("scope must be limited to windows.export.profile");
        }

        if (profile.LinuxEnabled || profile.MacOsEnabled || profile.WebEnabled || profile.AndroidEnabled || profile.IosEnabled)
        {
            errors.Add("non-Windows export targets must be disabled");
        }

        return new Task21ContractValidationResult(errors.Count == 0, errors);
    }
}

public sealed record Task21ScopeValidationRequest(
    string BaselineId,
    bool ValidatesWindowsExportProfile,
    bool ValidatesSteamStartupPath,
    bool RebootstrapProject,
    bool TouchesUnrelatedScope);

public sealed record Task21ScopeValidationResult(bool IsAccepted, string Reason);

public static class Task21ScopeGuard
{
    public static Task21ScopeValidationResult Validate(Task21ScopeValidationRequest request)
    {
        if (request.RebootstrapProject)
        {
            return new Task21ScopeValidationResult(false, "Project re-bootstrap is out of scope for Task 21.");
        }

        if (request.BaselineId != "T11")
        {
            return new Task21ScopeValidationResult(false, "Task 21 must reuse the T11 baseline.");
        }

        if (!request.ValidatesWindowsExportProfile || !request.ValidatesSteamStartupPath)
        {
            return new Task21ScopeValidationResult(false, "Task 21 requires both Windows export and Steam startup validation.");
        }

        if (request.TouchesUnrelatedScope)
        {
            return new Task21ScopeValidationResult(false, "Unrelated scope changes are out of Task 21 scope.");
        }

        return new Task21ScopeValidationResult(true, "Accepted");
    }
}

public static class Task21RuntimeTraceGuard
{
    public static bool IsEquivalent(IReadOnlyList<string> baselineTrace, IReadOnlyList<string> candidateTrace)
    {
        return baselineTrace.SequenceEqual(candidateTrace);
    }
}
