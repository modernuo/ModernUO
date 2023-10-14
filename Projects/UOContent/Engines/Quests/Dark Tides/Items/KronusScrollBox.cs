using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class KronusScrollBox : MetalBox
{
    [Constructible]
    public KronusScrollBox()
    {
        ItemID = 0xE80;
        Movable = false;

        for (var i = 0; i < 40; i++)
        {
            Item scroll = Loot.RandomScroll(0, 15, SpellbookType.Necromancer);
            scroll.Movable = false;
            DropItem(scroll);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && pm.InRange(GetWorldLocation(), 2))
        {
            var qs = pm.Quest;

            if (qs is DarkTidesQuest)
            {
                QuestObjective obj = qs.FindObjective<FindCallingScrollObjective>();

                if (obj?.Completed == false || DarkTidesQuest.HasLostCallingScroll(from))
                {
                    Item scroll = new KronusScroll();

                    if (pm.PlaceInBackpack(scroll))
                    {
                        // You rummage through the scrolls until you find the Scroll of Calling.  You quickly put it in your pack.
                        pm.SendLocalizedMessage(1060120, "", 0x41);

                        if (obj?.Completed == false)
                        {
                            obj.Complete();
                        }
                    }
                    else
                    {
                        pm.SendLocalizedMessage(1060148, "", 0x41); // You were unable to take the scroll.
                        scroll.Delete();
                    }
                }
            }
        }

        base.OnDoubleClick(from);
    }
}
