using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Architecture;

public class Task60LayerBoundaryTests
{
    private const string CoreBridgePath = "Game.Godot/Scripts/Building/BuildingModeCoreBridge.cs";
    private const string OverlayDeterminismTestPath = "Tests.Godot/tests/Integration/test_battle_map_overlay_determinism.gd";

    // ACC:T60.5
    [Fact]
    public void ShouldKeepLegalityDecisionOutOfGodot_WhenVerifyingConcreteCorePlacementContracts()
    {
        var serviceType = typeof(BuildingPlacementService);
        serviceType.Should().BeSealed("placement legality decisions belong to a dedicated Core service");

        var preview = serviceType.GetMethod("Preview", new[] { typeof(string), typeof(GridPoint) });
        preview.Should().NotBeNull();
        preview!.ReturnType.Should().Be(typeof(BuildingPlacementPreview));

        var tryPlaceByType = serviceType.GetMethod(
            "TryPlace",
            new[] { typeof(string), typeof(GridPoint), typeof(BuildingPlacementState) });
        tryPlaceByType.Should().NotBeNull();
        tryPlaceByType!.ReturnType.Should().Be(typeof(BuildingPlacementOutcome));

        var tryPlaceByBuilding = serviceType.GetMethod(
            "TryPlace",
            new[] { typeof(Building), typeof(GridPoint), typeof(BuildingPlacementState) });
        tryPlaceByBuilding.Should().NotBeNull();
        tryPlaceByBuilding!.ReturnType.Should().Be(typeof(BuildingPlacementOutcome));

        var outcomeType = typeof(BuildingPlacementOutcome);
        outcomeType.Should().BeSealed("Core should expose a concrete placement outcome contract");
        outcomeType.GetProperty(nameof(BuildingPlacementOutcome.IsAccepted))?.PropertyType.Should().Be(typeof(bool));
        outcomeType.GetProperty(nameof(BuildingPlacementOutcome.Reason))?.PropertyType.Should().Be(typeof(string));
        outcomeType.GetProperty(nameof(BuildingPlacementOutcome.EvaluatedFootprint))?.PropertyType.Should().Be(typeof(IReadOnlyList<GridPoint>));
        outcomeType.GetProperty(nameof(BuildingPlacementOutcome.CommittedCells))?.PropertyType.Should().Be(typeof(IReadOnlyList<GridPoint>));
        outcomeType.GetProperty(nameof(BuildingPlacementOutcome.RequiredCost))?.PropertyType.Should().Be(typeof(int));

        var acceptedFactory = outcomeType.GetMethod(
            "Accepted",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            new[] { typeof(IReadOnlyList<GridPoint>), typeof(int) },
            modifiers: null);
        acceptedFactory.Should().NotBeNull();
        acceptedFactory!.ReturnType.Should().Be(typeof(BuildingPlacementOutcome));

        var rejectedFactory = outcomeType.GetMethod(
            "Rejected",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            new[] { typeof(string), typeof(IReadOnlyList<GridPoint>), typeof(int) },
            modifiers: null);
        rejectedFactory.Should().NotBeNull();
        rejectedFactory!.ReturnType.Should().Be(typeof(BuildingPlacementOutcome));

        var stateType = typeof(BuildingPlacementState);
        stateType.GetProperty(nameof(BuildingPlacementState.Resources))?.PropertyType.Should().Be(typeof(int));
        stateType.GetProperty(nameof(BuildingPlacementState.Grid))?.PropertyType.Name.Should().Be("GridOccupancyTracker");
        stateType.GetProperty(nameof(BuildingPlacementState.Placements))?.PropertyType.Should().Be(typeof(List<BuildingPlacementRecord>));
    }

    // ACC:T61.5
    [Fact]
    public void ShouldContainDedicatedLegalityDecisionContractInCore_WhenEnforcingBoundary()
    {
        var catalogCtor = typeof(BuildingPlacementService).GetConstructor(new[] { typeof(IReadOnlyDictionary<string, Building>) });
        catalogCtor
            .Should().NotBeNull("the placement service should keep its catalog-injection seam");

        typeof(BuildingPlacementService).GetMethod("Preview", new[] { typeof(string), typeof(GridPoint) })
            .Should().NotBeNull("Core must keep a dedicated preview contract for legality feedback");
        typeof(BuildingPlacementService).GetMethod("TryPlace", new[] { typeof(string), typeof(GridPoint), typeof(BuildingPlacementState) })
            .Should().NotBeNull("Core must keep the string-based placement decision entrypoint");
        typeof(BuildingPlacementService).GetMethod("TryPlace", new[] { typeof(Building), typeof(GridPoint), typeof(BuildingPlacementState) })
            .Should().NotBeNull("Core must keep the building-object placement decision entrypoint");

        typeof(BuildingPlacementOutcome).GetMethod("Accepted", BindingFlags.Public | BindingFlags.Static)
            .Should().NotBeNull("Core must keep an accepted outcome factory");
        typeof(BuildingPlacementOutcome).GetMethod("Rejected", BindingFlags.Public | BindingFlags.Static)
            .Should().NotBeNull("Core must keep a rejected outcome factory");

        var previewType = typeof(BuildingPlacementPreview);
        previewType.Should().NotBeNull();
        previewType.GetProperty(nameof(BuildingPlacementPreview.BuildingType))?.PropertyType.Should().Be(typeof(string));
        previewType.GetProperty(nameof(BuildingPlacementPreview.Cells))?.PropertyType.Should().Be(typeof(IReadOnlyList<GridPoint>));
    }

    // ACC:T60.5
    [Fact]
    public void ShouldConsumeCorePlacementResultsFromGodotBridge_WhenVerifyingLegalityBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var bridgePath = Path.Combine(repoRoot, CoreBridgePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(bridgePath).Should().BeTrue("Godot placement bridge must exist for layer-boundary verification.");
        var bridgeText = File.ReadAllText(bridgePath);

        bridgeText.Should().Contain("using Game.Core.Services.Building;");
        bridgeText.Should().Contain("private readonly BuildingPlacementService placementService = new();");
        bridgeText.Should().Contain("var outcome = placementService.TryPlace(");
        bridgeText.Should().Contain("if (!outcome.IsAccepted)");
        bridgeText.Should().NotContain("EvaluateLegality(");
        bridgeText.Should().NotContain("IsPlacementLegal(");
    }

    // ACC:T60.5
    [Fact]
    public void ShouldKeepDeterminismEvidenceBoundToTask60Anchors_WhenPlacementContextIsInactive()
    {
        var repoRoot = FindRepositoryRoot();
        var testPath = Path.Combine(repoRoot, OverlayDeterminismTestPath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(testPath).Should().BeTrue("Task 60 determinism test evidence must exist.");
        var text = File.ReadAllText(testPath);

        text.Should().Contain("# acceptance: ACC:T60.3");
        text.Should().Contain("func test_inactive_placement_context_renders_no_legality_overlay()");
        text.Should().Contain("controller.set_placement_context_active(false)");
        text.Should().Contain("controller.apply_legality_overlay({\"B1\": controller.LEGALITY_WALL})");
        text.Should().Contain("assert_that(before_snapshot[\"overlay_state\"]).is_equal(\"overlay_legal\")");
        text.Should().Contain("assert_that(after_snapshot[\"overlay_state\"]).is_equal(\"overlay_hidden\")");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Game.Core")) &&
                Directory.Exists(Path.Combine(current.FullName, "Game.Godot")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found from test base directory.");
    }
}
