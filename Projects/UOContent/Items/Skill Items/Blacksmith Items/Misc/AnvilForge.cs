using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0xFAF, 0xFB0), Anvil]
    public class Anvil : Item
    {
        [Constructible]
        public Anvil() : base(0xFAF) => Movable = false;

        public Anvil(Serial serial) : base(serial)
        {
        }

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

    [Forge]
    public class Forge : Item
    {
        [Constructible]
        public Forge() : base(0xFB1) => Movable = false;

        public Forge(Serial serial) : base(serial)
        {
        }

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
