using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public class Task60AcceptanceRefsConsistencyTests
{
    private static readonly string[] TaskFiles =
    {
        ".taskmaster/tasks/tasks_back.json",
        ".taskmaster/tasks/tasks_gameplay.json",
    };

    // ACC:T60.10
    [Fact]
    public void ShouldKeepTask60AcceptanceRefsSynchronized_WhenValidatingRealTaskArtifacts()
    {
        var expectedRefs = LoadTask60AcceptanceRefs(TaskFiles[0]);
        expectedRefs.Should().NotBeEmpty();

        foreach (var path in TaskFiles)
        {
            var refs = LoadTask60AcceptanceRefs(path);
            refs.Should().Equal(expectedRefs, $"task 60 refs should stay synchronized in {path}");
            refs.Should().OnlyHaveUniqueItems($"task 60 refs should not duplicate entries in {path}");
            refs.All(ArtifactExists).Should().BeTrue($"all task 60 refs must resolve to existing artifacts in {path}");
        }
    }

    private static IReadOnlyList<string> LoadTask60AcceptanceRefs(string taskFilePath)
    {
        var root = ResolveRepositoryRoot();
        var fullPath = Path.Combine(root, taskFilePath);
        File.Exists(fullPath).Should().BeTrue($"task file must exist: {taskFilePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(fullPath));
        var task = doc.RootElement
            .EnumerateArray()
            .FirstOrDefault(x => x.TryGetProperty("taskmaster_id", out var id) && id.GetInt32() == 60);

        task.ValueKind.Should().NotBe(JsonValueKind.Undefined, $"taskmaster_id 60 must exist in {taskFilePath}");
        task.TryGetProperty("acceptance", out var acceptance).Should().BeTrue("task 60 must declare acceptance items");
        acceptance.ValueKind.Should().Be(JsonValueKind.Array);

        var refs = new List<string>();
        foreach (var item in acceptance.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String))
        {
            var text = item.GetString() ?? string.Empty;
            var marker = text.IndexOf("Refs:", StringComparison.Ordinal);
            if (marker < 0)
            {
                continue;
            }

            var suffix = text[(marker + 5)..].Trim();
            if (suffix.Length == 0)
            {
                continue;
            }

            refs.AddRange(suffix.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return refs
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static bool ArtifactExists(string relativePath)
    {
        var root = ResolveRepositoryRoot();
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(fullPath);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Lastking.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot resolve repository root from test base directory.");
    }
}
