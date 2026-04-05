using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Domain.Building;

public abstract class Building
{
    protected Building(
        string type,
        int level,
        int footprintSize,
        int hp,
        int cost,
        IReadOnlyList<GridPoint> footprintOffsets)
    {
        Type = type;
        Level = level;
        FootprintSize = footprintSize;
        Hp = hp;
        Cost = cost;
        FootprintOffsets = footprintOffsets;
    }

    public string Type { get; }

    public int Level { get; }

    public int FootprintSize { get; }

    public int Hp { get; }

    public int Cost { get; }

    public IReadOnlyList<GridPoint> FootprintOffsets { get; }

    public IReadOnlyList<GridPoint> ResolveFootprint(GridPoint origin)
    {
        return FootprintOffsets
            .Select(offset => origin + offset)
            .ToArray();
    }
}
