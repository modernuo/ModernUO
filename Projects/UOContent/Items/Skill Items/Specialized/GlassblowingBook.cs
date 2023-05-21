using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GlassblowingBook : Item
{
    [Constructible]
    public GlassblowingBook() : base(0xFF4) => Weight = 1.0;

    public override int LabelNumber => 1153528; // Crafting glass with Glassblowing

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from is not PlayerMobile pm || from.Skills.Alchemy.Base < 100.0)
        {
            from.SendLocalizedMessage(1080042); // Only a Grandmaster Alchemist can learn from this book.
        }
        else if (pm.Glassblowing)
        {
            from.SendLocalizedMessage(1080066); // You have already learned this information.
        }
        else
        {
            pm.Glassblowing = true;
            // You have learned to make items from glass.  You will need to find miners to mine fine sand for you to make these items.
            from.SendLocalizedMessage(1111702);
            Delete();
        }
    }
}
