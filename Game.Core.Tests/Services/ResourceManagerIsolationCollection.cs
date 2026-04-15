using Xunit;

namespace Game.Core.Tests.Services;

internal static class ResourceManagerIsolationCollection
{
    public const string Name = "ResourceManagerIsolation";
}

[CollectionDefinition(ResourceManagerIsolationCollection.Name, DisableParallelization = true)]
public sealed class ResourceManagerIsolationCollectionDefinition
{
}
