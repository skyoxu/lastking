using System.Collections.ObjectModel;

namespace Game.Core.Services;

public sealed class UIFeedbackMessageResolver
{
    private readonly IReadOnlyDictionary<string, string> reasonCodeToMessageKey;

    public UIFeedbackMessageResolver(IReadOnlyDictionary<string, string> reasonCodeToMessageKey)
    {
        this.reasonCodeToMessageKey = reasonCodeToMessageKey ?? throw new ArgumentNullException(nameof(reasonCodeToMessageKey));
    }

    public static IReadOnlyList<string> SupportedReasonCodes { get; } =
        new ReadOnlyCollection<string>(
            new[]
            {
                "invalid_target",
                "insufficient_resources",
                "cooldown_active",
            });

    public static UIFeedbackMessageResolver CreateDefault()
    {
        return new UIFeedbackMessageResolver(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invalid_target"] = "ui.invalid_action.invalid_target",
            ["insufficient_resources"] = "ui.invalid_action.insufficient_resources",
            ["cooldown_active"] = "ui.invalid_action.cooldown_active",
        });
    }

    public bool TryResolveMessageKey(string reasonCode, out string? messageKey)
    {
        if (!string.IsNullOrWhiteSpace(reasonCode) &&
            reasonCodeToMessageKey.TryGetValue(reasonCode, out var mapped) &&
            !string.IsNullOrWhiteSpace(mapped))
        {
            messageKey = mapped;
            return true;
        }

        messageKey = null;
        return false;
    }

    public static IReadOnlyList<string> FindMissingReasonCodes(
        IEnumerable<string> supportedReasonCodes,
        IReadOnlyDictionary<string, string> mappings)
    {
        if (supportedReasonCodes is null)
        {
            throw new ArgumentNullException(nameof(supportedReasonCodes));
        }

        if (mappings is null)
        {
            throw new ArgumentNullException(nameof(mappings));
        }

        return supportedReasonCodes
            .Where(reasonCode => string.IsNullOrWhiteSpace(reasonCode) || !mappings.ContainsKey(reasonCode))
            .ToArray();
    }

    public static IReadOnlyList<string> FindInvalidMappedReasonCodes(IReadOnlyDictionary<string, string> mappings)
    {
        if (mappings is null)
        {
            throw new ArgumentNullException(nameof(mappings));
        }

        return mappings
            .Where(pair => string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => pair.Key)
            .ToArray();
    }
}
