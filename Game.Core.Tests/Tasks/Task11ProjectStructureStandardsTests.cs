using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks
{
    public sealed class Task11ProjectStructureStandardsTests
    {
        private const string CanonicalMainScene = "res://Game.Godot/Scenes/Main.tscn";
        private const string CanonicalFeatureFlags = "res://Game.Godot/Scripts/Config/FeatureFlags.cs";
        private const string CanonicalSettingsLoader = "res://Game.Godot/Scripts/UI/SettingsLoader.cs";
        private const string CanonicalConfigStoragePath = "user://config/features.json";

        private static readonly string RepoRoot =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void ShouldDeclareFourStructureCategories_WhenBuildingProjectStructureStandard()
        {
            var standard = BuildProjectStructureStandard();

            standard.Should().HaveCount(4);
            standard.Select(static category => category.Name).Should().Equal("scripts", "scenes", "assets", "config");
            standard.Select(static category => category.RelativePath).Should().OnlyHaveUniqueItems();
        }

        // ACC:T11.9
        [Fact]
        public void ShouldLocateDedicatedDirectories_WhenRepositoryMatchesProjectStructureStandard()
        {
            var standard = BuildProjectStructureStandard();

            var missingDirectories = FindMissingDirectories(RepoRoot, standard).ToArray();

            missingDirectories.Should().BeEmpty();
            standard.Should().OnlyContain(category => Directory.Exists(ResolvePath(RepoRoot, category.RelativePath)));
        }

        [Fact]
        public void ShouldShowUsageMarkers_WhenEvaluatingProjectStructureStandard()
        {
            var usage = EvaluateUsage(RepoRoot);
            var projectText = File.ReadAllText(ResolvePath(RepoRoot, "project.godot"));
            var evaluation = EvaluateStructureStandard(
                existingRelativeDirectories: CollectRelevantDirectories(RepoRoot),
                configuredMainScene: ReadConfiguredMainScene(projectText),
                configEntrypoints: ReadConfigEntrypoints(RepoRoot));

            usage.HasScripts.Should().BeTrue();
            usage.HasScenes.Should().BeTrue();
            usage.HasAssets.Should().BeTrue();
            usage.HasConfig.Should().BeTrue();
            evaluation.IsAccepted.Should().BeTrue();
            evaluation.Reasons.Should().BeEmpty();
        }

        // ACC:T11.9 ACC:T11.26
        [Fact]
        public void ShouldRejectParallelRootsOrSubstitutePaths_WhenCanonicalStructureIsNotUsed()
        {
            var evaluation = EvaluateStructureStandard(
                existingRelativeDirectories: new[]
                {
                    "scripts",
                    "Game.Godot/Scenes",
                    "Scenes",
                    "Game.Godot/Assets",
                    "Assets",
                    "Game.Godot/Scripts/Config",
                    "config"
                },
                configuredMainScene: "res://Scenes/Main.tscn",
                configEntrypoints: new[]
                {
                    "res://config/FeatureFlags.cs",
                    "res://config/SettingsLoader.cs"
                });

            evaluation.IsAccepted.Should().BeFalse();
            evaluation.Reasons.Should().Contain(reason => reason.Contains("canonical scene root", StringComparison.Ordinal));
            evaluation.Reasons.Should().Contain(reason => reason.Contains("Parallel roots or substitute paths", StringComparison.Ordinal));
            evaluation.Reasons.Should().Contain(reason => reason.Contains("config layer", StringComparison.Ordinal));
        }

        // ACC:T11.11
        [Fact]
        public void ShouldKeepConfigurationEntrypointsInsideStandardizedConfigLayer_WhenBootstrapLoadsConfiguration()
        {
            var projectText = File.ReadAllText(ResolvePath(RepoRoot, "project.godot"));
            var mainSceneText = File.ReadAllText(ResolvePath(RepoRoot, "Game.Godot/Scenes/Main.tscn"));
            var featureFlagsText = File.ReadAllText(ResolvePath(RepoRoot, "Game.Godot/Scripts/Config/FeatureFlags.cs"));

            var evaluation = EvaluateConfigLayerBinding(
                configuredFeatureFlagsPath: ReadAutoloadPath(projectText, "FeatureFlags"),
                mainSceneLoaderPath: ReadSceneResourcePathContaining(mainSceneText, "SettingsLoader.cs"),
                userStoragePath: ReadStoragePath(featureFlagsText));

            evaluation.IsAccepted.Should().BeTrue();
            evaluation.Reasons.Should().BeEmpty();
            Directory.Exists(ResolvePath(RepoRoot, "Game.Godot/Scripts/Config")).Should().BeTrue();
            File.Exists(ResolvePath(RepoRoot, "Game.Godot/Scripts/Config/FeatureFlags.cs")).Should().BeTrue();
        }

        // ACC:T11.11
        [Fact]
        public void ShouldRejectConfigurationBypass_WhenBootstrapConfigLayerUsesNonCanonicalPaths()
        {
            var evaluation = EvaluateConfigLayerBinding(
                configuredFeatureFlagsPath: "res://config/FeatureFlags.cs",
                mainSceneLoaderPath: "res://config/SettingsLoader.cs",
                userStoragePath: CanonicalConfigStoragePath);

            evaluation.IsAccepted.Should().BeFalse();
            evaluation.Reasons.Should().Contain(reason => reason.Contains("FeatureFlags", StringComparison.Ordinal));
            evaluation.Reasons.Should().Contain(reason => reason.Contains("SettingsLoader", StringComparison.Ordinal));
        }

        private static IReadOnlyList<StructureCategory> BuildProjectStructureStandard()
        {
            return new[]
            {
                new StructureCategory("scripts", "scripts"),
                new StructureCategory("scenes", "Game.Godot/Scenes"),
                new StructureCategory("assets", "Game.Godot/Assets"),
                new StructureCategory("config", "Game.Godot/Scripts/Config")
            };
        }

        private static IEnumerable<StructureCategory> FindMissingDirectories(
            string repoRoot,
            IEnumerable<StructureCategory> categories)
        {
            return categories.Where(category => !Directory.Exists(ResolvePath(repoRoot, category.RelativePath)));
        }

        private static StructureUsage EvaluateUsage(string repoRoot)
        {
            var scriptsRoot = ResolvePath(repoRoot, "scripts");
            var scenesRoot = ResolvePath(repoRoot, "Game.Godot/Scenes");
            var assetsRoot = ResolvePath(repoRoot, "Game.Godot/Assets");
            var configRoot = ResolvePath(repoRoot, "Game.Godot/Scripts/Config");

            var hasScripts =
                Directory.Exists(scriptsRoot) &&
                Directory.EnumerateFiles(scriptsRoot, "*.*", SearchOption.AllDirectories).Any();

            var hasScenes =
                Directory.Exists(scenesRoot) &&
                Directory.EnumerateFiles(scenesRoot, "*.tscn", SearchOption.AllDirectories).Any();

            var hasAssets =
                Directory.Exists(assetsRoot) &&
                Directory.Exists(Path.Combine(assetsRoot, "Audio")) &&
                Directory.Exists(Path.Combine(assetsRoot, "Fonts")) &&
                Directory.Exists(Path.Combine(assetsRoot, "Models")) &&
                Directory.Exists(Path.Combine(assetsRoot, "Textures"));

            var hasConfig =
                Directory.Exists(configRoot) &&
                Directory.EnumerateFiles(configRoot, "*.*", SearchOption.AllDirectories).Any() &&
                File.Exists(ResolvePath(repoRoot, "project.godot"));

            return new StructureUsage(hasScripts, hasScenes, hasAssets, hasConfig);
        }

        private static StructureStandardEvaluation EvaluateStructureStandard(
            IReadOnlyCollection<string> existingRelativeDirectories,
            string configuredMainScene,
            IReadOnlyCollection<string> configEntrypoints)
        {
            var normalized = new HashSet<string>(
                existingRelativeDirectories.Select(NormalizeRelativePath),
                StringComparer.OrdinalIgnoreCase);
            var reasons = new List<string>();

            if (!normalized.Contains(NormalizeRelativePath("scripts")))
            {
                reasons.Add("Canonical scripts root must exist.");
            }

            if (!normalized.Contains(NormalizeRelativePath("Game.Godot/Scenes")))
            {
                reasons.Add("Canonical scene root must exist.");
            }

            if (!normalized.Contains(NormalizeRelativePath("Game.Godot/Assets")))
            {
                reasons.Add("Canonical asset root must exist.");
            }

            if (!normalized.Contains(NormalizeRelativePath("Game.Godot/Scripts/Config")))
            {
                reasons.Add("Canonical config layer must exist.");
            }

            if (normalized.Contains(NormalizeRelativePath("Scenes")) ||
                normalized.Contains(NormalizeRelativePath("Assets")))
            {
                reasons.Add("Parallel roots or substitute paths do not satisfy the canonical structure standard.");
            }

            if (!string.Equals(configuredMainScene, CanonicalMainScene, StringComparison.Ordinal))
            {
                reasons.Add("Main scene must stay inside the canonical scene root.");
            }

            if (configEntrypoints.Any(path => !IsCanonicalConfigPath(path)))
            {
                reasons.Add("Bootstrap configuration entrypoints may not bypass the canonical config layer.");
            }

            return new StructureStandardEvaluation(reasons.Count == 0, reasons);
        }

        private static ConfigLayerEvaluation EvaluateConfigLayerBinding(
            string configuredFeatureFlagsPath,
            string mainSceneLoaderPath,
            string userStoragePath)
        {
            var reasons = new List<string>();

            if (!string.Equals(configuredFeatureFlagsPath, CanonicalFeatureFlags, StringComparison.Ordinal))
            {
                reasons.Add("FeatureFlags must be loaded from the canonical config layer.");
            }

            if (!string.Equals(mainSceneLoaderPath, CanonicalSettingsLoader, StringComparison.Ordinal))
            {
                reasons.Add("SettingsLoader must keep the canonical startup binding.");
            }

            if (!string.Equals(userStoragePath, CanonicalConfigStoragePath, StringComparison.Ordinal))
            {
                reasons.Add("Config storage must remain under user://config/features.json.");
            }

            return new ConfigLayerEvaluation(reasons.Count == 0, reasons);
        }

        private static IReadOnlyCollection<string> CollectRelevantDirectories(string repoRoot)
        {
            return new[]
            {
                "scripts",
                "Game.Godot/Scenes",
                "Scenes",
                "Game.Godot/Assets",
                "Assets",
                "Game.Godot/Scripts/Config",
                "config"
            }
            .Where(relativePath => Directory.Exists(ResolvePath(repoRoot, relativePath)))
            .ToArray();
        }

        private static IReadOnlyCollection<string> ReadConfigEntrypoints(string repoRoot)
        {
            var projectText = File.ReadAllText(ResolvePath(repoRoot, "project.godot"));
            var mainSceneText = File.ReadAllText(ResolvePath(repoRoot, "Game.Godot/Scenes/Main.tscn"));

            return new[]
            {
                ReadAutoloadPath(projectText, "FeatureFlags"),
                ReadSceneResourcePathContaining(mainSceneText, "SettingsLoader.cs")
            };
        }

        private static string ReadConfiguredMainScene(string projectText)
        {
            const string prefix = "run/main_scene=\"";
            return ReadQuotedValue(projectText, prefix);
        }

        private static string ReadAutoloadPath(string projectText, string autoloadName)
        {
            return ReadQuotedValue(projectText, autoloadName + "=\"");
        }

        private static string ReadSceneResourcePathContaining(string sceneText, string fileName)
        {
            const string pathToken = "path=\"";
            foreach (var line in sceneText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (line.IndexOf(fileName, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var start = line.IndexOf(pathToken, StringComparison.Ordinal);
                if (start < 0)
                {
                    continue;
                }

                start += pathToken.Length;
                var end = line.IndexOf('"', start);
                if (end > start)
                {
                    return line[start..end];
                }
            }

            throw new InvalidOperationException($"Could not locate a scene resource path ending with '{fileName}'.");
        }

        private static string ReadStoragePath(string sourceText)
        {
            const string marker = "user://config/features.json";
            sourceText.Should().Contain(marker);
            return marker;
        }

        private static string ReadQuotedValue(string text, string prefix)
        {
            foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var end = trimmed.IndexOf('"', prefix.Length);
                if (end > prefix.Length)
                {
                    return trimmed[prefix.Length..end];
                }
            }

            throw new InvalidOperationException($"Could not locate a quoted value for prefix '{prefix}'.");
        }

        private static bool IsCanonicalConfigPath(string path)
        {
            return string.Equals(path, CanonicalFeatureFlags, StringComparison.Ordinal) ||
                   string.Equals(path, CanonicalSettingsLoader, StringComparison.Ordinal);
        }

        private static string ResolvePath(string repoRoot, string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                repoRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string NormalizeRelativePath(string relativePath)
        {
            return relativePath.Replace('\\', '/').Trim().Trim('/');
        }

        private sealed record StructureCategory(string Name, string RelativePath);

        private sealed record StructureUsage(
            bool HasScripts,
            bool HasScenes,
            bool HasAssets,
            bool HasConfig);

        private sealed record StructureStandardEvaluation(bool IsAccepted, IReadOnlyCollection<string> Reasons);

        private sealed record ConfigLayerEvaluation(bool IsAccepted, IReadOnlyCollection<string> Reasons);
    }
}
