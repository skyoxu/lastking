namespace Game.Core.Contracts;

using System.Text.Json;

/// <summary>
/// Generic domain event envelope used by the core event bus.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public record DomainEvent(
    string Type,
    string Source,
    [property: Obsolete("Legacy payload field. Use DataElement, DataJson, or DomainEvent.Create<TPayload>.")]
    object? Data,
    DateTime Timestamp,
    string Id,
    string SpecVersion = "1.0",
    string DataContentType = "application/json",
    int DataSchemaVersion = 1
)
{
    /// <summary>
    /// Canonical JSON payload for transport and serialization boundaries.
    /// </summary>
    public JsonElement? DataElement { get; init; } = ToJsonElement(Data);

    /// <summary>
    /// Canonical JSON payload string for logging and adapter emission.
    /// </summary>
    public string DataJson => DataElement?.GetRawText() ?? "{}";

    /// <summary>
    /// Creates an event using typed payload and canonical JSON serialization.
    /// </summary>
    public static DomainEvent Create<TPayload>(
        string type,
        string source,
        TPayload payload,
        DateTime timestamp,
        string id,
        string specVersion = "1.0",
        string dataContentType = "application/json",
        int dataSchemaVersion = 1
    )
    {
        return new DomainEvent(
            Type: type,
            Source: source,
            Data: null,
            Timestamp: timestamp,
            Id: id,
            SpecVersion: specVersion,
            DataContentType: dataContentType,
            DataSchemaVersion: dataSchemaVersion
        )
        {
            DataElement = ToJsonElement(payload),
        };
    }

    /// <summary>
    /// Deserializes canonical JSON payload to a strong type.
    /// </summary>
    public TPayload? DeserializeData<TPayload>()
    {
        if (!DataElement.HasValue)
        {
            return default;
        }

        return DataElement.Value.Deserialize<TPayload>();
    }

    private static JsonElement? ToJsonElement(object? payload)
    {
        if (payload is null)
        {
            return null;
        }

        if (payload is JsonElement element)
        {
            return element.Clone();
        }

        if (payload is string jsonString)
        {
            var trimmed = jsonString.Trim();
            if (trimmed.Length == 0)
            {
                return JsonSerializer.SerializeToElement(new { });
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return JsonSerializer.SerializeToElement(jsonString);
            }
        }

        return JsonSerializer.SerializeToElement(payload, payload.GetType());
    }
}

