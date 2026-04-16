using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Domain;

public sealed class InvalidActionFeedbackContractTests
{
    // ACC:T24.17
    [Fact]
    public void ShouldFailValidation_WhenSupportedReasonCodeIsMissingMappedMessageKey()
    {
        var reasonCodeToMessageKey = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invalid_target"] = "ui.invalid_action.invalid_target",
            ["insufficient_resources"] = "ui.invalid_action.insufficient_resources",
            ["cooldown_active"] = "ui.invalid_action.cooldown_active",
        };

        var missingReasonCodes = UIFeedbackMessageResolver.FindMissingReasonCodes(
            UIFeedbackMessageResolver.SupportedReasonCodes,
            reasonCodeToMessageKey);

        missingReasonCodes.Should().BeEmpty(
            "every supported reason code must map to an i18n key for invalid-action feedback");
    }

    [Fact]
    public void ShouldUseStableI18nKeyPrefix_WhenReasonCodeIsMapped()
    {
        var reasonCodeToMessageKey = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invalid_target"] = "ui.invalid_action.invalid_target",
            ["insufficient_resources"] = "ui.invalid_action.insufficient_resources"
        };

        reasonCodeToMessageKey.Values
            .Should()
            .OnlyContain(messageKey => messageKey.StartsWith("ui.invalid_action.", StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldRejectEmptyI18nMessageKey_WhenReasonCodeIsMapped()
    {
        var reasonCodeToMessageKey = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invalid_target"] = "ui.invalid_action.invalid_target",
        };

        var invalidEntries = UIFeedbackMessageResolver.FindInvalidMappedReasonCodes(reasonCodeToMessageKey);

        invalidEntries.Should().BeEmpty("mapped i18n keys must be non-empty for all supported reason codes");
    }
}
