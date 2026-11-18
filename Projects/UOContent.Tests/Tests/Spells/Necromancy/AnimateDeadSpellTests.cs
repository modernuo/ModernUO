using System.Collections;
using System.Reflection;
using Server.Spells.Necromancy;
using Xunit;

namespace Server.Tests.Spells.Necromancy;

[Collection("Sequential UOContent Tests")]
public class AnimateDeadSpellTests
{
    private static Mobile NewMobile()
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        return m;
    }

    [Fact]
    public void Register_PurgesEveryDeletedSummon()
    {
        var master = NewMobile();
        var a = NewMobile();
        var b = NewMobile();
        var c = NewMobile();

        AnimateDeadSpell.Register(master, a);
        AnimateDeadSpell.Register(master, b);
        a.Delete();
        b.Delete();

        AnimateDeadSpell.Register(master, c);

        var table = (IDictionary)typeof(AnimateDeadSpell)
            .GetField("_table", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
        Assert.Single((ICollection)table[master]);

        AnimateDeadSpell.Unregister(master, c);
        master.Delete();
        c.Delete();
    }
}
