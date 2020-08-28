using Server.Items;

namespace Server.Engines.Quests.Doom
{
    public class ChylothStaff : BlackStaff
    {
        [Constructible]
        public ChylothStaff() => Hue = 0x482;

        public ChylothStaff(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041111; // a magic staff

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
