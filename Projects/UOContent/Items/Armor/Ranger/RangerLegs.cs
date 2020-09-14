namespace Server.Items
{
    [Flippable(0x13da, 0x13e1)]
    public class RangerLegs : BaseArmor
    {
        [Constructible]
        public RangerLegs() : base(0x13DA)
        {
            Weight = 3.0;
            Hue = 0x59C;
        }

        public RangerLegs(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 30;
        public override int OldStrReq => 35;

        public override int ArmorBase => 16;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1041496; // studded leggings, ranger armor

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (Weight == 3.0)
            {
                Weight = 5.0;
            }
        }
    }
}
