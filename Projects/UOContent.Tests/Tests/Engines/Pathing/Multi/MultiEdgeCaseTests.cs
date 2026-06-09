using Server.Engines.Pathing.Cache;
using Server.Items;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiEdgeCaseTests
{
    private const int MapId = 1; // Trammel
    private const int GuildHouseId = 0x74;

    // GuildHouse placement (mirrors MultiMaskSynthesisTests) — open Trammel ground.
    private const int HouseX = 1480;
    private const int HouseY = 1620;

    // Open water in the south-Britain bay (mirrors BoatPathTests).
    private const int BoatMultiId = 0x0; // SmallBoat North heading
    private const int WaterX = 1450;
    private const int WaterY = 1770;
    private const sbyte DeckZ = 0; // boat deck floor tiles stand at world Z 0 (not the water avgZ)

    /// <summary>
    /// Two overlapping GuildHouse multis whose footprints intersect. At the stacked cells
    /// <c>GetStaticAndMultiTiles</c> yields tiles from BOTH multis; the synthesizer must still
    /// agree with CheckMovement everywhere over the union footprint + halo.
    /// </summary>
    [Fact]
    public void OverlappingMultis_SynthesizerMatchesCheckMovement()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];

        map.GetAverageZ(HouseX, HouseY, out _, out var z, out _);
        var locA = new Point3D(HouseX, HouseY, (sbyte)z);
        // Origins 3 tiles apart on X so the GuildHouse footprints overlap.
        var locB = new Point3D(HouseX + 3, HouseY, (sbyte)z);

        var multiA = new TestMulti(GuildHouseId);
        var multiB = new TestMulti(GuildHouseId);
        try
        {
            multiA.MoveToWorld(locA, map);
            multiB.MoveToWorld(locB, map);

            // Non-vacuity: prove the two footprints actually intersect at the chosen 3-tile
            // separation. The per-sweep touchedMulti guard only proves each multi touched its OWN
            // footprint; without this, "overlapping" would be an unverified comment.
            var overlap = MultiArt.FootprintCells(multiA);
            var setB = new System.Collections.Generic.HashSet<MultiArt.Cell>(MultiArt.FootprintCells(multiB));
            overlap.RemoveAll(c => !setB.Contains(c));
            Assert.NotEmpty(overlap); // the two footprints must actually intersect, else the test is meaningless

            // The synthesizer must match the oracle over BOTH footprints (each sweep crosses
            // the shared, doubly-covered cells).
            MultiTestSupport.AssertSynthesizerMatchesCheckMovement(multiA, map);
            MultiTestSupport.AssertSynthesizerMatchesCheckMovement(multiB, map);
        }
        finally
        {
            multiA.Delete();
            multiB.Delete();
        }
    }

    /// <summary>
    /// A boat placed over open water: deck surface tiles are walkable, surrounding water blocks
    /// the (non-swimming) walker. Exercises the synthesizer over water-adjacent deck-edge geometry.
    /// </summary>
    /// <remarks>
    /// The boat is placed at Z=0 (the deck's world Z), NOT at the water average Z (-15 here).
    /// The deck floor tiles stand at world Z 0, so a non-swimming walker only finds walkable
    /// transitions when the sweep origin Z equals the deck Z. Placing at the water avgZ makes the
    /// sweep vacuous ("no walkable transitions") because the deck is 15 tiles overhead and water
    /// blocks the rest — that vacuity is a fixture concern, not a synthesizer divergence (the
    /// synthesizer agrees with the oracle at every direction either way).
    /// </remarks>
    [Fact]
    public void BoatOverWater_SynthesizerMatchesCheckMovement()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];

        var boat = new TestMulti(BoatMultiId);
        boat.MoveToWorld(new Point3D(WaterX, WaterY, DeckZ), map);
        try
        {
            MultiTestSupport.AssertSynthesizerMatchesCheckMovement(boat, map);
        }
        finally
        {
            boat.Delete();
        }
    }

    /// <summary>
    /// Redesign a foundation in place (Internalize -> swap MCL -> MoveToWorld back, the same
    /// re-registration pattern HouseFoundation uses on commit) and assert the synthesizer reads
    /// the LIVE, post-redesign <c>Components</c> — i.e. it matches CheckMovement on the NEW shape.
    /// </summary>
    [Fact]
    public void RedesignedFoundation_SynthesizerMatchesNewFootprint()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];

        var foundation = new SwappableFoundation(GuildHouseId);
        map.GetAverageZ(HouseX, HouseY, out _, out var z, out _);
        var loc = new Point3D(HouseX, HouseY, (sbyte)z);
        foundation.MoveToWorld(loc, map);
        try
        {
            // Redesign, RE-REGISTERED so sectors track the new footprint (model a real commit).
            // Internalize() fires Map.OnLeave (removes the OLD footprint's registration); we swap
            // the MCL and MoveToWorld back, firing Map.OnEnter -> AddMulti against the new shape.
            foundation.Internalize();
            foundation.Redesign(MultiData.GetComponents(0x7A)); // Tower footprint (different shape)
            foundation.MoveToWorld(loc, map);

            MultiTestSupport.AssertSynthesizerMatchesCheckMovement(foundation, map);
        }
        finally
        {
            if (!foundation.Deleted)
            {
                foundation.Delete();
            }
        }
    }

    /// <summary>
    /// Extensible slot for repo-owner-supplied gnarly placements. The assertion already covers
    /// any (map,x,y) by construction — only the coordinates need filling in.
    ///
    /// TODO(coords): repo owner to supply (map,x,y) for a static tree inside a footprint and a
    /// dungeon cave-wall corner; add InlineData rows here — the assertion already covers them by
    /// construction. (Left intentionally unhunted: do not invent tree/dungeon coords.)
    /// </summary>
    [Theory]
    [InlineData(MapId, HouseX, HouseY)] // known-good open Trammel placement (passes today)
    public void UserSuppliedScenarios_SynthesizerMatchesCheckMovement(int mapId, int x, int y)
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[mapId];

        var multi = new TestMulti(GuildHouseId);
        map.GetAverageZ(x, y, out _, out var z, out _);
        multi.MoveToWorld(new Point3D(x, y, (sbyte)z), map);
        try
        {
            MultiTestSupport.AssertSynthesizerMatchesCheckMovement(multi, map);
        }
        finally
        {
            multi.Delete();
        }
    }
}
