namespace Server.Items
{
    [Flippable(0x1451, 0x1456)]
    public class DaemonHelm : BaseArmor
    {
        [Constructible]
        public DaemonHelm() : base(0x1451)
        {
            Hue = 0x648;
            Weight = 3.0;

            ArmorAttributes.SelfRepair = 1;
        }

        public DaemonHelm(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 7;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override int AosStrReq => 20;
        public override int OldStrReq => 40;

        public override int ArmorBase => 46;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1041374; // daemon bone helmet

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Weight == 1.0)
            {
                Weight = 3.0;
            }

            if (ArmorAttributes.SelfRepair == 0)
            {
                ArmorAttributes.SelfRepair = 1;
            }
        }
    }
}
