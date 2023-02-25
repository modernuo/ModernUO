using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SandMiningBook : Item
{
    [Constructible]
    public SandMiningBook() : base(0xFF4) => Weight = 1.0;
    public override int LabelNumber => 1153531; // Find Glass-Quality Sand

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from is not PlayerMobile pm || from.Skills.Mining.Base < 100.0)
        {
            // Only a Grandmaster Miner can learn from this book.
            from.SendLocalizedMessage(1080041);
        }
        else if (pm.SandMining)
        {
            from.SendLocalizedMessage(1080066); // You have already learned this information.
        }
        else
        {
            pm.SandMining = true;
            // You have learned to make items from glass.  You will need to find miners to mine fine sand for you to make these items.
            from.SendLocalizedMessage(1080065);
            Delete();
        }
    }
}
