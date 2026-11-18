using Server;
using Server.Engines.Doom;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class LeverPuzzleLeverTests
{
    [Fact]
    public void LeverOutOfReachIsNotPulled()
    {
        var controller = new LeverPuzzleController();
        var lever = new LeverPuzzleLever(1, controller);
        var m = new Mobile();
        try
        {
            lever.MoveToWorld(new Point3D(1800, 1800, 0), Map.Felucca);
            m.MoveToWorld(new Point3D(1810, 1800, 0), Map.Felucca);
            var itemId = lever.ItemID;

            lever.OnDoubleClick(m);

            Assert.Equal(itemId, lever.ItemID);
        }
        finally
        {
            m.Delete();
            lever.Delete();
            controller.Delete();
        }
    }
}
