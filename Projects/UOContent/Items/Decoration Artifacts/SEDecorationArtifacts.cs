using Server.Network;

namespace Server.Items
{
    public class Basket1Artifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket1Artifact() : base(0x24DD)
        {
        }

        public Basket1Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 1;

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

    public class Basket2Artifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket2Artifact() : base(0x24D7)
        {
        }

        public Basket2Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 1;

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

    public class Basket3WestArtifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket3WestArtifact() : base(0x24D9)
        {
        }

        public Basket3WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 1;

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

    public class Basket3NorthArtifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket3NorthArtifact() : base(0x24DA)
        {
        }

        public Basket3NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 1;

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

    public class Basket4Artifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket4Artifact() : base(0x24D8)
        {
        }

        public Basket4Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 2;

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

    public class Basket5WestArtifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket5WestArtifact() : base(0x24DC)
        {
        }

        public Basket5WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 2;

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

    public class Basket5NorthArtifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket5NorthArtifact() : base(0x24DB)
        {
        }

        public Basket5NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 2;

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

    public class Basket6Artifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public Basket6Artifact() : base(0x24D5)
        {
        }

        public Basket6Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 2;

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

    public class BowlArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BowlArtifact() : base(0x24DE)
        {
        }

        public BowlArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class BowlsVerticalArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BowlsVerticalArtifact() : base(0x24DF)
        {
        }

        public BowlsVerticalArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class BowlsHorizontalArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BowlsHorizontalArtifact() : base(0x24E0)
        {
        }

        public BowlsHorizontalArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class CupsArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public CupsArtifact() : base(0x24E1)
        {
        }

        public CupsArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class FanWestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public FanWestArtifact() : base(0x240A)
        {
        }

        public FanWestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class FanNorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public FanNorthArtifact() : base(0x2409)
        {
        }

        public FanNorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class TripleFanWestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public TripleFanWestArtifact() : base(0x240C)
        {
        }

        public TripleFanWestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class TripleFanNorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public TripleFanNorthArtifact() : base(0x240B)
        {
        }

        public TripleFanNorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class FlowersArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public FlowersArtifact() : base(0x284A)
        {
        }

        public FlowersArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 7;

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

    public class Painting1WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting1WestArtifact() : base(0x240E)
        {
        }

        public Painting1WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class Painting1NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting1NorthArtifact() : base(0x240D)
        {
        }

        public Painting1NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class Painting2WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting2WestArtifact() : base(0x2410)
        {
        }

        public Painting2WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class Painting2NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting2NorthArtifact() : base(0x240F)
        {
        }

        public Painting2NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class Painting3Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting3Artifact() : base(0x2411)
        {
        }

        public Painting3Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 5;

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

    public class Painting4WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting4WestArtifact() : base(0x2412)
        {
        }

        public Painting4WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 6;

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

    public class Painting4NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting4NorthArtifact() : base(0x2411)
        {
        }

        public Painting4NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 6;

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

    public class Painting5WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting5WestArtifact() : base(0x2416)
        {
        }

        public Painting5WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class Painting5NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting5NorthArtifact() : base(0x2415)
        {
        }

        public Painting5NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class Painting6WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting6WestArtifact() : base(0x2418)
        {
        }

        public Painting6WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class Painting6NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public Painting6NorthArtifact() : base(0x2417)
        {
        }

        public Painting6NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class SakeArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SakeArtifact() : base(0x24E2)
        {
        }

        public SakeArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 4;

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

    public class Sculpture1Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public Sculpture1Artifact() : base(0x2419)
        {
        }

        public Sculpture1Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class Sculpture2Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public Sculpture2Artifact() : base(0x241B)
        {
        }

        public Sculpture2Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class DolphinLeftArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public DolphinLeftArtifact() : base(0x2846)
        {
        }

        public DolphinLeftArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class DolphinRightArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public DolphinRightArtifact() : base(0x2847)
        {
        }

        public DolphinRightArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class ManStatuetteSouthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public ManStatuetteSouthArtifact() : base(0x2848)
        {
        }

        public ManStatuetteSouthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class ManStatuetteEastArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public ManStatuetteEastArtifact() : base(0x2849)
        {
        }

        public ManStatuetteEastArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class SwordDisplay1WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay1WestArtifact() : base(0x2842)
        {
        }

        public SwordDisplay1WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 5;

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

    public class SwordDisplay1NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay1NorthArtifact() : base(0x2843)
        {
        }

        public SwordDisplay1NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 5;

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

    public class SwordDisplay2WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay2WestArtifact() : base(0x2844)
        {
        }

        public SwordDisplay2WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 6;

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

    public class SwordDisplay2NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay2NorthArtifact() : base(0x2845)
        {
        }

        public SwordDisplay2NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 6;

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

    public class SwordDisplay3SouthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay3SouthArtifact() : base(0x2855)
        {
        }

        public SwordDisplay3SouthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class SwordDisplay3EastArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay3EastArtifact() : base(0x2856)
        {
        }

        public SwordDisplay3EastArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class SwordDisplay4WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay4WestArtifact() : base(0x2853)
        {
        }

        public SwordDisplay4WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 8;

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

    public class SwordDisplay4NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay4NorthArtifact() : base(0x2854)
        {
        }

        public SwordDisplay4NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class SwordDisplay5WestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay5WestArtifact() : base(0x2851)
        {
        }

        public SwordDisplay5WestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class SwordDisplay5NorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SwordDisplay5NorthArtifact() : base(0x2852)
        {
        }

        public SwordDisplay5NorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 9;

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

    public class TeapotWestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public TeapotWestArtifact() : base(0x24E7)
        {
        }

        public TeapotWestArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class TeapotNorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public TeapotNorthArtifact() : base(0x24E6)
        {
        }

        public TeapotNorthArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class TowerLanternArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public TowerLanternArtifact() : base(0x24C0) => Light = LightType.Circle225;

        public TowerLanternArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsOn
        {
            get => ItemID == 0x24BF;
            set => ItemID = value ? 0x24BF : 0x24C0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                if (IsOn)
                {
                    IsOn = false;
                    from.PlaySound(0x3BE);
                }
                else
                {
                    IsOn = true;
                    from.PlaySound(0x47);
                }
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version == 0)
            {
                Light = LightType.Circle225;
            }
        }
    }

    public class Urn1Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public Urn1Artifact() : base(0x241D)
        {
        }

        public Urn1Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class Urn2Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public Urn2Artifact() : base(0x241E)
        {
        }

        public Urn2Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class ZenRock1Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public ZenRock1Artifact() : base(0x24E4)
        {
        }

        public ZenRock1Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 2;

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

    public class ZenRock2Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public ZenRock2Artifact() : base(0x24E3)
        {
        }

        public ZenRock2Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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

    public class ZenRock3Artifact : BaseDecorationArtifact
    {
        [Constructible]
        public ZenRock3Artifact() : base(0x24E5)
        {
        }

        public ZenRock3Artifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 3;

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
