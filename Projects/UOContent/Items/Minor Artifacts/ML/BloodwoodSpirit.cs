namespace Server.Items
{
    public class BloodwoodSpirit : BaseTalisman
    {
        [Constructible]
        public BloodwoodSpirit() : base(0x2F5A)
        {
            Hue = 0x27;
            MaxChargeTime = 1200;

            Removal = TalismanRemoval.Damage;
            Blessed = GetRandomBlessed();
            Protection = GetRandomProtection(false);

            SkillBonuses.SetValues(0, SkillName.SpiritSpeak, 10.0);
            SkillBonuses.SetValues(1, SkillName.Necromancy, 5.0);
        }

        public BloodwoodSpirit(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075034; // Bloodwood Spirit
        public override bool ForceShowName => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version == 0 && Protection?.IsEmpty != false)
            {
                Protection = GetRandomProtection(false);
            }
        }
    }
}
