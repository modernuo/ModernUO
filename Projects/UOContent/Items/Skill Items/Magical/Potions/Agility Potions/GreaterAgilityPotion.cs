using System;

namespace Server.Items
{
    public class GreaterAgilityPotion : BaseAgilityPotion
    {
        [Constructible]
        public GreaterAgilityPotion() : base(PotionEffect.AgilityGreater)
        {
        }

        public GreaterAgilityPotion(Serial serial) : base(serial)
        {
        }

        public override int DexOffset => 20;
        public override TimeSpan Duration => TimeSpan.FromMinutes(2.0);

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
