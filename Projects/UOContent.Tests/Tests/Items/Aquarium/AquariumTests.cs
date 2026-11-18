using System.Linq;
using System.Reflection;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Xunit;

namespace Server.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class AquariumTests
{
    private static Mobile NewGm(uint serial)
    {
        var gm = new Mobile((Serial)serial);
        gm.DefaultMobileInit();
        gm.AccessLevel = AccessLevel.GameMaster;
        return gm;
    }

    private static void Click(Aquarium aquarium, Mobile from, string entry)
    {
        var list = PooledRefList<ContextMenuEntry>.Create();
        try
        {
            aquarium.GetContextMenuEntries(from, ref list);
            list.ToArray().First(e => e.GetType().Name == entry).OnClick(from, aquarium);
        }
        finally
        {
            list.Dispose();
        }
    }

    [Fact]
    public void ViewEvent_ConsumesEvent_ForPlayerWithAccess()
    {
        var gm = NewGm(0x2001u);
        var aquarium = new Aquarium(0x3062);

        try
        {
            aquarium.AddToEvents(1074367);

            Click(aquarium, gm, "ViewEventEntry");

            Assert.Empty(aquarium.Events);
        }
        finally
        {
            aquarium.Delete();
            gm.Delete();
        }
    }

    [Fact]
    public void CancelVacation_Works_ForPlayerWithAccess()
    {
        var gm = NewGm(0x2002u);
        var aquarium = new Aquarium(0x3062);

        try
        {
            typeof(Aquarium).GetProperty(nameof(Aquarium.VacationLeft))!
                .GetSetMethod(true)!.Invoke(aquarium, [5]);

            Click(aquarium, gm, "CancelVacationMode");

            Assert.Equal(0, aquarium.VacationLeft);
        }
        finally
        {
            aquarium.Delete();
            gm.Delete();
        }
    }

    [Fact]
    public void KillFish_KillsTheRequestedAmount()
    {
        var aquarium = new Aquarium(0x3062);

        try
        {
            for (var i = 0; i < 4; i++)
            {
                Assert.True(aquarium.AddFish(new LongClawCrab()));
            }

            aquarium.KillFish(4);

            Assert.Equal(4, aquarium.Items.OfType<BaseFish>().Count(f => f.Dead));
        }
        finally
        {
            aquarium.Delete();
        }
    }
}
