using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class IDWand : BaseWand
    {
        [Constructible]
        public IDWand() : base(WandEffect.Identification, 25, 175)
        {
        }

        public override TimeSpan GetUseDelay => TimeSpan.Zero;

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
