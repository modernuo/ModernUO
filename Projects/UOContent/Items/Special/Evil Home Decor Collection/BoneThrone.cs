namespace Server.Items
{
    [Flippable(0x2A58, 0x2A59)]
    public class BoneThroneComponent : AddonComponent
    {
        public BoneThroneComponent() : base(0x2A58)
        {
        }

        public BoneThroneComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074476; // Bone throne

        public override bool OnMoveOver(Mobile m)
        {
            var allow = base.OnMoveOver(m);

            if (allow && m.Alive && m.Player && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
            {
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x54B, 0x54D));
            }

            return allow;
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

    public class BoneThroneAddon : BaseAddon
    {
        [Constructible]
        public BoneThroneAddon()
        {
            AddComponent(new BoneThroneComponent(), 0, 0, 0);
        }

        public BoneThroneAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new BoneThroneDeed();

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

    public class BoneThroneDeed : BaseAddonDeed
    {
        [Constructible]
        public BoneThroneDeed() => LootType = LootType.Blessed;

        public BoneThroneDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new BoneThroneAddon();
        public override int LabelNumber => 1074476; // Bone throne

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
