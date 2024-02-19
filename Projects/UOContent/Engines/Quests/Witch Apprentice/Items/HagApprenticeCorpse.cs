using ModernUO.Serialization;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Hag;

[SerializationGenerator(0, false)]
public partial class HagApprenticeCorpse : Corpse
{
    [Constructible]
    public HagApprenticeCorpse() : base(GetOwner(), []) => Direction = Direction.South;

    private static Mobile GetOwner()
    {
        var apprentice = new Mobile
        {
            Hue = Race.Human.RandomSkinHue(),
            Female = false,
            Body = 0x190
        };

        apprentice.Delete();

        return apprentice;
    }

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add("a charred corpse");
    }

    public override void OnSingleClick(Mobile from)
    {
        var hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

        from.NetState.SendMessage(Serial, ItemID, MessageType.Label, hue, 3, true, null, "", "a charred corpse");
    }

    public override void Open(Mobile from, bool checkSelfLoot)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        if (from is PlayerMobile player)
        {
            var qs = player.Quest;

            if (qs is WitchApprenticeQuest)
            {
                var obj = qs.FindObjective<FindApprenticeObjective>();
                if (obj?.Completed == false)
                {
                    if (obj.Corpse == this)
                    {
                        obj.Complete();
                        Delete();
                    }
                    else
                    {
                        // You examine the corpse, but it doesn't fit the description of the particular apprentice the Hag tasked you with finding.
                        SendLocalizedMessageTo(from, 1055047);
                    }

                    return;
                }
            }
        }

        SendLocalizedMessageTo(from, 1055048); // You examine the corpse, but find nothing of interest.
    }
}
