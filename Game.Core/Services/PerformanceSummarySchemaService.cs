using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Game.Core.Services;

public sealed class PerformanceSummarySchemaService
{
    private static readonly string[] RequiredIntegerFields =
    {
        "baseline_1pct_low_fps",
        "baseline_avg_fps",
        "post_1pct_low_fps",
        "post_avg_fps",
        "applied_variance_window_percent",
        "threshold_1pct_low_fps",
        "threshold_avg_fps"
    };

    public PerformanceSummarySchemaEvaluation EvaluateJson(string artifactJson)
    {
        ArgumentNullException.ThrowIfNull(artifactJson);

        using var document = JsonDocument.Parse(artifactJson);
        return EvaluateRoot(document.RootElement);
    }

    public PerformanceSummarySchemaEvaluation EvaluateFile(string artifactPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);
        var artifactJson = File.ReadAllText(artifactPath);
        return EvaluateJson(artifactJson);
    }

    public PerformanceSummarySchemaEvaluation EvaluateRoot(JsonElement root)
    {
        var missingFields = new List<string>();
        var nonIntegerFields = new List<string>();
        if (root.ValueKind != JsonValueKind.Object)
        {
            missingFields.AddRange(RequiredIntegerFields);
            return new PerformanceSummarySchemaEvaluation(missingFields, nonIntegerFields);
        }

        foreach (var fieldName in RequiredIntegerFields)
        {
            if (!root.TryGetProperty(fieldName, out var value))
            {
                missingFields.Add(fieldName);
                continue;
            }

            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out _))
            {
                nonIntegerFields.Add(fieldName);
            }
        }

        return new PerformanceSummarySchemaEvaluation(missingFields, nonIntegerFields);
    }
}
