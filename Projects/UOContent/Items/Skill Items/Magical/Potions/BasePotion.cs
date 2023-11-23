using System;
using ModernUO.Serialization;
using Server.Engines.ConPVP;
using Server.Engines.Craft;
using Server.Utilities;

namespace Server.Items;

public enum PotionEffect
{
    Nightsight,
    CureLesser,
    Cure,
    CureGreater,
    Agility,
    AgilityGreater,
    Strength,
    StrengthGreater,
    PoisonLesser,
    Poison,
    PoisonGreater,
    PoisonDeadly,
    Refresh,
    RefreshTotal,
    HealLesser,
    Heal,
    HealGreater,
    ExplosionLesser,
    Explosion,
    ExplosionGreater,
    Conflagration,
    ConflagrationGreater,
    MaskOfDeath,        // Mask of Death is not available in OSI but does exist in cliloc files
    MaskOfDeathGreater, // included in enumeration for compatibility if later enabled by OSI
    ConfusionBlast,
    ConfusionBlastGreater,
    Invisibility,
    Parasitic,
    Darkglow
}

[SerializationGenerator(2, false)]
public abstract partial class BasePotion : Item, ICraftable, ICommodity
{
    [InvalidateProperties]
    [SerializableField(0)]
    private PotionEffect _potionEffect;

    public BasePotion(int itemID, PotionEffect effect) : base(itemID)
    {
        _potionEffect = effect;

        Stackable = Core.ML;
        Weight = 1.0;
    }

    public override int LabelNumber => 1041314 + (int)_potionEffect;

    public virtual bool RequireFreeHand => true;

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

    public int OnCraft(
        int quality,
        bool makersMark,
        Mobile from,
        CraftSystem craftSystem,
        Type typeRes,
        BaseTool tool,
        CraftItem craftItem,
        int resHue
    )
    {
        if (craftSystem is DefAlchemy)
        {
            var pack = from.Backpack;

            if (pack != null)
            {
                if ((int)PotionEffect >= (int)PotionEffect.Invisibility)
                {
                    return 1;
                }

                foreach (var keg in pack.EnumerateItemsByType<PotionKeg>())
                {
                    if (keg.Held is <= 0 or >= 100)
                    {
                        continue;
                    }

                    if (keg.Type != PotionEffect)
                    {
                        continue;
                    }

                    ++keg.Held;

                    Consume();
                    from.AddToBackpack(new Bottle());

                    return -1; // signal placed in keg
                }
            }
        }

        return 1;
    }

    public static bool HasFreeHand(Mobile m)
    {
        var handOne = m.FindItemOnLayer(Layer.OneHanded);
        var handTwo = m.FindItemOnLayer(Layer.TwoHanded);

        if (handTwo is BaseWeapon)
        {
            handOne = handTwo;
        }

        if (handTwo is BaseRanged ranged && ranged.Balanced)
        {
            return true;
        }

        return handOne == null || handTwo == null;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!Movable)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(502138); // That is too far away for you to use
            return;
        }

        if (RequireFreeHand && !HasFreeHand(from))
        {
            from.SendLocalizedMessage(502172); // You must have a free hand to drink a potion.
            return;
        }

        if (this is BaseExplosionPotion && Amount > 1)
        {
            var pot = GetType().CreateInstance<BaseExplosionPotion>();

            Amount--;

            if (from.Backpack?.Deleted == false)
            {
                from.Backpack.DropItem(pot);
            }
            else
            {
                pot.MoveToWorld(from.Location, from.Map);
            }

            pot.Drink(from);
        }
        else
        {
            Drink(from);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _potionEffect = (PotionEffect)reader.ReadInt();
    }

    public abstract void Drink(Mobile from);

    public static void PlayDrinkEffect(Mobile m)
    {
        m.RevealingAction();

        m.PlaySound(0x2D6);

        if (!DuelContext.IsFreeConsume(m))
        {
            m.AddToBackpack(new Bottle());
        }

        if (m.Body.IsHuman && !m.Mounted)
        {
            m.Animate(34, 5, 1, true, false, 0);
        }
    }

    public static int EnhancePotions(Mobile m)
    {
        var EP = AosAttributes.GetValue(m, AosAttribute.EnhancePotions);
        var skillBonus = (int)(m.Skills.Alchemy.Value * 10 / 33);

        if (Core.ML && EP > 50 && m.AccessLevel <= AccessLevel.Player)
        {
            EP = 50;
        }

        return EP + skillBonus;
    }

    public static TimeSpan Scale(Mobile m, TimeSpan v)
    {
        if (!Core.AOS)
        {
            return v;
        }

        return v * (1.0 + 0.01 * EnhancePotions(m));
    }

    public static double Scale(Mobile m, double v)
    {
        if (!Core.AOS)
        {
            return v;
        }

        var scalar = 1.0 + 0.01 * EnhancePotions(m);

        return v * scalar;
    }

    public static int Scale(Mobile m, int v) => !Core.AOS ? v : AOS.Scale(v, 100 + EnhancePotions(m));

    public override bool StackWith(Mobile from, Item dropped, bool playSound) =>
        dropped is BasePotion potion && potion._potionEffect == _potionEffect &&
        base.StackWith(from, potion, playSound);
}
