using System.Collections.Generic;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CorpseLootTests
{
    [Fact]
    public void LiftedWornGear_PutBack_StaysLoot()
    {
        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0x190;

        // Weapons take their layer from tiledata, which CI doesn't load.
        var sword = new VikingSword { Layer = Layer.OneHanded };
        Assert.True(owner.EquipItem(sword));

        var corpse = new Corpse(owner, new List<Item> { sword });
        corpse.DropItem(sword);

        try
        {
            Assert.Contains(sword, corpse.EquipItems);

            corpse.OnItemLifted(owner, sword);
            sword.Internalize();
            corpse.DropItem(sword);

            Assert.Same(corpse, sword.Parent);
            Assert.DoesNotContain(sword, corpse.EquipItems);
        }
        finally
        {
            corpse.Delete();
            owner.Delete();
        }
    }
}
