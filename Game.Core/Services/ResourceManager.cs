using System.Text.Json;
using Game.Core.Contracts;

namespace Game.Core.Services;

public sealed class ResourceManager
{
    public const int InitialGold = 800;
    public const int InitialIron = 150;
    public const int InitialPopulationCap = 50;

    private readonly IEventBus? _eventBus;
    private readonly string _runId;
    private readonly int _dayNumber;
    private readonly string _eventSource;

    public ResourceManager() : this(eventBus: null, runId: "default-run", dayNumber: 1, eventSource: nameof(ResourceManager))
    {
    }

    public ResourceManager(IEventBus? eventBus) : this(eventBus: eventBus, runId: "default-run", dayNumber: 1, eventSource: nameof(ResourceManager))
    {
    }

    public ResourceManager(IEventBus? eventBus, string runId, int dayNumber) : this(eventBus: eventBus, runId: runId, dayNumber: dayNumber, eventSource: nameof(ResourceManager))
    {
    }

    public ResourceManager(IEventBus? eventBus, string runId, int dayNumber, string eventSource = nameof(ResourceManager))
    {
        _eventBus = eventBus;
        _runId = runId;
        _dayNumber = dayNumber;
        _eventSource = eventSource;

        Gold = InitialGold;
        Iron = InitialIron;
        PopulationCap = InitialPopulationCap;
    }

    public int Gold { get; private set; }
    public int Iron { get; private set; }
    public int PopulationCap { get; private set; }

    public ResourceSnapshot GetSnapshot() => new(Gold, Iron, PopulationCap);

    public string ExportSnapshot()
    {
        var payload = new
        {
            gold = Gold,
            iron = Iron,
            populationCap = PopulationCap,
        };
        return JsonSerializer.Serialize(payload);
    }

    public ResourceMutationResult TryAdd(int goldDelta, int ironDelta, int populationCapDelta, string reason = "add")
    {
        if (goldDelta < 0 || ironDelta < 0 || populationCapDelta < 0)
        {
            return ResourceMutationResult.Fail("negative input is not allowed for TryAdd", GetSnapshot());
        }

        return ApplyDelta(goldDelta, ironDelta, populationCapDelta, reason);
    }

    public ResourceMutationResult TrySubtract(int goldDelta, int ironDelta, int populationCapDelta, string reason = "subtract")
    {
        if (goldDelta < 0 || ironDelta < 0 || populationCapDelta < 0)
        {
            return ResourceMutationResult.Fail("negative input is not allowed for TrySubtract", GetSnapshot());
        }

        return ApplyDelta(-goldDelta, -ironDelta, -populationCapDelta, reason);
    }

    public ResourceMutationResult TrySpend(int goldCost, int ironCost, string reason = "spend")
    {
        if (goldCost < 0 || ironCost < 0)
        {
            return ResourceMutationResult.Fail("negative input is not allowed for TrySpend", GetSnapshot());
        }

        return ApplyDelta(-goldCost, -ironCost, 0, reason);
    }

    public ResourceImportResult TryImportSnapshot(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return new ResourceImportResult(false, "snapshot payload is empty");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(snapshotJson);
        }
        catch (JsonException ex)
        {
            return new ResourceImportResult(false, $"invalid snapshot json: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!TryReadRequiredInt(root, "gold", out var nextGold, out var goldError))
            {
                return new ResourceImportResult(false, goldError);
            }

            if (!TryReadRequiredInt(root, "iron", out var nextIron, out var ironError))
            {
                return new ResourceImportResult(false, ironError);
            }

            if (!TryReadRequiredInt(root, "populationCap", out var nextPopulationCap, out var popError))
            {
                return new ResourceImportResult(false, popError);
            }

            if (nextGold < 0 || nextIron < 0 || nextPopulationCap < 0)
            {
                return new ResourceImportResult(false, "snapshot values cannot be negative");
            }

            var deltaGold = nextGold - Gold;
            var deltaIron = nextIron - Iron;
            var deltaPopulationCap = nextPopulationCap - PopulationCap;

            Gold = nextGold;
            Iron = nextIron;
            PopulationCap = nextPopulationCap;

            if (deltaGold != 0 || deltaIron != 0 || deltaPopulationCap != 0)
            {
                PublishChange(deltaGold, deltaIron, deltaPopulationCap, "snapshot-import");
            }

            return new ResourceImportResult(true, "accepted");
        }
    }

    private ResourceMutationResult ApplyDelta(int deltaGold, int deltaIron, int deltaPopulationCap, string reason)
    {
        if (deltaGold == 0 && deltaIron == 0 && deltaPopulationCap == 0)
        {
            return ResourceMutationResult.Ok(GetSnapshot());
        }

        long nextGold;
        long nextIron;
        long nextPopulationCap;
        try
        {
            checked
            {
                nextGold = Gold + (long)deltaGold;
                nextIron = Iron + (long)deltaIron;
                nextPopulationCap = PopulationCap + (long)deltaPopulationCap;
            }
        }
        catch (OverflowException)
        {
            return ResourceMutationResult.Fail("mutation overflow", GetSnapshot());
        }

        if (nextGold < 0 || nextIron < 0 || nextPopulationCap < 0)
        {
            return ResourceMutationResult.Fail("mutation underflow", GetSnapshot());
        }

        if (nextGold > int.MaxValue || nextIron > int.MaxValue || nextPopulationCap > int.MaxValue)
        {
            return ResourceMutationResult.Fail("mutation overflow", GetSnapshot());
        }

        Gold = (int)nextGold;
        Iron = (int)nextIron;
        PopulationCap = (int)nextPopulationCap;
        PublishChange(deltaGold, deltaIron, deltaPopulationCap, reason);
        return ResourceMutationResult.Ok(GetSnapshot());
    }

    private void PublishChange(int deltaGold, int deltaIron, int deltaPopulationCap, string reason)
    {
        if (_eventBus is null)
        {
            return;
        }

        var changedAt = DateTimeOffset.UtcNow;
        var payload = new
        {
            runId = _runId,
            dayNumber = _dayNumber,
            gold = Gold,
            iron = Iron,
            populationCap = PopulationCap,
            delta = new
            {
                gold = deltaGold,
                iron = deltaIron,
                populationCap = deltaPopulationCap,
            },
            reason,
            changedAt,
        };

        _ = _eventBus.PublishAsync(DomainEvent.Create(
            type: EventTypes.LastkingResourcesChanged,
            source: _eventSource,
            payload: payload,
            timestamp: changedAt.UtcDateTime,
            id: Guid.NewGuid().ToString("N")));
    }

    private static bool TryReadRequiredInt(JsonElement root, string propertyName, out int value, out string error)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            value = default;
            error = $"missing required property '{propertyName}'";
            return false;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out value))
        {
            value = default;
            error = $"property '{propertyName}' must be an integer number";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

public readonly record struct ResourceSnapshot(int Gold, int Iron, int PopulationCap);

public readonly record struct ResourceMutationResult(bool Succeeded, string? FailureReason, ResourceSnapshot Snapshot)
{
    public static ResourceMutationResult Ok(ResourceSnapshot snapshot) => new(true, null, snapshot);

    public static ResourceMutationResult Fail(string failureReason, ResourceSnapshot snapshot) => new(false, failureReason, snapshot);
}

public readonly record struct ResourceImportResult(bool Accepted, string? FailureReason);
