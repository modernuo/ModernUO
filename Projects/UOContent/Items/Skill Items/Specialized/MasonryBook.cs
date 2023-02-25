using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MasonryBook : Item
{
    [Constructible]
    public MasonryBook() : base(0xFBE) => Weight = 1.0;

    public override int LabelNumber => 1153527; // Making valuables with Stonecrafting

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from is not PlayerMobile pm || from.Skills.Carpentry.Base < 100.0)
        {
            from.SendLocalizedMessage(1080043); // Only a Grandmaster Carpenter can learn from this book.
        }
        else if (pm.Masonry)
        {
            from.SendLocalizedMessage(1080066); // You have already learned this information.
        }
        else
        {
            pm.Masonry = true;
            // You have learned to make items from stone. You will need miners to gather stones for you to make these items.
            from.SendLocalizedMessage(1080044);
            Delete();
        }
    }
}
