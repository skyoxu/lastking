using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Architecture;

public sealed class BattleMapLayoutContractTests
{
    private const string RuntimeBridgePath = "Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs";
    private const string SelectionDataProviderPath = "Game.Godot/Scripts/Screens/BattleMapSelectionDataProvider.gd";
    private const string BuildPlacementControllerPath = "Game.Godot/Scripts/Screens/BattleMapBuildPlacementController.gd";
    private const string CombatDebugGeometryControllerPath = "Game.Godot/Scripts/Screens/BattleMapCombatDebugGeometryController.gd";
    private const string FeedbackControllerPath = "Game.Godot/Scripts/Screens/BattleMapFeedbackController.gd";
    private const string BattleMapScreenPath = "Game.Godot/Scenes/Screens/BattleMapScreen.tscn";

    [Fact]
    public void ShouldKeepRuntimeFallbackMarkersInsideCurrentBattleMapRegions()
    {
        var runtimeBridge = ReadRepositoryFile(RuntimeBridgePath);

        runtimeBridge.ShouldContainPattern(@"\[""MgTower""\]\s*=\s*new\s+Vector2\(216f,\s*24f\)");
        runtimeBridge.ShouldContainPattern(@"\[""Barracks""\]\s*=\s*new\s+Vector2\(600f,\s*24f\)");
        runtimeBridge.ShouldContainPattern(@"\[""Residence""\]\s*=\s*new\s+Vector2\(360f,\s*24f\)");
        runtimeBridge.ShouldNotContainPattern(@"\[""MgTower""\]\s*=\s*new\s+Vector2\(792f,\s*312f\)");
        runtimeBridge.ShouldNotContainPattern(@"\[""Barracks""\]\s*=\s*new\s+Vector2\(648f,\s*312f\)");
        runtimeBridge.ShouldNotContainPattern(@"\[""Residence""\]\s*=\s*new\s+Vector2\(936f,\s*312f\)");
    }

    [Fact]
    public void ShouldMapBarracksDefaultSelectionAndVisualsToRightOuterField()
    {
        var selectionDataProvider = ReadRepositoryFile(SelectionDataProviderPath);
        var buildPlacementController = ReadRepositoryFile(BuildPlacementControllerPath);
        var combatDebugGeometryController = ReadRepositoryFile(CombatDebugGeometryControllerPath);
        var feedbackController = ReadRepositoryFile(FeedbackControllerPath);

        selectionDataProvider.ShouldContainPattern(@"""Battlefield/Barracks""\s*:\s*\{[^}]*""slot_id""\s*:\s*""RightOuterFieldSlot_00_00""");
        buildPlacementController.ShouldContainPattern(@"""barracks_alpha""\s*:\s*snapshot\[""building_slots""\]\s*=\s*\[""RightOuterFieldSlot_00_00""\]");
        combatDebugGeometryController.ShouldContainPattern(@"""Barracks""\s*:\s*""RightOuterFieldSlot_00_00""");
        feedbackController.ShouldContainPattern(@"""Barracks""\s*:\s*""RightOuterFieldSlot_00_00""");

        selectionDataProvider.ShouldNotContainPattern(@"""Battlefield/Barracks""\s*:\s*\{[^}]*""slot_id""\s*:\s*""InnerCastleRegionSlot_00_00""");
        buildPlacementController.ShouldNotContainPattern(@"""barracks_alpha""\s*:\s*snapshot\[""building_slots""\]\s*=\s*\[""InnerCastleRegionSlot_00_00""\]");
        combatDebugGeometryController.ShouldNotContainPattern(@"""Barracks""\s*:\s*""InnerCastleRegionSlot_00_00""");
        feedbackController.ShouldNotContainPattern(@"""Barracks""\s*:\s*""InnerCastleRegionSlot_00_00""");
    }

    [Fact]
    public void ShouldKeepBarracksBuildingSlotSeparateFromLinkedUnitSlot()
    {
        var selectionDataProvider = ReadRepositoryFile(SelectionDataProviderPath);
        var task61FeedbackTest = ReadRepositoryFile("Tests.Godot/tests/Scenes/BattleMap/test_task61_building_selection_feedback.gd");

        selectionDataProvider.ShouldContainPattern(@"snapshot\[""building_slots""\]\s*=\s*\[slot_id\]");
        selectionDataProvider.ShouldContainPattern(@"""Battlefield/Barracks""\s*:\s*\{[^}]*""slot_id""\s*:\s*""RightOuterFieldSlot_00_00""");
        selectionDataProvider.ShouldContainPattern(@"snapshot\[""linked_unit_slots""\]\s*=\s*\[""RightOuterFieldSlot_01_00""\]");
        selectionDataProvider.ShouldNotContainPattern(@"snapshot\[""linked_unit_slots""\]\s*=\s*\[""RightOuterFieldSlot_00_00""\]");

        task61FeedbackTest.Should().Contain("\"building_slots\": [\"RightOuterFieldSlot_00_00\"]");
        task61FeedbackTest.Should().Contain("\"linked_unit_slots\": [\"RightOuterFieldSlot_01_00\"]");
        task61FeedbackTest.Should().NotContain("\"linked_unit_slots\": [\"RightOuterFieldSlot_00_00\"]");
    }

    [Fact]
    public void ShouldKeepScenePathAlignedWithRightSpawnRuntimePath()
    {
        var battleMapScreen = ReadRepositoryFile(BattleMapScreenPath);

        battleMapScreen.ShouldContainPattern(@"points\s*=\s*PackedVector2Array\(1560,\s*312,\s*24,\s*312\)");
        battleMapScreen.ShouldNotContainPattern(@"points\s*=\s*PackedVector2Array\(96,\s*144,\s*336,\s*144,\s*576,\s*240,\s*792,\s*240,\s*1008,\s*336,\s*1296,\s*432,\s*1488,\s*432\)");
    }

    [Fact]
    public void ShouldNotKeepLeftSpawnLaneRuntimeSemantics()
    {
        var runtimeBridge = ReadRepositoryFile(RuntimeBridgePath);

        runtimeBridge.ShouldContainPattern(@"private\s+enum\s+SpawnLane\s*\{\s*Right\s*=\s*0");
        runtimeBridge.ShouldNotContainPattern(@"\bLeft\s*=\s*0");
        runtimeBridge.Should().NotContain("SpawnLane.Left");
        runtimeBridge.ShouldNotContainPattern(@"AppendWaveLaneSpawns\(waveEntry,\s*""left""");
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "Lastking.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Repository root not found from test base directory.");
    }
}

file static class BattleMapLayoutContractTestStringExtensions
{
    public static void ShouldContainPattern(this string text, string pattern)
    {
        Regex.IsMatch(text, pattern, RegexOptions.Singleline).Should().BeTrue($"expected to match pattern {pattern}");
    }

    public static void ShouldNotContainPattern(this string text, string pattern)
    {
        Regex.IsMatch(text, pattern, RegexOptions.Singleline).Should().BeFalse($"expected not to match pattern {pattern}");
    }
}
