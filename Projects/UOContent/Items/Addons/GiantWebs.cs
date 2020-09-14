namespace Server.Items
{
    public class GiantWeb1 : BaseAddon
    {
        [Constructible]
        public GiantWeb1()
        {
            var itemID = 4280;
            var count = 5;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    count - 1 - i,
                    -(count - 1 - i),
                    0
                );
            }
        }

        public GiantWeb1(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadByte();
        }
    }

    public class GiantWeb2 : BaseAddon
    {
        [Constructible]
        public GiantWeb2()
        {
            var itemID = 4285;
            var count = 5;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    i,
                    -i,
                    0
                );
            }
        }

        public GiantWeb2(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadByte();
        }
    }

    public class GiantWeb3 : BaseAddon
    {
        [Constructible]
        public GiantWeb3()
        {
            var itemID = 4290;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    i,
                    -i,
                    0
                );
            }
        }

        public GiantWeb3(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadByte();
        }
    }

    public class GiantWeb4 : BaseAddon
    {
        [Constructible]
        public GiantWeb4()
        {
            var itemID = 4294;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    count - 1 - i,
                    -(count - 1 - i),
                    0
                );
            }
        }

        public GiantWeb4(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadByte();
        }
    }

    public class GiantWeb5 : BaseAddon
    {
        [Constructible]
        public GiantWeb5()
        {
            var itemID = 4298;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    i,
                    -i,
                    0
                );
            }
        }

        public GiantWeb5(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadByte();
        }
    }

    public class GiantWeb6 : BaseAddon
    {
        [Constructible]
        public GiantWeb6()
        {
            var itemID = 4302;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    count - 1 - i,
                    -(count - 1 - i),
                    0
                );
            }
        }

        public GiantWeb6(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadByte();
        }
    }
}
