namespace Server.Items
{
    public class SecretStoneDoor1 : BaseDoor
    {
        [Constructible]
        public SecretStoneDoor1(DoorFacing facing) : base(
            0xE8 + 2 * (int)facing,
            0xE9 + 2 * (int)facing,
            0xED,
            0xF4,
            GetOffset(facing)
        )
        {
        }

        public SecretStoneDoor1(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SecretDungeonDoor : BaseDoor
    {
        [Constructible]
        public SecretDungeonDoor(DoorFacing facing) : base(
            0x314 + 2 * (int)facing,
            0x315 + 2 * (int)facing,
            0xED,
            0xF4,
            GetOffset(facing)
        )
        {
        }

        public SecretDungeonDoor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SecretStoneDoor2 : BaseDoor
    {
        [Constructible]
        public SecretStoneDoor2(DoorFacing facing) : base(
            0x324 + 2 * (int)facing,
            0x325 + 2 * (int)facing,
            0xED,
            0xF4,
            GetOffset(facing)
        )
        {
        }

        public SecretStoneDoor2(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SecretWoodenDoor : BaseDoor
    {
        [Constructible]
        public SecretWoodenDoor(DoorFacing facing) : base(
            0x334 + 2 * (int)facing,
            0x335 + 2 * (int)facing,
            0xED,
            0xF4,
            GetOffset(facing)
        )
        {
        }

        public SecretWoodenDoor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SecretLightWoodDoor : BaseDoor
    {
        [Constructible]
        public SecretLightWoodDoor(DoorFacing facing) : base(
            0x344 + 2 * (int)facing,
            0x345 + 2 * (int)facing,
            0xED,
            0xF4,
            GetOffset(facing)
        )
        {
        }

        public SecretLightWoodDoor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SecretStoneDoor3 : BaseDoor
    {
        [Constructible]
        public SecretStoneDoor3(DoorFacing facing) : base(
            0x354 + 2 * (int)facing,
            0x355 + 2 * (int)facing,
            0xED,
            0xF4,
            GetOffset(facing)
        )
        {
        }

        public SecretStoneDoor3(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
