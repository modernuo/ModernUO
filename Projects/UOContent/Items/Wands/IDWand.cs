using System;

namespace Server.Items
{
    public class IDWand : BaseWand
    {
        [Constructible]
        public IDWand() : base(WandEffect.Identification, 25, 175)
        {
        }

        public IDWand(Serial serial) : base(serial)
        {
        }

        public override TimeSpan GetUseDelay => TimeSpan.Zero;

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

        public override bool OnWandTarget(Mobile from, object o)
        {
            if (o is Item item)
            {
                if (item is BaseWeapon weapon)
                {
                    weapon.Identified = true;
                }
                else if (item is BaseArmor armor)
                {
                    armor.Identified = true;
                }

                if (!Core.AOS)
                {
                    item.OnSingleClick(from);
                }

                return true;
            }

            return false;
        }
    }
}
