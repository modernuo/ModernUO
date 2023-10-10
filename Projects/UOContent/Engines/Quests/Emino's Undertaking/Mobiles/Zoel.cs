using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0, false)]
public partial class Zoel : BaseQuester
{
    [Constructible]
    public Zoel() : base("the Masterful Tactician")
    {
    }

    public override string DefaultName => "Elite Ninja Zoel";

    public override int TalkNumber => -1;

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83FE;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        HairItemID = 0x203B;
        HairHue = 0x901;

        AddItem(new HakamaShita(0x1));
        AddItem(new NinjaTabi());
        AddItem(new TattsukeHakama());
        AddItem(new Bandana());

        AddItem(new LeatherNinjaBelt());

        var tekagi = new Tekagi();
        tekagi.Movable = false;
        AddItem(tekagi);
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 2;

    public override bool CanTalkTo(PlayerMobile to) => to.Quest is EminosUndertakingQuest;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is EminosUndertakingQuest)
        {
            QuestObjective obj = qs.FindObjective<FindZoelObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
            }
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from is PlayerMobile player)
        {
            var qs = player.Quest;

            if (qs is EminosUndertakingQuest)
            {
                if (dropped is NoteForZoel)
                {
                    QuestObjective obj = qs.FindObjective<GiveZoelNoteObjective>();

                    if (obj?.Completed == false)
                    {
                        dropped.Delete();
                        obj.Complete();
                        return true;
                    }
                }
            }
        }

        return base.OnDragDrop(from, dropped);
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (!m.Frozen && !m.Alive && InRange(m, 4) && !InRange(oldLocation, 4) && InLOS(m))
        {
            if (m.Map?.CanFit(m.Location, 16, false, false) != true)
            {
                m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            }
            else
            {
                Direction = GetDirectionTo(m);

                m.PlaySound(0x214);
                m.FixedEffect(0x376A, 10, 16);

                m.CloseGump<ResurrectGump>();
                m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
            }
        }
    }
}
