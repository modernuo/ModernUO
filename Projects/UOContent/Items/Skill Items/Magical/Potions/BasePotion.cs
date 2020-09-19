using System;
using Server.Engines.ConPVP;
using Server.Engines.Craft;
using Server.Utilities;

namespace Server.Items
{
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

    public abstract class BasePotion : Item, ICraftable, ICommodity
    {
        private PotionEffect m_PotionEffect;

        public BasePotion(int itemID, PotionEffect effect) : base(itemID)
        {
            m_PotionEffect = effect;

            Stackable = Core.ML;
            Weight = 1.0;
        }

        public BasePotion(Serial serial) : base(serial)
        {
        }

        public PotionEffect PotionEffect
        {
            get => m_PotionEffect;
            set
            {
                m_PotionEffect = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1041314 + (int)m_PotionEffect;

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

                    var kegs = pack.FindItemsByType<PotionKeg>();

                    for (var i = 0; i < kegs.Count; ++i)
                    {
                        var keg = kegs[i];

                        // Should never happen
                        //            if (keg == null)
                        //              continue;

                        if (keg.Held <= 0 || keg.Held >= 100)
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

            if (from.InRange(GetWorldLocation(), 1))
            {
                if (!RequireFreeHand || HasFreeHand(from))
                {
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
                else
                {
                    from.SendLocalizedMessage(502172); // You must have a free hand to drink a potion.
                }
            }
            else
            {
                from.SendLocalizedMessage(502138); // That is too far away for you to use
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write((int)m_PotionEffect);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_PotionEffect = (PotionEffect)reader.ReadInt();
                        break;
                    }
            }

            if (version == 0)
            {
                Stackable = Core.ML;
            }
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
            var skillBonus = m.Skills.Alchemy.Fixed / 330 * 10;

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

            var scalar = 1.0 + 0.01 * EnhancePotions(m);

            return TimeSpan.FromSeconds(v.TotalSeconds * scalar);
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

        public static int Scale(Mobile m, int v)
        {
            if (!Core.AOS)
            {
                return v;
            }

            return AOS.Scale(v, 100 + EnhancePotions(m));
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound) =>
            dropped is BasePotion potion && potion.m_PotionEffect == m_PotionEffect &&
            base.StackWith(from, potion, playSound);
    }
}
