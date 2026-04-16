using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class UIFeedbackMessageResolverTests
{
    // ACC:T24.17
    [Fact]
    public void ShouldFailContract_WhenAnySupportedReasonCodeLacksMappedMessageKey()
    {
        var supportedReasonCodes = new[]
        {
            "invalid_target",
            "insufficient_resources",
            "cooldown_active"
        };

        var resolver = new UIFeedbackMessageResolver(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invalid_target"] = "ui.invalid_action.invalid_target",
            ["insufficient_resources"] = "ui.invalid_action.insufficient_resources",
            ["cooldown_active"] = "ui.invalid_action.cooldown_active",
        });

        var missingReasonCodes = supportedReasonCodes
            .Where(reasonCode => !resolver.TryResolveMessageKey(reasonCode, out _))
            .ToArray();

        missingReasonCodes.Should().BeEmpty(
            "every supported reason code must map to an i18n message key");
    }

    [Fact]
    public void ShouldReturnFalse_WhenReasonCodeIsUnsupported()
    {
        var resolver = UIFeedbackMessageResolver.CreateDefault();

        var resolved = resolver.TryResolveMessageKey("unknown_reason_code", out var messageKey);

        resolved.Should().BeFalse("unsupported reason codes must not produce a feedback key");
        messageKey.Should().BeNull();
    }

    [Fact]
    public void ShouldUseUiInvalidActionPrefix_WhenReasonCodeIsSupported()
    {
        var resolver = UIFeedbackMessageResolver.CreateDefault();

        var resolved = resolver.TryResolveMessageKey("invalid_target", out var messageKey);

        resolved.Should().BeTrue();
        messageKey.Should().StartWith("ui.invalid_action.");
    }

}
