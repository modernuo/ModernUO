using Server.Mobiles;

namespace Server.Items
{
    public class MasonryBook : Item
    {
        [Constructible]
        public MasonryBook() : base(0xFBE) => Weight = 1.0;

        public MasonryBook(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Making Valuables With Stonecrafting";

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
            else if (from is not PlayerMobile pm || from.Skills.Carpentry.Base < 100.0)
            {
                from.SendMessage("Only a Grandmaster Carpenter can learn from this book.");
            }
            else if (pm.Masonry)
            {
                from.SendMessage("You have already learned this information.");
            }
            else
            {
                pm.Masonry = true;
                from.SendMessage(
                    "You have learned to make items from stone. You will need miners to gather stones for you to make these items."
                );
                Delete();
            }
        }
    }
}
