using Server.Network;

namespace Server.Items
{
    [Flippable(0x2A79, 0x2A7A)]
    public class MountedPixieWhiteComponent : AddonComponent
    {
        public MountedPixieWhiteComponent() : base(0x2A79)
        {
        }

        public MountedPixieWhiteComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074482; // Mounted pixie

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
            {
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x562, 0x564));
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class MountedPixieWhiteAddon : BaseAddon
    {
        public MountedPixieWhiteAddon()
        {
            AddComponent(new MountedPixieWhiteComponent(), 0, 0, 0);
        }

        public MountedPixieWhiteAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new MountedPixieWhiteDeed();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class MountedPixieWhiteDeed : BaseAddonDeed
    {
        [Constructible]
        public MountedPixieWhiteDeed() => LootType = LootType.Blessed;

        public MountedPixieWhiteDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new MountedPixieWhiteAddon();
        public override int LabelNumber => 1074482; // Mounted pixie

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
