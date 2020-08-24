using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
    public class FactionItemDefinition
    {
        private static readonly FactionItemDefinition m_MetalArmor = new FactionItemDefinition(1000, typeof(Blacksmith));
        private static readonly FactionItemDefinition m_Weapon = new FactionItemDefinition(1000, typeof(Blacksmith));
        private static readonly FactionItemDefinition m_RangedWeapon = new FactionItemDefinition(1000, typeof(Bowyer));
        private static readonly FactionItemDefinition m_LeatherArmor = new FactionItemDefinition(750, typeof(Tailor));
        private static readonly FactionItemDefinition m_Clothing = new FactionItemDefinition(200, typeof(Tailor));
        private static readonly FactionItemDefinition m_Scroll = new FactionItemDefinition(500, typeof(Mage));

        public FactionItemDefinition(int silverCost, Type vendorType)
        {
            SilverCost = silverCost;
            VendorType = vendorType;
        }

        public int SilverCost { get; }

        public Type VendorType { get; }

        public static FactionItemDefinition Identify(Item item)
        {
            if (item is BaseArmor armor)
            {
                if (CraftResources.GetType(armor.Resource) == CraftResourceType.Leather)
                    return m_LeatherArmor;

                return m_MetalArmor;
            }

            if (item is BaseRanged)
                return m_RangedWeapon;
            if (item is BaseWeapon)
                return m_Weapon;
            if (item is BaseClothing)
                return m_Clothing;
            if (item is SpellScroll)
                return m_Scroll;

            return null;
        }
    }
}
