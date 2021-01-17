using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class PrismaticCrystal : Item
    {
        [Constructible]
        public PrismaticCrystal() : base(0x2DA)
        {
            Movable = false;
            Hue = 0x32;
        }

        public PrismaticCrystal(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074269; // prismatic crystal

        public override void OnDoubleClick(Mobile from)
        {
            if (!(from is PlayerMobile pm) || pm.Backpack == null)
            {
                return;
            }

            if (pm.InRange(GetWorldLocation(), 2))
            {
                if (MLQuestSystem.GetContext(pm)?.IsDoingQuest(typeof(UnfadingMemoriesPartOne)) == true &&
                    pm.Backpack.FindItemByType<PrismaticAmber>(false) == null)
                {
                    Item amber = new PrismaticAmber();

                    if (pm.PlaceInBackpack(amber))
                    {
                        MLQuestSystem.MarkQuestItem(pm, amber);
                        Delete();
                    }
                    else
                    {
                        pm.SendLocalizedMessage(502385); // Your pack cannot hold this item.
                        amber.Delete();
                    }
                }
                else
                {
                    pm.SendLocalizedMessage(1075464); // You already have as many of those as you need.
                }
            }
            else
            {
                pm.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
