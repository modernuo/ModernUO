namespace Server.Items
{
    public class CurseWeaponScroll : SpellScroll
    {
        [Constructible]
        public CurseWeaponScroll(int amount = 1) : base(103, 0x2263, amount)
        {
        }

        public CurseWeaponScroll(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
