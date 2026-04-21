using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class PerformanceSummarySchemaTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();
    private static readonly string FixtureRoot = Path.Combine(RepoRoot, "Game.Core.Tests", "Fixtures", "Performance");

    // ACC:T30.14
    [Fact]
    public void ShouldContainAllRequiredIntegerFields_WhenGateSummaryArtifactIsEvaluated()
    {
        var sut = new PerformanceSummarySchemaService();
        var artifactPath = GetFixturePath("task30-gate-summary.valid.json");

        var evaluation = sut.EvaluateFile(artifactPath);

        evaluation.MissingFields.Should().BeEmpty();
        evaluation.NonIntegerFields.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReportMissingFields_WhenGateSummaryArtifactOmitsRequiredPerformanceMetrics()
    {
        var sut = new PerformanceSummarySchemaService();
        var artifactPath = GetFixturePath("task30-gate-summary.missing-post-avg.json");

        var evaluation = sut.EvaluateFile(artifactPath);

        evaluation.MissingFields.Should().Contain("post_avg_fps");
        evaluation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ShouldRejectNonIntegerFields_WhenGateSummaryArtifactUsesNonIntegerThresholdValues()
    {
        var sut = new PerformanceSummarySchemaService();
        var artifactPath = GetFixturePath("task30-gate-summary.non-integer.json");

        var evaluation = sut.EvaluateFile(artifactPath);

        evaluation.NonIntegerFields.Should().Contain("baseline_1pct_low_fps");
        evaluation.NonIntegerFields.Should().Contain("applied_variance_window_percent");
        evaluation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ShouldMarkSchemaAsValid_WhenGateSummaryArtifactContainsAllRequiredFields()
    {
        var sut = new PerformanceSummarySchemaService();
        var artifactPath = GetFixturePath("task30-gate-summary.valid-alt.json");

        var evaluation = sut.EvaluateFile(artifactPath);

        evaluation.IsValid.Should().BeTrue();
        evaluation.MissingFields.Should().BeEmpty();
        evaluation.NonIntegerFields.Should().BeEmpty();
    }

    [Fact]
    public void ShouldThrowJsonException_WhenArtifactJsonIsMalformed()
    {
        var sut = new PerformanceSummarySchemaService();

        Action action = () => sut.EvaluateJson("{\"baseline_1pct_low_fps\": 45");

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void ShouldReportRequiredFieldsMissing_WhenArtifactRootIsNotAnObject()
    {
        var sut = new PerformanceSummarySchemaService();

        var evaluation = sut.EvaluateJson("[1,2,3]");

        evaluation.IsValid.Should().BeFalse();
        evaluation.MissingFields.Should().Contain("baseline_1pct_low_fps");
        evaluation.MissingFields.Should().Contain("post_avg_fps");
    }

    [Fact]
    public void ShouldThrowFileNotFoundException_WhenArtifactFileDoesNotExist()
    {
        var sut = new PerformanceSummarySchemaService();
        var missingPath = Path.Combine(FixtureRoot, "task30-gate-summary.missing-file.json");

        Action action = () => sut.EvaluateFile(missingPath);

        action.Should().Throw<FileNotFoundException>();
    }

    private static string GetFixturePath(string fileName)
    {
        var path = Path.Combine(FixtureRoot, fileName);
        File.Exists(path).Should().BeTrue($"fixture must exist: {path}");
        return path;
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "LastKing.sln");
            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Cannot resolve repository root from AppContext.BaseDirectory.");
    }
}
