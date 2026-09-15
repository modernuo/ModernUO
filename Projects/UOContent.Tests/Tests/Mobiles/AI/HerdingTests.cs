using Server;
using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Driven manually against live map statics; the pathfinder's buffers are not reentrant.
[Collection("Sequential Pathfinding Tests")]
public class HerdingTests
{
    private sealed class Stub : BaseCreature
    {
        public Stub(Serial serial) : base(serial) => Body = 0xC9;
    }

    [SkippableFact]
    public void HerdedCreature_ReachesTheTile_AndClearsTargetLocation()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Maps[1];
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = new Stub(World.NewMobile);
        bc.DefaultMobileInit();
        bc.MoveToWorld(new Point3D(1500, 1600, (sbyte)z), map);
        BaseAI ai = new AnimalAI(bc);
        ai.AITimer?.Stop();
        StepCache.Instance.Clear();

        try
        {
            var goal = new Point2D(1500, 1595);
            bc.TargetLocation = goal;

            for (var i = 0; i < 40 && bc.TargetLocation != null; i++)
            {
                ai.NextMove = 0;
                ai.CheckHerding();
            }

            Assert.Null(bc.TargetLocation);
            Assert.Equal(goal, new Point2D(bc.X, bc.Y));
        }
        finally
        {
            bc.Delete();
        }
    }
}
