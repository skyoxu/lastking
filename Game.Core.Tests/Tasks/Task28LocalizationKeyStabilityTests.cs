using System.Collections.Generic;
using System;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task28LocalizationKeyStabilityTests
{
    // ACC:T28.13
    [Fact]
    public void ShouldFailAudit_WhenPublishedUiKeyIsRenamedWithoutDeclaration()
    {
        var publishedKeys = new Dictionary<string, string>
        {
            ["ui.main_menu.start"] = "Start",
            ["ui.main_menu.settings"] = "Settings"
        };
        var currentKeys = new Dictionary<string, string>
        {
            ["ui.main_menu.begin"] = "Start",
            ["ui.main_menu.settings"] = "Settings"
        };
        var declaredRenames = new Dictionary<string, string>();

        var auditPassed = RunPublishedKeyAudit(publishedKeys, currentKeys, declaredRenames);

        auditPassed.Should().BeFalse("renaming a published UI key without declaration must fail the audit");
    }

    // ACC:T28.9
    [Fact]
    public void ShouldFailAudit_WhenPublishedUiKeyIsRemoved()
    {
        var publishedKeys = new Dictionary<string, string>
        {
            ["ui.main_menu.start"] = "Start",
            ["ui.main_menu.settings"] = "Settings"
        };
        var currentKeys = new Dictionary<string, string>
        {
            ["ui.main_menu.start"] = "Start"
        };
        var declaredRenames = new Dictionary<string, string>();

        var auditPassed = RunPublishedKeyAudit(publishedKeys, currentKeys, declaredRenames);

        auditPassed.Should().BeFalse("removing any published UI key must fail and cannot pass silently");
    }

    [Fact]
    public void ShouldPassAudit_WhenPublishedUiKeysAreUnchanged()
    {
        var publishedKeys = new Dictionary<string, string>
        {
            ["ui.main_menu.start"] = "Start",
            ["ui.main_menu.settings"] = "Settings"
        };
        var currentKeys = new Dictionary<string, string>
        {
            ["ui.main_menu.start"] = "Start",
            ["ui.main_menu.settings"] = "Settings"
        };
        var declaredRenames = new Dictionary<string, string>();

        var auditPassed = RunPublishedKeyAudit(publishedKeys, currentKeys, declaredRenames);

        auditPassed.Should().BeTrue("stable published keys should keep the regression audit green");
    }

    private static bool RunPublishedKeyAudit(
        IReadOnlyDictionary<string, string> publishedKeys,
        IReadOnlyDictionary<string, string> currentKeys,
        IReadOnlyDictionary<string, string> declaredRenames)
    {
        var normalizedRenames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in declaredRenames)
        {
            normalizedRenames[pair.Key] = pair.Value;
        }
        foreach (var publishedKey in publishedKeys.Keys)
        {
            if (currentKeys.ContainsKey(publishedKey))
            {
                continue;
            }

            if (normalizedRenames.TryGetValue(publishedKey, out var renamedTo)
                && !string.IsNullOrWhiteSpace(renamedTo)
                && currentKeys.ContainsKey(renamedTo))
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
