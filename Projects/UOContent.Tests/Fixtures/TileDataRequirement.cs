using Xunit;

namespace Server.Tests;

/// <summary>
/// Shared guard for tests that require the copyrighted UO client tile/map data (tiledata.mul and
/// friends), which is absent on CI. Call <see cref="SkipIfMissing"/> as the first statement of a
/// <c>[SkippableFact]</c>/<c>[SkippableTheory]</c> so the test is skipped — not failed — when the
/// data was not loaded. See <see cref="TestServerInitializer.TileDataLoaded"/>.
/// </summary>
internal static class TileDataRequirement
{
    public static void SkipIfMissing() =>
        Skip.If(
            !TestServerInitializer.TileDataLoaded,
            "Requires UO client tile data (tiledata.mul); absent on CI."
        );
}
