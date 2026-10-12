using System.Collections.Generic;
using System.IO;
using Server.Engines.Pathing.Tiered;
using Xunit;

namespace Server.Tests.Pathfinding.Tiered;

public class NavGraphFileTests
{
    /// <summary>A graph written and read back plans the same routes at the same cost, and a file of other map files is refused.</summary>
    [Fact]
    public void WrittenAndReadBack_PlansTheSame()
    {
        var terrain = new GridNavTerrain(128, 128, '.');

        terrain.Fill(64, 0, 64, 127, '#');
        terrain.Fill(64, 90, 64, 92, '.');

        var graph = new NavGraph(terrain);

        graph.BuildAll(new NavWindow());

        var path = Path.Combine(Path.GetTempPath(), $"navgraph-test-{System.Guid.NewGuid():N}.nvg");

        try
        {
            NavGraphFile.Write(path, graph.Snapshot(42, [3, 4]));

            Assert.Null(NavGraphFile.Read(path, 43, 128, 128));

            var read = NavGraphFile.Read(path, 42, 128, 128);

            Assert.NotNull(read);
            Assert.Equal(new[] { 3, 4 }, read.Housed);

            var copy = new NavGraph(terrain);

            copy.Restore(read, 0, 0);
            copy.CountComponents();

            Assert.True(copy.Complete);
            Assert.Equal(graph.Nodes, copy.Nodes);
            Assert.Equal(graph.Edges, copy.Edges);

            var a = new HierarchicalPlanner();
            var b = new HierarchicalPlanner();
            var pa = new List<Point3D>();
            var pb = new List<Point3D>();

            Assert.Equal(a.Route(graph, 5, 5, 0, 120, 10, 0, pa), b.Route(copy, 5, 5, 0, 120, 10, 0, pb));
            Assert.Equal(a.LastCost, b.LastCost);
            Assert.Equal(pa, pb);

            // A house raised on the restored graph is drawn again from the ground, as on the original.
            terrain.Fill(64, 90, 64, 92, '#');
            copy.MarkDirty(64, 90, 64, 92);
            copy.Slice(new NavWindow(), long.MaxValue);

            Assert.Equal(NavStatus.Unreachable, b.Route(copy, 5, 5, 0, 120, 10, 0, pb));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
