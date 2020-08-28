namespace Server.Items
{
    public class IolosLute : Lute
    {
        [Constructible]
        public IolosLute()
        {
            Hue = 0x47E;
            Slayer = SlayerName.Silver;
            // Slayer2 = SlayerName.DaemonDismissal;
            Slayer2 = SlayerName.Exorcism;
        }

        public IolosLute(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063479;

        public override int InitMinUses => 1600;
        public override int InitMaxUses => 1600;

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
