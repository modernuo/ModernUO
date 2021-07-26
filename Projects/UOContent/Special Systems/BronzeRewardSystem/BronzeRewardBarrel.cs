using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Items
{
    public class BronzeRewardBarrel : Item
    {
        int prize;
        [Constructible]
        public BronzeRewardBarrel() : base(0xE77)
        {
            Name = "trash reward barrel";
            Movable = false;
            Hue = 2101;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped.LootType == LootType.Blessed || dropped.LootType == LootType.Newbied)
            {
                from.SendMessage(38, "You can not trash blessed or newbied items!");
                return false;
            }
            else
            {
                if (dropped is BaseWeapon || dropped is BaseArmor)
                {
                    prize = GetSellPriceFor(dropped);
                    from.AddToBackpack(new BronzeRewardToken(prize));
                    from.SendMessage(53, "You have been rewarded with {0} bronze coins for your trash!", prize);
                    dropped.Delete();
                    return true;
                }
                else
                {
                    from.SendMessage(38, "This barrel only accepts weapons and armour parts!");
                    return false;
                }
            }
        }

        public int GetSellPriceFor(Item item)
        {
            int price = 1;

            if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                price += 24 * (int)armor.Durability; // 98 
                price += 24 * (int)armor.ProtectionLevel; // 98

                if (armor.Quality == ArmorQuality.Low)
                    price = (int)(price * 0.60);
                else if (armor.Quality == ArmorQuality.Exceptional)
                    price = (int)(price * 1.25);

                if (price < 2)
                    price = 2;

                if (price > 1000)
                    price = 0;
            }
            else if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                price += 12 * (int)weapon.DurabilityLevel; // 49
                price += 22 * (int)weapon.DamageLevel; // 89
                price += 17 * (int)weapon.AccuracyLevel; // 69

                if (weapon.Quality == WeaponQuality.Low)
                    price = (int)(price * 0.60);
                else if (weapon.Quality == WeaponQuality.Exceptional)
                    price = (int)(price * 1.25);

                if (price < 2)
                    price = 2;

                if (price > 1000)
                    price = 0;
            }
            return price;
        }

        public BronzeRewardBarrel(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
