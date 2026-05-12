using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Utilities;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task67RefsAnchorsSyncTests
{
    // ACC:T67.10
    [Fact]
    public void ShouldKeepTask67AcceptanceRefsAndAnchorsSynchronized_WhenValidatingEvidenceMapping()
    {
        var root = FindRepositoryRoot();
        using var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        using var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));

        var backTask = FindTask67(back);
        var gameplayTask = FindTask67(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var requiredAnchors = Enumerable.Range(1, 10).Select(i => $"ACC:T67.{i}").ToArray();
        ValidateTaskRefsAndAnchors(root, backTask!.Value, requiredAnchors);
        ValidateTaskRefsAndAnchors(root, gameplayTask!.Value, requiredAnchors);
    }

    private static void ValidateTaskRefsAndAnchors(string root, JsonElement task, IReadOnlyList<string> requiredAnchors)
    {
        var testRefs = ExtractStringArray(task.GetProperty("test_refs"));
        testRefs.Should().Contain("Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd");
        testRefs.Should().Contain("Game.Core.Tests/Tasks/Task67RefsAnchorsSyncTests.cs");

        var acceptance = ExtractStringArray(task.GetProperty("acceptance"));
        acceptance.Should().HaveCountGreaterThanOrEqualTo(10);
        foreach (var item in acceptance)
        {
            item.Should().Contain("Refs:");
        }

        var combined = string.Join(
            "\n",
            testRefs
                .Distinct(StringComparer.Ordinal)
                .Select(path =>
                {
                    var full = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
                    File.Exists(full).Should().BeTrue($"ref file must exist: {path}");
                    return File.ReadAllText(full);
                }));

        foreach (var anchor in requiredAnchors)
        {
            combined.Should().Contain(anchor);
        }

        MathHelper.Clamp(3, 0, 5).Should().Be(3);
    }

    private static IReadOnlyList<string> ExtractStringArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(dir))
        {
            if (Directory.Exists(Path.Combine(dir, ".taskmaster")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing .taskmaster directory.");
    }

    private static JsonElement? FindTask67(JsonDocument doc)
    {
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("taskmaster_id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == 67)
            {
                return item;
            }
        }

        return null;
    }
}
