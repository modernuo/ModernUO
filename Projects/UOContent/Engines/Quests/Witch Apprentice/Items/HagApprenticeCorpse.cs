using System.Collections.Generic;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Hag
{
    public class HagApprenticeCorpse : Corpse
    {
        [Constructible]
        public HagApprenticeCorpse() : base(GetOwner(), GetEquipment())
        {
            Direction = Direction.South;

            foreach (var item in EquipItems)
            {
                DropItem(item);
            }
        }

        public HagApprenticeCorpse(Serial serial) : base(serial)
        {
        }

        // TODO: What is this? Why are we creating a mobile and deleting it?
        private static Mobile GetOwner()
        {
            var apprentice = new Mobile();

            apprentice.Hue = Race.Human.RandomSkinHue();
            apprentice.Female = false;
            apprentice.Body = 0x190;

            apprentice.Delete();

            return apprentice;
        }

        private static List<Item> GetEquipment() => new();

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
                            SendLocalizedMessageTo(
                                from,
                                1055047
                            ); // You examine the corpse, but it doesn't fit the description of the particular apprentice the Hag tasked you with finding.
                        }

                        return;
                    }
                }
            }

            SendLocalizedMessageTo(from, 1055048); // You examine the corpse, but find nothing of interest.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
