using Xunit;

namespace Server.Tests;

[CollectionDefinition("Sequential UOContent Tests", DisableParallelization = true)]
public class UOContentFixture : ICollectionFixture<UOContentFixture>
{
    // All process-global initialization lives in TestServerInitializer and runs exactly once,
    // shared across every collection fixture. Tearing down global state here is intentionally
    // omitted: the world/serialization workers are initialized once and reused for the whole
    // test host, so there is nothing per-collection to dispose.
    public UOContentFixture() => TestServerInitializer.Initialize();
}
