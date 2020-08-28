namespace Server.Items
{
    // TODO: Commodity?
    public class DaemonBone : BaseReagent
    {
        [Constructible]
        public DaemonBone(int amount = 1) : base(0xF80, amount)
        {
        }

        public DaemonBone(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 1.0;

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
