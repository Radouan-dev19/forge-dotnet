using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestCollectionOrderer(
    "ForgeDotNet.IntegrationTests.DockerTestCollectionOrderer",
    "ForgeDotNet.IntegrationTests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ForgeDotNet.IntegrationTests;

public sealed class DockerTestCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(
        IEnumerable<ITestCollection> testCollections) => testCollections
        .OrderBy(Priority)
        .ThenBy(collection => collection.DisplayName, StringComparer.Ordinal);

    private static int Priority(ITestCollection collection) =>
        collection.DisplayName.Contains(
            EfDockerCodeRunnerTestGroup.CollectionName,
            StringComparison.Ordinal) ? 0 : 1;
}
