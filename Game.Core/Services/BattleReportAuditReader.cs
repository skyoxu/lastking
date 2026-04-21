using System;

namespace Game.Core.Services;

public sealed class BattleReportAuditReader
{
    public string GetConfigMetadataForAudit(BattleReportPayload report)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        if (report.Metadata.TryGetValue("config_hash", out var hashValue) && !string.IsNullOrWhiteSpace(hashValue))
        {
            return hashValue;
        }

        if (report.Metadata.TryGetValue("config_version", out var versionValue) && !string.IsNullOrWhiteSpace(versionValue))
        {
            return versionValue;
        }

        throw new InvalidOperationException("Audit retrieval requires config_hash or config_version metadata.");
    }
}
