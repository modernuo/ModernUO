using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StoneMiningBook : Item
{
    [Constructible]
    public StoneMiningBook() : base(0xFBE) => Weight = 1.0;

    public override int LabelNumber => 1153530; // Mining For Quality Stone

    public override void OnDoubleClick(Mobile from)
    {
        var pm = from as PlayerMobile;

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (pm == null || from.Skills.Mining.Base < 100.0)
        {
            from.SendLocalizedMessage(1080041); // Only a Grandmaster Miner can learn from this book.
        }
        else if (pm.StoneMining)
        {
            from.SendLocalizedMessage(1080066); // You have already learned this information.
        }
        else
        {
            pm.StoneMining = true;
            // You have learned to mine for stones.  Target mountains when mining to find stones.
            pm.SendLocalizedMessage(1080045);
            Delete();
        }
    }
}
