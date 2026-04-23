namespace Game.Core.Services;

/// <summary>
/// Strongly typed gameplay balance snapshot consumed by runtime systems.
/// </summary>
public sealed partial record BalanceSnapshot(
    int DaySeconds,
    int NightSeconds,
    int Day1Budget,
    decimal DailyGrowth,
    string EliteChannel,
    string BossChannel,
    int SpawnCadenceSeconds,
    int BossCount,
    int CastleStartHp,
    int? RegularSpawnCadenceSeconds,
    int? BossSpawnCadenceSeconds,
    ChannelRule? EliteRule,
    ChannelRule? BossRule
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
        RegularSpawnCadenceSeconds: null,
        BossSpawnCadenceSeconds: null,
        BossCount: 2,
        CastleStartHp: 100,
        EliteRule: new ChannelRule(120, 1.2m, 8, 20),
        BossRule: new ChannelRule(300, 1.2m, 3, 100));
}
