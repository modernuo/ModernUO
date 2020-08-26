namespace Server.Items
{
    public class BackpackArtifact : BaseDecorationContainerArtifact
    {
        [Constructible]
        public BackpackArtifact() : base(0x9B2)
        {
        }

        public BackpackArtifact(Serial serial) : base(serial)
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

    public class BloodyWaterArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BloodyWaterArtifact() : base(0xE23)
        {
        }

        public BloodyWaterArtifact(Serial serial) : base(serial)
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

    public class BooksWestArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BooksWestArtifact() : base(0x1E25)
        {
        }

        public BooksWestArtifact(Serial serial) : base(serial)
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

    public class BooksNorthArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BooksNorthArtifact() : base(0x1E24)
        {
        }

        public BooksNorthArtifact(Serial serial) : base(serial)
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

    public class BooksFaceDownArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BooksFaceDownArtifact() : base(0x1E21)
        {
        }

        public BooksFaceDownArtifact(Serial serial) : base(serial)
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

    public class BottleArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BottleArtifact() : base(0xE28)
        {
        }

        public BottleArtifact(Serial serial) : base(serial)
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

    public class BrazierArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public BrazierArtifact() : base(0xE31) => Light = LightType.Circle150;

        public BrazierArtifact(Serial serial) : base(serial)
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

    public class CocoonArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public CocoonArtifact() : base(0x10DA)
        {
        }

        public CocoonArtifact(Serial serial) : base(serial)
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

    public class DamagedBooksArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public DamagedBooksArtifact() : base(0xC16)
        {
        }

        public DamagedBooksArtifact(Serial serial) : base(serial)
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

    public class EggCaseArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public EggCaseArtifact() : base(0x10D9)
        {
        }

        public EggCaseArtifact(Serial serial) : base(serial)
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

    public class GruesomeStandardArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public GruesomeStandardArtifact() : base(0x428)
        {
        }

        public GruesomeStandardArtifact(Serial serial) : base(serial)
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

    public class LampPostArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public LampPostArtifact() : base(0xB24) => Light = LightType.Circle300;

        public LampPostArtifact(Serial serial) : base(serial)
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

    public class LeatherTunicArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public LeatherTunicArtifact() : base(0x13CA)
        {
        }

        public LeatherTunicArtifact(Serial serial) : base(serial)
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

    public class RockArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public RockArtifact() : base(0x1363)
        {
        }

        public RockArtifact(Serial serial) : base(serial)
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

    public class RuinedPaintingArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public RuinedPaintingArtifact() : base(0xC2C)
        {
        }

        public RuinedPaintingArtifact(Serial serial) : base(serial)
        {
        }

        public override int ArtifactRarity => 12;

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

    public class SaddleArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SaddleArtifact() : base(0xF38)
        {
        }

        public SaddleArtifact(Serial serial) : base(serial)
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

    public class SkinnedDeerArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SkinnedDeerArtifact() : base(0x1E91)
        {
        }

        public SkinnedDeerArtifact(Serial serial) : base(serial)
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

    public class SkinnedGoatArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SkinnedGoatArtifact() : base(0x1E88)
        {
        }

        public SkinnedGoatArtifact(Serial serial) : base(serial)
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

    public class SkullCandleArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public SkullCandleArtifact() : base(0x1858) => Light = LightType.Circle150;

        public SkullCandleArtifact(Serial serial) : base(serial)
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

    public class StretchedHideArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public StretchedHideArtifact() : base(0x106B)
        {
        }

        public StretchedHideArtifact(Serial serial) : base(serial)
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

    public class StuddedLeggingsArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public StuddedLeggingsArtifact() : base(0x13D8)
        {
        }

        public StuddedLeggingsArtifact(Serial serial) : base(serial)
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

    public class StuddedTunicArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public StuddedTunicArtifact() : base(0x13D9)
        {
        }

        public StuddedTunicArtifact(Serial serial) : base(serial)
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

    public class TarotCardsArtifact : BaseDecorationArtifact
    {
        [Constructible]
        public TarotCardsArtifact() : base(0x12A5)
        {
        }

        public TarotCardsArtifact(Serial serial) : base(serial)
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
}
