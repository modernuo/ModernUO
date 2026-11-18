using System.Reflection;
using Server;
using Server.Factions;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class StormsEyeTests
{
    [Fact]
    public void BlastHitsTheFactionMemberInRangeNotTheUser()
    {
        var origin = new Point3D(1800, 1800, 0);

        var user = new PlayerMobile();
        var enemy = new PlayerMobile();
        try
        {
            user.MoveToWorld(new Point3D(origin.X + 20, origin.Y, 0), Map.Felucca);
            enemy.MoveToWorld(new Point3D(origin.X + 2, origin.Y, 0), Map.Felucca);
            _ = new PlayerState(enemy, new Minax(), []);

            var onHit = typeof(StormsEye).GetMethod("OnHit", BindingFlags.NonPublic | BindingFlags.Static);
            onHit!.Invoke(null, [user, origin, Map.Felucca]);

            // The harmful action is done by the user against each target.
            Assert.Same(enemy, user.Combatant);
        }
        finally
        {
            user.Delete();
            enemy.Delete();
        }
    }
}
