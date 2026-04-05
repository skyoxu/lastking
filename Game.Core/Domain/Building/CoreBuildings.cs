namespace Game.Core.Domain.Building;

public sealed class CastleBuilding : Building
{
    public CastleBuilding()
        : base(
            BuildingTypeIds.Castle,
            level: 1,
            footprintSize: 4,
            hp: 1000,
            cost: 120,
            footprintOffsets:
            [
                new GridPoint(0, 0),
                new GridPoint(1, 0),
                new GridPoint(0, 1),
                new GridPoint(1, 1),
            ])
    {
    }
}

public sealed class ResidenceBuilding : Building
{
    public ResidenceBuilding()
        : base(
            BuildingTypeIds.Residence,
            level: 1,
            footprintSize: 1,
            hp: 150,
            cost: 30,
            footprintOffsets:
            [
                new GridPoint(0, 0),
            ])
    {
    }
}

public sealed class MineBuilding : Building
{
    public MineBuilding()
        : base(
            BuildingTypeIds.Mine,
            level: 1,
            footprintSize: 2,
            hp: 220,
            cost: 45,
            footprintOffsets:
            [
                new GridPoint(0, 0),
                new GridPoint(1, 0),
            ])
    {
    }
}

public sealed class BarracksBuilding : Building
{
    public BarracksBuilding()
        : base(
            BuildingTypeIds.Barracks,
            level: 1,
            footprintSize: 2,
            hp: 300,
            cost: 80,
            footprintOffsets:
            [
                new GridPoint(0, 0),
                new GridPoint(0, 1),
            ])
    {
    }
}

public sealed class MgTowerBuilding : Building
{
    public MgTowerBuilding()
        : base(
            BuildingTypeIds.MgTower,
            level: 1,
            footprintSize: 1,
            hp: 260,
            cost: 60,
            footprintOffsets:
            [
                new GridPoint(0, 0),
            ])
    {
    }
}

public sealed class WallBuilding : Building
{
    public WallBuilding()
        : base(
            BuildingTypeIds.Wall,
            level: 1,
            footprintSize: 1,
            hp: 200,
            cost: 5,
            footprintOffsets:
            [
                new GridPoint(0, 0),
            ])
    {
    }
}

public sealed class MineTrapBuilding : Building
{
    public MineTrapBuilding()
        : base(
            BuildingTypeIds.MineTrap,
            level: 1,
            footprintSize: 1,
            hp: 60,
            cost: 20,
            footprintOffsets:
            [
                new GridPoint(0, 0),
            ])
    {
    }
}
