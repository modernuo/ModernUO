using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SpellbookCastTests
{
    [Fact]
    public void CastSpellRequestIgnoresSpellbookNotCarried()
    {
        SpellRegistry.Register(4, typeof(MagicArrowSpell));

        var player = new PlayerMobile { Player = true, Str = 100, Int = 100 };
        player.Mana = player.ManaMax;
        player.AddItem(new Backpack());
        player.Backpack.DropItem(new SulfurousAsh(10));

        var book = new Spellbook(ulong.MaxValue);
        player.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        book.MoveToWorld(new Point3D(1000, 1001, 0), Map.Felucca);

        try
        {
            Spellbook.CastSpellRequest(player, 4, book); // Magic Arrow

            Assert.Null(player.Spell);
        }
        finally
        {
            book.Delete();
            player.Delete();
        }
    }
}
