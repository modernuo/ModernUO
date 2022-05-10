using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public enum WandEffect
    {
        Clumsiness,
        Identification,
        Healing,
        Feeblemindedness,
        Weakness,
        MagicArrow,
        Harming,
        Fireball,
        GreaterHealing,
        Lightning,
        ManaDraining
    }

    [SerializationGenerator(1, false)]
    public abstract partial class BaseWand : BaseBashing
    {
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        [SerializableField(0)]
        private WandEffect _wandEffect;

        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        [SerializableField(1)]
        private int _charges;

        public BaseWand(WandEffect effect, int minCharges, int maxCharges) : base(0xDF2 + Utility.Random(4))
        {
            Weight = 1.0;
            _wandEffect = effect;
            _charges = Utility.RandomMinMax(minCharges, maxCharges);
            Attributes.SpellChanneling = 1;
            Attributes.CastSpeed = -1;
            WeaponAttributes.MageWeapon = Utility.RandomMinMax(1, 10);
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.Dismount;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 5;
        public override int AosMinDamage => 9;
        public override int AosMaxDamage => 11;
        public override int AosSpeed => 40;
        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 0;
        public override int OldMinDamage => 2;
        public override int OldMaxDamage => 6;
        public override int OldSpeed => 35;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public virtual TimeSpan GetUseDelay => TimeSpan.FromSeconds(4.0);

        public void ConsumeCharge(Mobile from)
        {
            --Charges;

            if (_charges == 0)
            {
                from.SendLocalizedMessage(1019073); // This item is out of charges.
            }

            ApplyDelayTo(from);
        }

        public virtual void ApplyDelayTo(Mobile from)
        {
            from.BeginAction<BaseWand>();
            Timer.StartTimer(GetUseDelay,
                () =>
                {
                    from.EndAction<BaseWand>();
                    ReleaseWandLock_Callback(from);
                }
            );
        }

        public virtual void ReleaseWandLock_Callback(Mobile state)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.CanBeginAction<BaseWand>())
            {
                from.SendLocalizedMessage(1070860); // You must wait a moment for the wand to recharge.
                return;
            }

            if (Parent == from)
            {
                if (_charges > 0)
                {
                    OnWandUse(from);
                }
                else
                {
                    from.SendLocalizedMessage(1019073); // This item is out of charges.
                }
            }
            else
            {
                from.SendLocalizedMessage(502641); // You must equip this item to use it.
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _wandEffect = (WandEffect)reader.ReadInt();
            _charges = reader.ReadInt();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            switch (_wandEffect)
            {
                case WandEffect.Clumsiness:
                    list.Add(1017326, _charges.ToString());
                    break; // clumsiness charges: ~1_val~
                case WandEffect.Identification:
                    list.Add(1017350, _charges.ToString());
                    break; // identification charges: ~1_val~
                case WandEffect.Healing:
                    list.Add(1017329, _charges.ToString());
                    break; // healing charges: ~1_val~
                case WandEffect.Feeblemindedness:
                    list.Add(1017327, _charges.ToString());
                    break; // feeblemind charges: ~1_val~
                case WandEffect.Weakness:
                    list.Add(1017328, _charges.ToString());
                    break; // weakness charges: ~1_val~
                case WandEffect.MagicArrow:
                    list.Add(1060492, _charges.ToString());
                    break; // magic arrow charges: ~1_val~
                case WandEffect.Harming:
                    list.Add(1017334, _charges.ToString());
                    break; // harm charges: ~1_val~
                case WandEffect.Fireball:
                    list.Add(1060487, _charges.ToString());
                    break; // fireball charges: ~1_val~
                case WandEffect.GreaterHealing:
                    list.Add(1017330, _charges.ToString());
                    break; // greater healing charges: ~1_val~
                case WandEffect.Lightning:
                    list.Add(1060491, _charges.ToString());
                    break; // lightning charges: ~1_val~
                case WandEffect.ManaDraining:
                    list.Add(1017339, _charges.ToString());
                    break; // mana drain charges: ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            var attrs = new List<EquipInfoAttribute>();

            if (DisplayLootType)
            {
                if (LootType == LootType.Blessed)
                {
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                }
                else if (LootType == LootType.Cursed)
                {
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
                }
            }

            if (!Identified)
            {
                attrs.Add(new EquipInfoAttribute(1038000)); // Unidentified
            }
            else
            {
                var num = _wandEffect switch
                {
                    WandEffect.Clumsiness       => 3002011,
                    WandEffect.Identification   => 1044063,
                    WandEffect.Healing          => 3002014,
                    WandEffect.Feeblemindedness => 3002013,
                    WandEffect.Weakness         => 3002018,
                    WandEffect.MagicArrow       => 3002015,
                    WandEffect.Harming          => 3002022,
                    WandEffect.Fireball         => 3002028,
                    WandEffect.GreaterHealing   => 3002039,
                    WandEffect.Lightning        => 3002040,
                    WandEffect.ManaDraining     => 3002041,
                    _                           => 0
                };

                if (num > 0)
                {
                    attrs.Add(new EquipInfoAttribute(num, _charges));
                }
            }

            int number;

            if (Name == null)
            {
                number = 1017085;
            }
            else
            {
                LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
            {
                return;
            }

            from.NetState.SendDisplayEquipmentInfo(Serial, number, Crafter?.RawName, false, attrs);
        }

        public void Cast(Spell spell)
        {
            var m = Movable;

            Movable = false;
            spell.Cast();
            Movable = m;
        }

        public virtual void OnWandUse(Mobile from)
        {
            from.Target = new WandTarget(this);
        }

        public virtual void DoWandTarget(Mobile from, object o)
        {
            if (Deleted || _charges <= 0 || Parent != from || o is StaticTarget or LandTarget)
            {
                return;
            }

            if (OnWandTarget(from, o))
            {
                ConsumeCharge(from);
            }
        }

        public virtual bool OnWandTarget(Mobile from, object o) => true;
    }
}
