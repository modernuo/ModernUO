namespace Server.Items
{
    public class SongWovenMantle : LeafArms
    {
        [Constructible]
        public SongWovenMantle()
        {
            Hue = 0x493;

            SkillBonuses.SetValues(0, SkillName.Musicianship, 10.0);

            Attributes.Luck = 100;
            Attributes.DefendChance = 5;
        }

        public SongWovenMantle(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072931; // Song Woven Mantle

        public override int BasePhysicalResistance => 14;
        public override int BaseColdResistance => 14;
        public override int BaseEnergyResistance => 16;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
