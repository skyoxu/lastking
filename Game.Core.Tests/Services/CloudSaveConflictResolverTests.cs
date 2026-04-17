using Game.Core.Services;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class CloudSaveConflictResolverTests
{
    // ACC:T26.6
    [Fact]
    public void ShouldApplyLocalWithoutPrompt_WhenRevisionsAreEqual()
    {
        var resolver = new CloudSaveConflictResolver();
        var localSnapshot = new CloudSaveSnapshot("rev-2", "local-data");
        var cloudSnapshot = new CloudSaveSnapshot("rev-2", "cloud-data");

        var result = resolver.Resolve(localSnapshot, cloudSnapshot, CloudConflictChoice.None);

        result.RequiresUserDecision.Should().BeFalse();
        result.AppliedLocalForThisOperation.Should().BeTrue();
        result.AppliedCloudForThisOperation.Should().BeFalse();
        result.CloudOverwriteScheduled.Should().BeFalse();
    }

    // ACC:T26.6
    [Fact]
    public void ShouldBlockAutomaticOverwriteAndRequireExplicitPrompt_WhenVersionsConflictAndDecisionIsMissing()
    {
        var resolver = new CloudSaveConflictResolver();
        var localSnapshot = new CloudSaveSnapshot("local-rev-2", "local-data");
        var cloudSnapshot = new CloudSaveSnapshot("cloud-rev-5", "cloud-data");

        var result = resolver.Resolve(localSnapshot, cloudSnapshot, CloudConflictChoice.None);

        result.RequiresUserDecision.Should().BeTrue();
        result.AppliedLocalForThisOperation.Should().BeFalse();
        result.AppliedCloudForThisOperation.Should().BeFalse();
        result.CloudOverwriteScheduled.Should().BeFalse();
    }

    // ACC:T26.11
    [Fact]
    public void ShouldApplyLocalDataForCurrentOperationWithoutAutomaticCloudOverwrite_WhenUserChoosesLocal()
    {
        var resolver = new CloudSaveConflictResolver();
        var localSnapshot = new CloudSaveSnapshot("local-rev-2", "local-data");
        var cloudSnapshot = new CloudSaveSnapshot("cloud-rev-5", "cloud-data");

        var result = resolver.Resolve(localSnapshot, cloudSnapshot, CloudConflictChoice.Local);

        result.RequiresUserDecision.Should().BeFalse();
        result.AppliedLocalForThisOperation.Should().BeTrue();
        result.AppliedCloudForThisOperation.Should().BeFalse();
        result.CloudOverwriteScheduled.Should().BeFalse();
    }

    [Fact]
    public void ShouldApplyCloudDataForCurrentOperation_WhenUserChoosesCloud()
    {
        var resolver = new CloudSaveConflictResolver();
        var localSnapshot = new CloudSaveSnapshot("local-rev-2", "local-data");
        var cloudSnapshot = new CloudSaveSnapshot("cloud-rev-5", "cloud-data");

        var result = resolver.Resolve(localSnapshot, cloudSnapshot, CloudConflictChoice.Cloud);

        result.RequiresUserDecision.Should().BeFalse();
        result.AppliedLocalForThisOperation.Should().BeFalse();
        result.AppliedCloudForThisOperation.Should().BeTrue();
        result.CloudOverwriteScheduled.Should().BeFalse();
    }

}
