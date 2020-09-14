namespace Server.Factions
{
    public class Silver : Item
    {
        [Constructible]
        public Silver() : this(1)
        {
        }

        [Constructible]
        public Silver(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Silver(int amount) : base(0xEF0)
        {
            Stackable = true;
            Amount = amount;
        }

        public Silver(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.02;

        public override int GetDropSound()
        {
            if (Amount <= 1)
            {
                return 0x2E4;
            }

            if (Amount <= 5)
            {
                return 0x2E5;
            }

            return 0x2E6;
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
