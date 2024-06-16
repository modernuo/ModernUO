using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
    public class FactionItemDefinition
    {
        private static readonly FactionItemDefinition m_MetalArmor = new(1000, typeof(Blacksmith));
        private static readonly FactionItemDefinition m_Weapon = new(1000, typeof(Blacksmith));
        private static readonly FactionItemDefinition m_RangedWeapon = new(1000, typeof(Bowyer));
        private static readonly FactionItemDefinition m_LeatherArmor = new(750, typeof(Tailor));
        private static readonly FactionItemDefinition m_Clothing = new(200, typeof(Tailor));
        private static readonly FactionItemDefinition m_Scroll = new(500, typeof(Mage));

        public FactionItemDefinition(int silverCost, Type vendorType)
        {
            SilverCost = silverCost;
            VendorType = vendorType;
        }

        public int SilverCost { get; }

        public Type VendorType { get; }

        public static FactionItemDefinition Identify(Item item)
        {
            return item switch
            {
                BaseArmor armor => CraftResources.GetType(armor.Resource) == CraftResourceType.Leather
                    ? m_LeatherArmor
                    : m_MetalArmor,
                BaseRanged => m_RangedWeapon,
                BaseWeapon => m_Weapon,
                BaseClothing => m_Clothing,
                SpellScroll => m_Scroll,
                _ => null
            };
        }
    }
}
