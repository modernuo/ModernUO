namespace Server.Items
{
    public class AdmiralsHeartyRum : BeverageBottle
    {
        [Constructible]
        public AdmiralsHeartyRum() : base(BeverageType.Ale) => Hue = 0x66C;

        public AdmiralsHeartyRum(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063477;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
