using Server.Mobiles;

namespace Server.Items
{
    public class GlassblowingBook : Item
    {
        [Constructible]
        public GlassblowingBook() : base(0xFF4) => Weight = 1.0;

        public GlassblowingBook(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Crafting Glass With Glassblowing";

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

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from is not PlayerMobile pm || from.Skills.Alchemy.Base < 100.0)
            {
                from.SendMessage("Only a Grandmaster Alchemist can learn from this book.");
            }
            else if (pm.Glassblowing)
            {
                from.SendMessage("You have already learned this information.");
            }
            else
            {
                pm.Glassblowing = true;
                from.SendMessage(
                    "You have learned to make items from glass. You will need to find miners to mine find sand for you to make these items."
                );
                Delete();
            }
        }
    }
}
