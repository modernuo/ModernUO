namespace Server.Items
{
    public class ArcaneShield : WoodenKiteShield
    {
        [Constructible]
        public ArcaneShield()
        {
            ItemID = 0x1B78;
            Hue = 0x556;
            Attributes.NightSight = 1;
            Attributes.SpellChanneling = 1;
            Attributes.DefendChance = 15;
            Attributes.CastSpeed = 1;
        }

        public ArcaneShield(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061101; // Arcane Shield
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Attributes.NightSight == 0)
            {
                Attributes.NightSight = 1;
            }
        }
    }
}
