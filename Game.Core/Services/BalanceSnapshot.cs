namespace Game.Core.Services;

/// <summary>
/// Strongly typed gameplay balance snapshot consumed by runtime systems.
/// </summary>
public sealed record BalanceSnapshot(
    int DaySeconds,
    int NightSeconds,
    int Day1Budget,
    decimal DailyGrowth,
    string EliteChannel,
    string BossChannel,
    int SpawnCadenceSeconds,
    int BossCount,
    int CastleStartHp
)
{
    public static BalanceSnapshot Default { get; } = new(
        DaySeconds: 240,
        NightSeconds: 120,
        Day1Budget: 50,
        DailyGrowth: 1.2m,
        EliteChannel: "elite",
        BossChannel: "boss",
        SpawnCadenceSeconds: 10,
        BossCount: 2,
        CastleStartHp: 100);
}
