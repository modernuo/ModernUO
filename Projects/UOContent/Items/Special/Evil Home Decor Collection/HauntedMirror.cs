namespace Server.Items
{
    [Flippable(0x2A7B, 0x2A7D)]
    public class HaunterMirrorComponent : AddonComponent
    {
        public HaunterMirrorComponent() : base(0x2A7B)
        {
        }

        public HaunterMirrorComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074800; // Haunted Mirror
        public override bool HandlesOnMovement => true;

        public override void OnMovement(Mobile m, Point3D old)
        {
            base.OnMovement(m, old);

            if (m.Alive && m.Player && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
            {
                if (!Utility.InRange(old, Location, 2) && Utility.InRange(m.Location, Location, 2))
                {
                    if (ItemID == 0x2A7B || ItemID == 0x2A7D)
                    {
                        Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x551, 0x553));
                        ItemID += 1;
                    }
                }
                else if (Utility.InRange(old, Location, 2) && !Utility.InRange(m.Location, Location, 2))
                {
                    if (ItemID == 0x2A7C || ItemID == 0x2A7E)
                    {
                        ItemID -= 1;
                    }
                }
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

    public class HaunterMirrorAddon : BaseAddon
    {
        [Constructible]
        public HaunterMirrorAddon()
        {
            AddComponent(new HaunterMirrorComponent(), 0, 0, 0);
        }

        public HaunterMirrorAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new HaunterMirrorDeed();

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

    public class HaunterMirrorDeed : BaseAddonDeed
    {
        [Constructible]
        public HaunterMirrorDeed() => LootType = LootType.Blessed;

        public HaunterMirrorDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new HaunterMirrorAddon();
        public override int LabelNumber => 1074800; // Haunted Mirror

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
