using System;
using Server;

namespace Server.Items
{
    public class BronzeRewardToken : Item
    {
        [Constructible]
        public BronzeRewardToken() : this(1)
        {
        }

        [Constructible]
        public BronzeRewardToken(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public BronzeRewardToken(int amount) : base(0xEED)
        {
            Stackable = true;
            Name = "bronze";
            Hue = 2418;
            Weight = 0;
            Amount = amount;
        }

        public BronzeRewardToken(Serial serial) : base(serial)
        {
        }

        public override int GetDropSound()
        {
            if (Amount <= 1)
                return 0x2E4;
            else if (Amount <= 5)
                return 0x2E5;
            else
                return 0x2E6;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
