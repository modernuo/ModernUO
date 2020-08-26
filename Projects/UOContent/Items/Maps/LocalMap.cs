namespace Server.Items
{
    public class LocalMap : MapItem
    {
        [Constructible]
        public LocalMap()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public LocalMap(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1015230; // local map

        public override void CraftInit(Mobile from)
        {
            var skillValue = from.Skills.Cartography.Value;
            var dist = 64 + (int)(skillValue * 2);

            SetDisplay(from.X - dist, from.Y - dist, from.X + dist, from.Y + dist, 200, 200);
        }

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
