using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceGateArtifactContractServiceTests
{
    [Fact]
    public void IsAllowedArtifactPath_ShouldFail_WhenPathEscapesRepoRoot()
    {
        var sut = new PerformanceGateArtifactContractService();

        var allowed = sut.IsAllowedArtifactPath(@"F:\Lastking", @"..\outside.json");

        allowed.Should().BeFalse();
    }

    [Fact]
    public void IsAllowedArtifactPath_ShouldFail_WhenAbsolutePathIsProvided()
    {
        var sut = new PerformanceGateArtifactContractService();

        var allowed = sut.IsAllowedArtifactPath(@"F:\Lastking", @"F:\Lastking\logs\ci\task-30\perf-gate.json");

        allowed.Should().BeFalse();
    }

    [Fact]
    public void IsAllowedArtifactPath_ShouldPass_ForLogsCiAndLogsPerf()
    {
        var sut = new PerformanceGateArtifactContractService();

        var logsCiAllowed = sut.IsAllowedArtifactPath(@"F:\Lastking", @"logs/ci/task-30/perf-gate.json");
        var logsPerfAllowed = sut.IsAllowedArtifactPath(@"F:\Lastking", @"logs/perf/task-30/perf-gate.json");

        logsCiAllowed.Should().BeTrue();
        logsPerfAllowed.Should().BeTrue();
    }

    [Fact]
    public void IsArtifactContractValid_ShouldPass_WhenBothRunsAndRequiredFieldsExist()
    {
        var sut = new PerformanceGateArtifactContractService();
        const string artifactJson =
            """
            {
              "seed": 20260421,
              "runs": {
                "windows_headless": {
                  "baseline_threshold_fps": 60,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "pass",
                  "average_verdict": "pass"
                },
                "windows_playable": {
                  "baseline_threshold_fps": 60,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "pass",
                  "average_verdict": "pass"
                }
              }
            }
            """;

        var result = sut.IsArtifactContractValid(artifactJson);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsArtifactContractValid_ShouldFail_WhenWindowsPlayableRunIsMissing()
    {
        var sut = new PerformanceGateArtifactContractService();
        const string artifactJson =
            """
            {
              "seed": 20260421,
              "runs": {
                "windows_headless": {
                  "baseline_threshold_fps": 60,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "pass",
                  "average_verdict": "pass"
                }
              }
            }
            """;

        var result = sut.IsArtifactContractValid(artifactJson);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsArtifactContractValid_ShouldFail_WhenThresholdIsNotInteger()
    {
        var sut = new PerformanceGateArtifactContractService();
        const string artifactJson =
            """
            {
              "seed": 20260421,
              "runs": {
                "windows_headless": {
                  "baseline_threshold_fps": 60.5,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "pass",
                  "average_verdict": "pass"
                },
                "windows_playable": {
                  "baseline_threshold_fps": 60,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "pass",
                  "average_verdict": "pass"
                }
              }
            }
            """;

        var result = sut.IsArtifactContractValid(artifactJson);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsArtifactContractValid_ShouldFail_WhenVerdictFieldIsBlank()
    {
        var sut = new PerformanceGateArtifactContractService();
        const string artifactJson =
            """
            {
              "seed": 20260421,
              "runs": {
                "windows_headless": {
                  "baseline_threshold_fps": 60,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "",
                  "average_verdict": "pass"
                },
                "windows_playable": {
                  "baseline_threshold_fps": 60,
                  "low_1_percent_threshold_fps": 45,
                  "average_threshold_fps": 60,
                  "baseline_verdict": "pass",
                  "low_1_percent_verdict": "pass",
                  "average_verdict": "pass"
                }
              }
            }
            """;

        var result = sut.IsArtifactContractValid(artifactJson);

        result.Should().BeFalse();
    }
}
