namespace Game.Core.Domain.Building;

public readonly record struct GridPoint(int X, int Y)
{
    public static GridPoint operator +(GridPoint left, GridPoint right)
    {
        return new GridPoint(left.X + right.X, left.Y + right.Y);
    }
}
