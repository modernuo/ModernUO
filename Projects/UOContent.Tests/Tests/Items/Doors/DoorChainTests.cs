using System.Collections.Generic;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class DoorChainTests
{
    [Fact]
    public void GetChain_LoopNotThroughStart_YieldsEachDoorOnce()
    {
        var a = new MetalDoor(DoorFacing.WestCW);
        var b = new MetalDoor(DoorFacing.WestCW);
        var c = new MetalDoor(DoorFacing.WestCW);

        try
        {
            // A -> B -> C -> B: the chain never returns to A
            a.Link = b;
            b.Link = c;
            c.Link = b;

            var chain = new List<BaseDoor>();
            foreach (var door in a.GetChain())
            {
                chain.Add(door);
                Assert.True(chain.Count <= 3, "Chain enumeration did not terminate");
            }

            Assert.Equal([a, b, c], chain);
        }
        finally
        {
            a.Delete();
            b.Delete();
            c.Delete();
        }
    }
}
