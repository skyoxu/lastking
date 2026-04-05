using Game.Core.Domain.Building;
using Game.Core.State.Building;

namespace Game.Core.Services.Building;

public sealed class BuildingSubtypePlacementService
{
    private readonly BuildingPlacementService placementService;

    public BuildingSubtypePlacementService(BuildingPlacementService? placementService = null)
    {
        this.placementService = placementService ?? new BuildingPlacementService();
    }

    public BuildingPlacementOutcome TryPlace(string subtype, GridPoint origin, BuildingPlacementState state)
    {
        return placementService.TryPlace(subtype, origin, state);
    }
}
