using System;
using Server.Mobiles;

namespace Server.Items
{
    public class TotemOfVoid : BaseTalisman
    {
        [Constructible]
        public TotemOfVoid() : base(0x2F5B)
        {
            Hue = 0x2D0;
            MaxChargeTime = 1800;

            Blessed = GetRandomBlessed();
            Protection = GetRandomProtection(false);

            Attributes.RegenHits = 2;
            Attributes.LowerManaCost = 10;
        }

        public TotemOfVoid(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075035; // Totem of the Void
        public override bool ForceShowName => true;

        public override Type GetSummoner() => Utility.RandomBool() ? typeof(SummonedSkeletalKnight) : typeof(SummonedSheep);

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version == 0 && Protection?.IsEmpty != false)
            {
                Protection = GetRandomProtection(false);
            }
        }
    }
}
