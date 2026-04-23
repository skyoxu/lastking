using System;

namespace Game.Core.Services;

public sealed class BattleReportAuditValidator
{
    public void EnsureAuditComplete(BattleReportPayload report)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        var hasHash = report.Metadata.TryGetValue("config_hash", out var hashValue) &&
                      !string.IsNullOrWhiteSpace(hashValue);
        var hasVersion = report.Metadata.TryGetValue("config_version", out var versionValue) &&
                         !string.IsNullOrWhiteSpace(versionValue);

        if (!hasHash && !hasVersion)
        {
            throw new InvalidOperationException("Battle report metadata must contain config_hash or config_version.");
        }
    }
}
