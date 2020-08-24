namespace Server.Items
{
    public class AnimatedWeaponScroll : SpellScroll
    {
        [Constructible]
        public AnimatedWeaponScroll(int amount = 1)
            : base(683, 0x2DA4, amount)
        {
        }

        public AnimatedWeaponScroll(Serial serial)
            : base(serial)
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

            /*int version = */
            reader.ReadInt();
        }
    }
}
