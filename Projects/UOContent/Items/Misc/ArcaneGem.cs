using System;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArcaneGem : Item
{
    public const int DefaultArcaneHue = 2117;

    [Constructible]
    public ArcaneGem() : base(0x1EA7)
    {
        Stackable = Core.ML;
        Weight = 1.0;
    }

    public override string DefaultName => "arcane gem";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else
        {
            from.BeginTarget(2, false, TargetFlags.None, OnTarget);
            from.SendMessage("What do you wish to use the gem on?");
        }
    }

    public static int GetChargesFor(Mobile m) => Math.Clamp((int)(m.Skills.Tailoring.Value / 5), 16, 24);

    public void OnTarget(Mobile from, object obj)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (obj is IArcaneEquip eq and Item item)
        {
            var clothing = item as BaseClothing;
            var armor = item as BaseArmor;
            var weapon = item as BaseWeapon;

            var resource = clothing?.Resource ?? armor?.Resource ?? weapon?.Resource ?? CraftResource.None;

            if (!item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (item.LootType == LootType.Blessed)
            {
                from.SendMessage(
                    "You can only use this on exceptionally crafted robes, thigh boots, cloaks, or leather gloves."
                );
                return;
            }

            if (resource != CraftResource.None && resource != CraftResource.RegularLeather)
            {
                from.SendLocalizedMessage(1049690); // Arcane gems can not be used on that type of leather.
                return;
            }

            var charges = GetChargesFor(from);

            if (eq.IsArcane)
            {
                if (eq.CurArcaneCharges >= eq.MaxArcaneCharges)
                {
                    from.SendMessage("That item is already fully charged.");
                }
                else
                {
                    if (eq.CurArcaneCharges <= 0)
                    {
                        item.Hue = DefaultArcaneHue;
                    }

                    if (eq.CurArcaneCharges + charges > eq.MaxArcaneCharges)
                    {
                        eq.CurArcaneCharges = eq.MaxArcaneCharges;
                    }
                    else
                    {
                        eq.CurArcaneCharges += charges;
                    }

                    from.SendMessage("You recharge the item.");
                    if (Amount <= 1)
                    {
                        Delete();
                    }
                    else
                    {
                        Amount--;
                    }
                }
            }
            else if (from.Skills.Tailoring.Value >= 80.0)
            {
                var isExceptional = clothing?.Quality == ClothingQuality.Exceptional ||
                                    armor?.Quality == ArmorQuality.Exceptional ||
                                    weapon?.Quality == WeaponQuality.Exceptional;

                if (isExceptional)
                {
                    if (clothing != null)
                    {
                        clothing.Quality = ClothingQuality.Regular;
                        clothing.Crafter = from.RawName;
                    }
                    else if (armor != null)
                    {
                        armor.Quality = ArmorQuality.Regular;
                        armor.Crafter = from.RawName;
                        armor.PhysicalBonus =
                            armor.FireBonus =
                                armor.ColdBonus =
                                    armor.PoisonBonus = armor.EnergyBonus = 0; // Is there a method to remove bonuses?
                    }
                    else
                    {
                        weapon.Quality = WeaponQuality.Regular;
                        weapon.Crafter = from.RawName;
                    }

                    eq.CurArcaneCharges = eq.MaxArcaneCharges = charges;

                    item.Hue = DefaultArcaneHue;

                    from.SendMessage("You enhance the item with your gem.");
                    if (Amount <= 1)
                    {
                        Delete();
                    }
                    else
                    {
                        Amount--;
                    }
                }
                else
                {
                    from.SendMessage("Only exceptional items can be enhanced with the gem.");
                }
            }
            else
            {
                from.SendMessage("You do not have enough skill in tailoring to enhance the item.");
            }
        }
        else
        {
            from.SendMessage(
                "You can only use this on exceptionally crafted robes, thigh boots, cloaks, or leather gloves."
            );
        }
    }

    public static bool ConsumeCharges(Mobile from, int amount)
    {
        var items = from.Items;
        var avail = 0;

        for (var i = 0; i < items.Count; ++i)
        {
            var obj = items[i];

            if (obj is IArcaneEquip eq && eq.IsArcane)
            {
                avail += eq.CurArcaneCharges;
            }
        }

        if (avail < amount)
        {
            return false;
        }

        for (var i = 0; i < items.Count; ++i)
        {
            var obj = items[i];

            if (obj is IArcaneEquip eq && eq.IsArcane)
            {
                if (eq.CurArcaneCharges > amount)
                {
                    eq.CurArcaneCharges -= amount;
                    break;
                }

                amount -= eq.CurArcaneCharges;
                eq.CurArcaneCharges = 0;
            }
        }

        return true;
    }
}
