namespace Server.Items
{
    public class ZyronicClaw : ExecutionersAxe
    {
        [Constructible]
        public ZyronicClaw()
        {
            Hue = 0x485;
            Slayer = SlayerName.ElementalBan;
            WeaponAttributes.HitLeechMana = 50;
            Attributes.AttackChance = 30;
            Attributes.WeaponDamage = 50;
        }

        public ZyronicClaw(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061593; // Zyronic Claw
        public override int ArtifactRarity => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            chaos = direct = 0;
            phys = fire = cold = pois = nrgy = 20;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Slayer == SlayerName.None)
            {
                Slayer = SlayerName.ElementalBan;
            }
        }
    }
}
