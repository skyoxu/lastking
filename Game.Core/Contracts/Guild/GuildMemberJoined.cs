namespace Game.Core.Contracts.Guild;

/// <summary>
/// Domain event: core.guild.member.joined
/// Description: Emitted when a user joins a guild.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// </remarks>
public sealed record GuildMemberJoined(
    string UserId,
    string GuildId,
    System.DateTimeOffset JoinedAt,
    string Role
)
{
    public const string EventType = EventTypes.GuildMemberJoined;
}

