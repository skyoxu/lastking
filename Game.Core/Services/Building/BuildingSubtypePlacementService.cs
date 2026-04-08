using Game.Core.Domain.Building;
using Game.Core.Services;
using Game.Core.State.Building;

namespace Game.Core.Services.Building;

public sealed class BuildingSubtypePlacementService
{
    private readonly BuildingPlacementService placementService;
    private readonly ResourceManager? resourceManager;
    private readonly int residencePopulationCapDelta;

    public BuildingSubtypePlacementService(
        BuildingPlacementService? placementService = null,
        ResourceManager? resourceManager = null,
        int residencePopulationCapDelta = 6)
    {
        this.placementService = placementService ?? new BuildingPlacementService();
        this.resourceManager = resourceManager ?? ResourceManager.Current;
        this.residencePopulationCapDelta = residencePopulationCapDelta;
    }

    public BuildingPlacementOutcome TryPlace(string subtype, GridPoint origin, BuildingPlacementState state)
    {
        var outcome = placementService.TryPlace(subtype, origin, state);
        if (outcome.IsAccepted &&
            subtype == BuildingTypeIds.Residence &&
            residencePopulationCapDelta > 0 &&
            resourceManager is not null)
        {
            resourceManager.TryAdd(0, 0, residencePopulationCapDelta, "residence-placement");
        }

        return outcome;
    }
}
