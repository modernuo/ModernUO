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
        var owner = new Mobile(World.NewMobile);
        VikingSword sword = null;
        Corpse corpse = null;

        try
        {
            owner.DefaultMobileInit();
            owner.Body = 0x190;

            // Weapons take their layer from tiledata, which CI doesn't load.
            sword = new VikingSword { Layer = Layer.OneHanded };
            Assert.True(owner.EquipItem(sword));

            corpse = new Corpse(owner, [sword]);
            corpse.DropItem(sword);

            Assert.Contains(sword, corpse.EquipItems);

            corpse.OnItemLifted(owner, sword);
            sword.Internalize();
            corpse.DropItem(sword);

            Assert.Same(corpse, sword.Parent);
            Assert.DoesNotContain(sword, corpse.EquipItems);
        }
        finally
        {
            corpse?.Delete();
            sword?.Delete();
            owner.Delete();
        }
    }
}
