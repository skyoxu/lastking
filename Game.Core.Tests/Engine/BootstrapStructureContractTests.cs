using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Engine;

public class BootstrapStructureContractTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void ShouldDeclareStartupSceneAndStartupScript_WhenBuildingBootstrapAssetContract()
    {
        var requirements = BuildRequiredBootstrapAssetContract();

        requirements.Should().NotBeEmpty();
        requirements.Select(static requirement => requirement.Key).Should().OnlyHaveUniqueItems();
        requirements.Should().Contain(requirement =>
            requirement.Key == "ProjectConfig" &&
            NormalizeSeparators(requirement.RelativePath) == "project.godot");
        requirements.Should().Contain(requirement =>
            requirement.Key == "StartupScene" &&
            NormalizeSeparators(requirement.RelativePath) == "Game.Godot/Scenes/Main.tscn");
        requirements.Should().Contain(requirement =>
            requirement.Key == "StartupScript" &&
            NormalizeSeparators(requirement.RelativePath) == "Game.Godot/Scripts/Main.gd");
    }

    // ACC:T11.25
    [Fact]
    public void ShouldPassBootstrapStructureGate_WhenRequiredAssetsMatchRepositoryLayout()
    {
        var requirements = BuildRequiredBootstrapAssetContract();
        var missingAssets = FindMissingAssets(RepoRoot, requirements).ToArray();

        missingAssets.Should().BeEmpty();
        EvaluateGate(missingAssets).Should().BeTrue();

        var projectText = File.ReadAllText(ResolvePath(RepoRoot, "project.godot"));
        var sceneText = File.ReadAllText(ResolvePath(RepoRoot, "Game.Godot/Scenes/Main.tscn"));
        var startupScriptResourcePath = "res://Game.Godot/Scripts/Main.gd";
        var startupScriptResourceId = ReadExtResourceId(sceneText, startupScriptResourcePath);

        ReadConfiguredMainScene(projectText).Should().Be("res://Game.Godot/Scenes/Main.tscn");
        sceneText.Should().Contain($"path=\"{startupScriptResourcePath}\"");
        sceneText.Should().Contain($"script = ExtResource(\"{startupScriptResourceId}\")");
    }

    [Fact]
    public void ShouldFailBootstrapStructureGate_WhenAnyRequiredAssetIsMissing()
    {
        var requirements = BuildRequiredBootstrapAssetContract()
            .Concat(new[]
            {
                new BootstrapAssetRequirement("MissingProbe", "__contract_probe__/missing.bootstrap.asset")
            });

        var missingAssets = FindMissingAssets(RepoRoot, requirements).ToArray();

        missingAssets.Should().ContainSingle(requirement => requirement.Key == "MissingProbe");
        EvaluateGate(missingAssets).Should().BeFalse();
    }

    [Fact]
    public void ShouldReadConfiguredMainScene_WhenProjectConfigurationContainsStartupBinding()
    {
        var projectText = File.ReadAllText(ResolvePath(RepoRoot, "project.godot"));

        var configuredMainScene = ReadConfiguredMainScene(projectText);

        configuredMainScene.Should().Be("res://Game.Godot/Scenes/Main.tscn");
    }

    private static IEnumerable<BootstrapAssetRequirement> BuildRequiredBootstrapAssetContract()
    {
        return new[]
        {
            new BootstrapAssetRequirement("ProjectConfig", "project.godot"),
            new BootstrapAssetRequirement("StartupScene", "Game.Godot/Scenes/Main.tscn"),
            new BootstrapAssetRequirement("StartupScript", "Game.Godot/Scripts/Main.gd")
        };
    }

    private static IEnumerable<BootstrapAssetRequirement> FindMissingAssets(
        string repoRoot,
        IEnumerable<BootstrapAssetRequirement> requirements)
    {
        return requirements.Where(requirement => !File.Exists(ResolvePath(repoRoot, requirement.RelativePath)));
    }

    private static bool EvaluateGate(IEnumerable<BootstrapAssetRequirement> missingAssets)
    {
        return !missingAssets.Any();
    }

    private static string ReadConfiguredMainScene(string projectText)
    {
        const string prefix = "run/main_scene=\"";

        foreach (var line in projectText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var startIndex = prefix.Length;
            var endIndex = line.IndexOf('"', startIndex);
            if (endIndex <= startIndex)
            {
                throw new InvalidOperationException("Invalid run/main_scene configuration.");
            }

            return line.Substring(startIndex, endIndex - startIndex);
        }

        throw new InvalidOperationException("Missing run/main_scene configuration.");
    }

    private static string ReadExtResourceId(string sceneText, string resourcePath)
    {
        var marker = $"[ext_resource type=\"Script\" path=\"{resourcePath}\" id=\"";
        var startIndex = sceneText.IndexOf(marker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            throw new InvalidOperationException($"Missing ext_resource for {resourcePath}.");
        }

        startIndex += marker.Length;
        var endIndex = sceneText.IndexOf('"', startIndex);
        if (endIndex <= startIndex)
        {
            throw new InvalidOperationException($"Invalid ext_resource declaration for {resourcePath}.");
        }

        return sceneText.Substring(startIndex, endIndex - startIndex);
    }

    private static string ResolvePath(string repoRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(
            repoRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace('\\', '/');
    }

    private sealed record BootstrapAssetRequirement(string Key, string RelativePath);
}
