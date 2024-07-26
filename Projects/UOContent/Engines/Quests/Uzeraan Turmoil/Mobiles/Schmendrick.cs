using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class Schmendrick : BaseQuester
{
    [Constructible]
    public Schmendrick() : base("the High Mage")
    {
    }

    public override string DefaultName => "Schmendrick";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83F3;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new Robe(0x4DD));
        AddItem(new WizardsHat(0x482));
        AddItem(new Shoes(0x482));

        HairItemID = 0x203C;
        HairHue = 0x455;

        FacialHairItemID = 0x203E;
        FacialHairHue = 0x455;

        var staff = new GlacialStaff
        {
            Movable = false
        };
        AddItem(staff);

        var pack = new Backpack
        {
            Movable = false
        };
        AddItem(pack);
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 7;

    public override bool CanTalkTo(PlayerMobile to) =>
        to.Quest is UzeraanTurmoilQuest qs && qs.FindObjective<FindSchmendrickObjective>() != null;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is not UzeraanTurmoilQuest)
        {
            return;
        }

        if (UzeraanTurmoilQuest.HasLostScrollOfPower(player))
        {
            FocusTo(player);
            qs.AddConversation(new LostScrollOfPowerConversation(false));
            return;
        }

        var obj = qs.FindObjective<FindSchmendrickObjective>();

        if (obj?.Completed == false)
        {
            FocusTo(player);
            obj.Complete();
        }
        else if (contextMenu)
        {
            FocusTo(player);
            SayTo(player, 1049357); // I have nothing more for you at this time.
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is not BlankScroll || !UzeraanTurmoilQuest.HasLostScrollOfPower(from))
        {
            return base.OnDragDrop(from, dropped);
        }

        FocusTo(from);
        var scroll = new SchmendrickScrollOfPower();

        if (!from.PlaceInBackpack(scroll))
        {
            scroll.Delete();
            // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
            from.SendLocalizedMessage(1046260);
            return false;
        }

        dropped.Consume();

        // Schmendrick scribbles on the scroll for a few moments and hands you the finished product.
        from.SendLocalizedMessage(1049346);
        return dropped.Deleted;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (m is not PlayerMobile || m.Frozen || m.Alive || !InRange(m, 4) || InRange(oldLocation, 4) || !InLOS(m))
        {
            return;
        }

        if (m.Map?.CanFit(m.Location, 16, false, false) != true)
        {
            m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            return;
        }

        Direction = GetDirectionTo(m);

        m.PlaySound(0x214);
        m.FixedEffect(0x376A, 10, 16);

        m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
    }
}
