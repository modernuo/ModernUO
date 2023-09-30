using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PrismaticCrystal : Item
{
    [Constructible]
    public PrismaticCrystal() : base(0x2DA)
    {
        Movable = false;
        Hue = 0x32;
    }

    public override int LabelNumber => 1074269; // prismatic crystal

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm || pm.Backpack == null)
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
}
