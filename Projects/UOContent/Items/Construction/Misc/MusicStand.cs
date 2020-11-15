namespace Server.Items
{
    [Furniture, Flippable(0xEBB, 0xEBC)]
    public class TallMusicStand : Item
    {
        [Constructible]
        public TallMusicStand() : base(0xEBB) => Weight = 10.0;

        public TallMusicStand(Serial serial) : base(serial)
        {
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

            if (Weight == 8.0)
            {
                Weight = 10.0;
            }
        }
    }

    [Furniture, Flippable(0xEB6, 0xEB8)]
    public class ShortMusicStand : Item
    {
        [Constructible]
        public ShortMusicStand() : base(0xEB6) => Weight = 10.0;

        public ShortMusicStand(Serial serial) : base(serial)
        {
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

            if (Weight == 6.0)
            {
                Weight = 10.0;
            }
        }
    }
}
