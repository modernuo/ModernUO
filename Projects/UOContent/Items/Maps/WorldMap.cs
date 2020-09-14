namespace Server.Items
{
    public class WorldMap : MapItem
    {
        [Constructible]
        public WorldMap()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public WorldMap(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1015233; // world map

        public override void CraftInit(Mobile from)
        {
            // Unlike the others, world map is not based on crafted location

            var skillValue = from.Skills.Cartography.Value;
            var x20 = (int)(skillValue * 20);
            var size = 25 + (int)(skillValue * 6.6);

            if (size < 200)
            {
                size = 200;
            }
            else if (size > 400)
            {
                size = 400;
            }

            SetDisplay(1344 - x20, 1600 - x20, 1472 + x20, 1728 + x20, size, size);
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
