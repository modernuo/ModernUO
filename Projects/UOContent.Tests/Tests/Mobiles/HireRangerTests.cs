using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

// A ranger's constructor must not leave equipment behind: an item that EquipItem refuses has no
// parent and nothing deletes it, so it stays in the world save for good.
[Collection("Sequential UOContent Tests")]
public class HireRangerTests
{
    [Fact]
    public void CreatingAndDeletingRangers_LeavesNoItemBehind()
    {
        var itemsBefore = World.Items.Count;

        // A male ranger is about one in two; 32 draws make a run of females vanishingly unlikely.
        for (var i = 0; i < 32; i++)
        {
            new HireRanger().Delete();
        }

        Assert.Equal(itemsBefore, World.Items.Count);
    }
}
