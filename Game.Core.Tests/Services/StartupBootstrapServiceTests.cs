using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

// RED-FIRST for Task 11.
// ADR refs: ADR-0011, ADR-0005.
public sealed class StartupBootstrapServiceTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ACC:T11.18
    [Trait("acceptance", "ACC:T11.18")]
    [Fact]
    public void ShouldEnterSinglePlayerStartupFlowWithoutMultiplayerInitialization_WhenValidatingWindowsBootstrapPath()
    {
        var analysis = AnalyzeMainSceneBootstrap();

        analysis.HasCSharpBootstrapBinding.Should().BeTrue();
        analysis.HasSinglePlayerStartupTarget.Should().BeTrue();
        analysis.RequiresMultiplayerSession.Should().BeFalse();
        analysis.SupportsValidatedSinglePlayerWindowsStartup.Should().BeTrue();
    }

    // ACC:T11.19
    [Trait("acceptance", "ACC:T11.19")]
    [Fact]
    public void ShouldCompleteMainSceneStartupBinding_WhenRunningBootstrapPath()
    {
        var analysis = AnalyzeMainSceneBootstrap();

        analysis.HasConfiguredMainScene.Should().BeTrue();
        analysis.HasCSharpLifecycleBootstrapBinding.Should().BeTrue();
        analysis.PassesObligationLockO7.Should().BeTrue();
    }

    [Fact]
    public void ShouldRejectSinglePlayerStartup_WhenBootstrapRequiresMultiplayerSessionInitialization()
    {
        var analysis = AnalyzeBootstrapAssets(
            mainSceneConfigured: true,
            scriptAssets:
            [
                new ScriptAsset(
                    "res://Game.Godot/Scripts/Bootstrap/StartupBootstrapService.cs",
                    """
                    using Godot;

                    public sealed class StartupBootstrapService : Node
                    {
                        public override void _Ready()
                        {
                            InitializeMultiplayerSession();
                            GetTree().ChangeSceneToFile("res://Game.Godot/Scenes/Screens/StartScreen.tscn");
                        }
                    }
                    """)
            ]);

        analysis.RequiresMultiplayerSession.Should().BeTrue();
        analysis.SupportsValidatedSinglePlayerWindowsStartup.Should().BeFalse();
    }

    [Fact]
    public void ShouldFailObligationLockO7_WhenMainSceneBindingOrCSharpBootstrapLifecycleBindingIsMissing()
    {
        var missingMainSceneBinding = AnalyzeBootstrapAssets(
            mainSceneConfigured: false,
            scriptAssets:
            [
                new ScriptAsset(
                    "res://Game.Godot/Scripts/Bootstrap/InputMapper.cs",
                    """
                    using Godot;

                    public partial class InputMapper : Node
                    {
                        public override void _Ready()
                        {
                        }
                    }
                    """)
            ]);

        var missingCSharpLifecycleBinding = AnalyzeBootstrapAssets(
            mainSceneConfigured: true,
            scriptAssets:
            [
                new ScriptAsset(
                    "res://Game.Godot/Scripts/Main.gd",
                    """
                    extends Control

                    func _ready() -> void:
                        pass
                    """)
            ]);

        missingMainSceneBinding.PassesObligationLockO7.Should().BeFalse();
        missingCSharpLifecycleBinding.PassesObligationLockO7.Should().BeFalse();
    }

    private static BootstrapAnalysis AnalyzeMainSceneBootstrap()
    {
        var projectText = File.ReadAllText(ResolvePath(RepoRoot, "project.godot"));
        var sceneText = File.ReadAllText(ResolvePath(RepoRoot, "Game.Godot/Scenes/Main.tscn"));
        var scriptAssets = ReadSceneScriptAssets(sceneText);
        var hasConfiguredMainScene = projectText.Contains(
            "run/main_scene=\"res://Game.Godot/Scenes/Main.tscn\"",
            StringComparison.Ordinal);

        return AnalyzeBootstrapAssets(hasConfiguredMainScene, scriptAssets);
    }

    private static BootstrapAnalysis AnalyzeBootstrapAssets(bool mainSceneConfigured, IEnumerable<ScriptAsset> scriptAssets)
    {
        var assets = scriptAssets.ToArray();
        var aggregateText = string.Join(Environment.NewLine, assets.Select(static asset => asset.Text));
        var csharpScriptPaths = assets
            .Where(static asset => asset.ResourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(static asset => asset.ResourcePath)
            .ToArray();
        var csharpBootstrapScriptPaths = csharpScriptPaths
            .Where(static path =>
                path.Contains("/Scripts/Bootstrap/", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/Autoloads/", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var hasCSharpLifecycleBootstrapBinding = assets.Any(asset =>
            asset.ResourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
            (asset.ResourcePath.Contains("/Scripts/Bootstrap/", StringComparison.OrdinalIgnoreCase) ||
             asset.ResourcePath.Contains("/Autoloads/", StringComparison.OrdinalIgnoreCase)) &&
            ContainsAny(asset.Text, CSharpLifecycleMarkers));

        return new BootstrapAnalysis(
            HasConfiguredMainScene: mainSceneConfigured,
            HasCSharpBootstrapBinding: csharpBootstrapScriptPaths.Length > 0,
            HasCSharpLifecycleBootstrapBinding: hasCSharpLifecycleBootstrapBinding,
            HasSinglePlayerStartupTarget: ContainsAny(aggregateText, SinglePlayerStartupMarkers),
            RequiresMultiplayerSession: ContainsAny(aggregateText, MultiplayerSessionMarkers),
            CSharpScriptPaths: csharpScriptPaths,
            CSharpBootstrapScriptPaths: csharpBootstrapScriptPaths);
    }

    private static ScriptAsset[] ReadSceneScriptAssets(string sceneText)
    {
        const string marker = "[ext_resource type=\"Script\" path=\"";
        var assets = new List<ScriptAsset>();
        var searchIndex = 0;

        while (searchIndex < sceneText.Length)
        {
            var startIndex = sceneText.IndexOf(marker, searchIndex, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                break;
            }

            startIndex += marker.Length;
            var endIndex = sceneText.IndexOf('"', startIndex);
            if (endIndex <= startIndex)
            {
                throw new InvalidOperationException("Invalid script ext_resource.");
            }

            var resourcePath = sceneText.Substring(startIndex, endIndex - startIndex);
            var scriptText = File.ReadAllText(ResolvePath(RepoRoot, resourcePath.Replace("res://", string.Empty, StringComparison.Ordinal)));
            assets.Add(new ScriptAsset(resourcePath, scriptText));
            searchIndex = endIndex + 1;
        }

        assets.Should().NotBeEmpty();
        return assets.ToArray();
    }

    private static bool ContainsAny(string text, params string[] markers)
    {
        foreach (var marker in markers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolvePath(string repoRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(
            repoRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static readonly string[] CSharpLifecycleMarkers =
    {
        "public override void _Ready(",
        "public override void _EnterTree(",
        "public override void _Process(",
        "public override void _PhysicsProcess("
    };

    private static readonly string[] SinglePlayerStartupMarkers =
    {
        "ui.menu.start",
        "StartScreen.tscn",
        "StartGame(",
        "SinglePlayer",
        "singleplayer"
    };

    private static readonly string[] MultiplayerSessionMarkers =
    {
        "ENetMultiplayerPeer",
        "SceneMultiplayer",
        "CreateServer(",
        "CreateClient(",
        "SetMultiplayerPeer(",
        "InitializeMultiplayerSession",
        "InitializeSession",
        "MultiplayerPeer",
        "multiplayer."
    };

    private sealed record ScriptAsset(string ResourcePath, string Text);

    private sealed record BootstrapAnalysis(
        bool HasConfiguredMainScene,
        bool HasCSharpBootstrapBinding,
        bool HasCSharpLifecycleBootstrapBinding,
        bool HasSinglePlayerStartupTarget,
        bool RequiresMultiplayerSession,
        string[] CSharpScriptPaths,
        string[] CSharpBootstrapScriptPaths)
    {
        public bool PassesObligationLockO7 => HasConfiguredMainScene && HasCSharpLifecycleBootstrapBinding;

        public bool SupportsValidatedSinglePlayerWindowsStartup =>
            PassesObligationLockO7 &&
            HasCSharpBootstrapBinding &&
            HasSinglePlayerStartupTarget &&
            !RequiresMultiplayerSession;
    }
}
