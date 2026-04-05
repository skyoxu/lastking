namespace Game.Core.Domain.Building;

public static class BuildingPlacementReasonCodes
{
    public const string Accepted = "Accepted";
    public const string OutOfBounds = "OutOfBounds";
    public const string Blocked = "Blocked";
    public const string Occupied = "Occupied";
    public const string InsufficientResources = "InsufficientResources";
    public const string UnknownType = "UnknownType";
    public const string InvalidFootprint = "InvalidFootprint";
}
