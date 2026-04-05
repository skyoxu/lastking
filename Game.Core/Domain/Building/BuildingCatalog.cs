using System;
using System.Collections.Generic;

namespace Game.Core.Domain.Building;

public static class BuildingCatalog
{
    private static readonly IReadOnlyDictionary<string, Building> Catalog =
        new Dictionary<string, Building>(StringComparer.Ordinal)
        {
            [BuildingTypeIds.Castle] = new CastleBuilding(),
            [BuildingTypeIds.Residence] = new ResidenceBuilding(),
            [BuildingTypeIds.Mine] = new MineBuilding(),
            [BuildingTypeIds.Barracks] = new BarracksBuilding(),
            [BuildingTypeIds.MgTower] = new MgTowerBuilding(),
            [BuildingTypeIds.Wall] = new WallBuilding(),
            [BuildingTypeIds.MineTrap] = new MineTrapBuilding(),
        };

    public static IReadOnlyDictionary<string, Building> GetAll()
    {
        return Catalog;
    }

    public static bool TryGet(string buildingType, out Building building)
    {
        return Catalog.TryGetValue(buildingType, out building!);
    }
}
