using Server.Mobiles;

namespace Server.Items
{
    public class SandMiningBook : Item
    {
        [Constructible]
        public SandMiningBook() : base(0xFF4) => Weight = 1.0;

        public SandMiningBook(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Find Glass-Quality Sand";

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
            else if (from is not PlayerMobile pm || from.Skills.Mining.Base < 100.0)
            {
                from.SendMessage("Only a Grandmaster Miner can learn from this book.");
            }
            else if (pm.SandMining)
            {
                from.SendMessage("You have already learned this information.");
            }
            else
            {
                pm.SandMining = true;
                from.SendMessage(
                    "You have learned how to mine fine sand. Target sand areas when mining to look for fine sand."
                );
                Delete();
            }
        }
    }
}
