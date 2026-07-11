using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Mysticism;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;

namespace Server
{
    public static class AOS
    {
        public const int CastingFocusChanceCap = 12;
        public const int ResonanceChanceCap = 40;
        public const int MassiveStrengthRequirement = 125;

        public static void DisableStatInfluences()
        {
            for (var i = 0; i < SkillInfo.Table.Length; ++i)
            {
                var info = SkillInfo.Table[i];

                info.StrScale = 0.0;
                info.DexScale = 0.0;
                info.IntScale = 0.0;
                info.StatTotal = 0.0;
            }
        }

        public static int Damage(Mobile m, int damage, bool ignoreArmor, int phys, int fire, int cold, int pois, int nrgy) =>
            Damage(m, null, damage, ignoreArmor, phys, fire, cold, pois, nrgy);

        public static int Damage(Mobile m, int damage, int phys, int fire, int cold, int pois, int nrgy) =>
            Damage(m, null, damage, phys, fire, cold, pois, nrgy);

        public static int Damage(Mobile m, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy) =>
            Damage(m, from, damage, false, phys, fire, cold, pois, nrgy);

        public static int Damage(
            Mobile m, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy,
            int chaos
        ) => Damage(m, from, damage, false, phys, fire, cold, pois, nrgy, chaos);

        public static int Damage(
            Mobile m, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy,
            bool keepAlive
        ) => Damage(m, from, damage, false, phys, fire, cold, pois, nrgy, 0, 0, keepAlive);

        public static int Damage(
            Mobile m, Mobile from, int damage, bool ignoreArmor, int phys, int fire, int cold, int pois,
            int nrgy, int chaos = 0, int direct = 0, bool keepAlive = false, bool archer = false, bool deathStrike = false
        )
        {
            if (m?.Deleted != false || !m.Alive || damage <= 0)
            {
                return 0;
            }

            if (phys == 0 && fire == 100 && cold == 0 && pois == 0 && nrgy == 0)
            {
                MeerMage.StopEffect(m, true);
            }

            if (!Core.AOS)
            {
                m.Damage(damage, from);
                return damage;
            }

            Fix(ref phys);
            Fix(ref fire);
            Fix(ref cold);
            Fix(ref pois);
            Fix(ref nrgy);
            Fix(ref chaos);
            Fix(ref direct);

            var damageType = GetDamageType(phys, fire, cold, pois, nrgy, chaos, direct);

            var physicalPostResistDamage = 0;
            var firePostResistDamage = 0;
            var coldPostResistDamage = 0;
            var poisonPostResistDamage = 0;
            var energyPostResistDamage = 0;
            var directPostResistDamage = 0;

            if (Core.ML && chaos > 0)
            {
                switch (Utility.Random(5))
                {
                    case 0:
                        {
                            phys += chaos;
                            break;
                        }
                    case 1:
                        {
                            fire += chaos;
                            break;
                        }
                    case 2:
                        {
                            cold += chaos;
                            break;
                        }
                    case 3:
                        {
                            pois += chaos;
                            break;
                        }
                    case 4:
                        {
                            nrgy += chaos;
                            break;
                        }
                }
            }

            if (ignoreArmor)
            {
                physicalPostResistDamage = damage * phys / 100;
                firePostResistDamage = damage * fire / 100;
                coldPostResistDamage = damage * cold / 100;
                poisonPostResistDamage = damage * pois / 100;
                energyPostResistDamage = damage * nrgy / 100;
                directPostResistDamage = damage * direct / 100;
            }

            BaseQuiver quiver = null;

            if (archer && from != null)
            {
                quiver = from.FindItemOnLayer<BaseQuiver>(Layer.Cloak);
            }

            int totalDamage;

            if (!ignoreArmor)
            {
                // Armor Ignore on OSI ignores all defenses, not just physical.
                var resPhys = m.PhysicalResistance;
                var resFire = m.FireResistance;
                var resCold = m.ColdResistance;
                var resPois = m.PoisonResistance;
                var resNrgy = m.EnergyResistance;

                var fireDamageNumerator = damage * fire * (100 - resFire);

                physicalPostResistDamage = damage * phys * (100 - resPhys) / 10000;
                firePostResistDamage = fireDamageNumerator / 10000;
                coldPostResistDamage = damage * cold * (100 - resCold) / 10000;
                poisonPostResistDamage = damage * pois * (100 - resPois) / 10000;
                energyPostResistDamage = damage * nrgy * (100 - resNrgy) / 10000;

                totalDamage = damage * phys * (100 - resPhys);
                totalDamage += fireDamageNumerator;
                totalDamage += damage * cold * (100 - resCold);
                totalDamage += damage * pois * (100 - resPois);
                totalDamage += damage * nrgy * (100 - resNrgy);
                totalDamage /= 10000;

                if (Core.ML)
                {
                    directPostResistDamage = damage * direct / 100;
                    totalDamage += directPostResistDamage;

                    if (quiver != null)
                    {
                        totalDamage += totalDamage * quiver.DamageIncrease / 100;
                    }
                }

                if (totalDamage < 1)
                {
                    totalDamage = 1;
                }
            }
            else if (Core.ML && m is PlayerMobile && from is PlayerMobile)
            {
                if (quiver != null)
                {
                    damage += damage * quiver.DamageIncrease / 100;
                }

                if (!deathStrike)
                {
                    totalDamage = Math.Min(damage, 35); // Direct Damage cap of 35
                }
                else
                {
                    totalDamage = Math.Min(damage, 70); // Direct Damage cap of 70
                }
            }
            else
            {
                totalDamage = damage;

                if (Core.ML && quiver != null)
                {
                    totalDamage += totalDamage * quiver.DamageIncrease / 100;
                }
            }

            if (from?.Player != true && m.Player && m.Mount is SwampDragon { HasBarding: true } pet)
            {
                var percent = pet.BardingExceptional ? 20 : 10;
                var absorbed = Scale(totalDamage, percent);

                totalDamage -= absorbed;
                pet.BardingHP -= absorbed;

                if (pet.BardingHP < 0)
                {
                    pet.HasBarding = false;
                    pet.BardingHP = 0;

                    m.SendLocalizedMessage(1053031); // Your dragon's barding has been destroyed!
                }
            }

            if (keepAlive && totalDamage > m.Hits)
            {
                totalDamage = m.Hits;
            }

            var bcFrom = from as BaseCreature;

            if (from is { Deleted: false, Alive: true })
            {
                var reflectPhys = AosAttributes.GetValue(m, AosAttribute.ReflectPhysical);
                var reflectPhysAbility = bcFrom
                    ?.GetAbility(MonsterAbilityType.ReflectPhysicalDamage) as ReflectPhysicalDamage;

                if (reflectPhysAbility?.CanTrigger(bcFrom, MonsterAbilityTrigger.CombatAction) == true)
                {
                    reflectPhys += reflectPhysAbility.PercentReflected;
                    m.SendLocalizedMessage(1070844); // The creature repels the attack back at you.
                }

                if (reflectPhys > 0)
                {
                    var reflectDamage = Scale(
                        damage * phys * (100 - (ignoreArmor ? 0 : m.PhysicalResistance)) / 10000,
                        reflectPhys
                    );

                    bcFrom
                        ?.GetAbility(MonsterAbilityType.MagicalBarrier)
                        ?.AlterMeleeDamageFrom(bcFrom, m, ref reflectDamage);

                    if (reflectDamage > 0)
                    {
                        from.Damage(reflectDamage, m);
                    }
                }
            }

            if (totalDamage <= 0)
            {
                return 0;
            }

            if (from != null) // sanity check
            {
                SpellHelper.DoLeech(totalDamage, from, m);
            }

            var oldHits = m.Hits;
            m.Damage(totalDamage, from, damageType);
            var appliedDamage = Math.Max(0, oldHits - m.Hits);

            PurgeMagicSpell.OnMobileDamaged(from, m, appliedDamage);

            DamageEater.OnDamageTaken(
                m,
                appliedDamage,
                physicalPostResistDamage,
                firePostResistDamage,
                coldPostResistDamage,
                poisonPostResistDamage,
                energyPostResistDamage,
                directPostResistDamage
            );

            if (firePostResistDamage > 0 && appliedDamage > 0)
            {
                Swarm.ClearDefender(m);
            }

            BattleLust.OnDamageTaken(m, from, appliedDamage);
            SoulCharge.OnDamageTaken(m, appliedDamage);
            return totalDamage;
        }

        public static void Fix(ref int val)
        {
            if (val < 0)
            {
                val = 0;
            }
        }

        private static DamageType GetDamageType(int phys, int fire, int cold, int pois, int nrgy, int chaos, int direct)
        {
            if (chaos != 0 || direct != 0)
            {
                return DamageType.None;
            }

            return (phys, fire, cold, pois, nrgy) switch
            {
                (100, 0, 0, 0, 0) => DamageType.Physical,
                (0, 100, 0, 0, 0) => DamageType.Fire,
                (0, 0, 100, 0, 0) => DamageType.Cold,
                (0, 0, 0, 100, 0) => DamageType.Poison,
                (0, 0, 0, 0, 100) => DamageType.Energy,
                _                  => DamageType.None
            };
        }

        public static int Scale(int input, int percent) => input * percent / 100;

        public static int GetStatus(Mobile from, int index)
        {
            return index switch
            {
                // TODO: Account for buffs/debuffs
                0  => from.GetMaxResistance(ResistanceType.Physical),
                1  => from.GetMaxResistance(ResistanceType.Fire),
                2  => from.GetMaxResistance(ResistanceType.Cold),
                3  => from.GetMaxResistance(ResistanceType.Poison),
                4  => from.GetMaxResistance(ResistanceType.Energy),
                5  => AosAttributes.GetValue(from, AosAttribute.DefendChance),
                6  => 45,
                7  => AosAttributes.GetValue(from, AosAttribute.AttackChance),
                8  => AosAttributes.GetValue(from, AosAttribute.WeaponSpeed),
                9  => AosAttributes.GetValue(from, AosAttribute.WeaponDamage),
                10 => AosAttributes.GetValue(from, AosAttribute.LowerRegCost),
                11 => AosAttributes.GetValue(from, AosAttribute.SpellDamage),
                12 => AosAttributes.GetValue(from, AosAttribute.CastRecovery),
                13 => AosAttributes.GetValue(from, AosAttribute.CastSpeed),
                14 => AosAttributes.GetValue(from, AosAttribute.LowerManaCost),
                _  => 0
            };
        }
    }

    [Flags]
    public enum AosAttribute
    {
        RegenHits = 0x00000001,
        RegenStam = 0x00000002,
        RegenMana = 0x00000004,
        DefendChance = 0x00000008,
        AttackChance = 0x00000010,
        BonusStr = 0x00000020,
        BonusDex = 0x00000040,
        BonusInt = 0x00000080,
        BonusHits = 0x00000100,
        BonusStam = 0x00000200,
        BonusMana = 0x00000400,
        WeaponDamage = 0x00000800,
        WeaponSpeed = 0x00001000,
        SpellDamage = 0x00002000,
        CastRecovery = 0x00004000,
        CastSpeed = 0x00008000,
        LowerManaCost = 0x00010000,
        LowerRegCost = 0x00020000,
        ReflectPhysical = 0x00040000,
        EnhancePotions = 0x00080000,
        Luck = 0x00100000,
        SpellChanneling = 0x00200000,
        NightSight = 0x00400000,
        IncreasedKarmaLoss = 0x00800000,
        SpellFocusing = 0x01000000
    }

    public interface IAosItem
    {
        public AosAttributes Attributes { get; }
    }

    public sealed class AosAttributes : BaseAttributes
    {
        private Mobile _spellFocusingOwner;
        private Mobile _spellFocusingTarget;
        private int _spellFocusingCount;

        public AosAttributes(Item owner) : base(owner)
        {
        }

        public AosAttributes(Item owner, AosAttributes other) : base(owner, other)
        {
        }

        public int this[AosAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RegenHits
        {
            get => this[AosAttribute.RegenHits];
            set => this[AosAttribute.RegenHits] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RegenStam
        {
            get => this[AosAttribute.RegenStam];
            set => this[AosAttribute.RegenStam] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RegenMana
        {
            get => this[AosAttribute.RegenMana];
            set => this[AosAttribute.RegenMana] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DefendChance
        {
            get => this[AosAttribute.DefendChance];
            set => this[AosAttribute.DefendChance] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AttackChance
        {
            get => this[AosAttribute.AttackChance];
            set => this[AosAttribute.AttackChance] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusStr
        {
            get => this[AosAttribute.BonusStr];
            set => this[AosAttribute.BonusStr] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusDex
        {
            get => this[AosAttribute.BonusDex];
            set => this[AosAttribute.BonusDex] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusInt
        {
            get => this[AosAttribute.BonusInt];
            set => this[AosAttribute.BonusInt] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusHits
        {
            get => this[AosAttribute.BonusHits];
            set => this[AosAttribute.BonusHits] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusStam
        {
            get => this[AosAttribute.BonusStam];
            set => this[AosAttribute.BonusStam] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusMana
        {
            get => this[AosAttribute.BonusMana];
            set => this[AosAttribute.BonusMana] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponDamage
        {
            get => this[AosAttribute.WeaponDamage];
            set => this[AosAttribute.WeaponDamage] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponSpeed
        {
            get => this[AosAttribute.WeaponSpeed];
            set => this[AosAttribute.WeaponSpeed] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpellDamage
        {
            get => this[AosAttribute.SpellDamage];
            set => this[AosAttribute.SpellDamage] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CastRecovery
        {
            get => this[AosAttribute.CastRecovery];
            set => this[AosAttribute.CastRecovery] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CastSpeed
        {
            get => this[AosAttribute.CastSpeed];
            set => this[AosAttribute.CastSpeed] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LowerManaCost
        {
            get => this[AosAttribute.LowerManaCost];
            set => this[AosAttribute.LowerManaCost] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LowerRegCost
        {
            get => this[AosAttribute.LowerRegCost];
            set => this[AosAttribute.LowerRegCost] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ReflectPhysical
        {
            get => this[AosAttribute.ReflectPhysical];
            set => this[AosAttribute.ReflectPhysical] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EnhancePotions
        {
            get => this[AosAttribute.EnhancePotions];
            set => this[AosAttribute.EnhancePotions] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Luck
        {
            get => this[AosAttribute.Luck];
            set => this[AosAttribute.Luck] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpellChanneling
        {
            get => this[AosAttribute.SpellChanneling];
            set => this[AosAttribute.SpellChanneling] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NightSight
        {
            get => this[AosAttribute.NightSight];
            set => this[AosAttribute.NightSight] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int IncreasedKarmaLoss
        {
            get => this[AosAttribute.IncreasedKarmaLoss];
            set => this[AosAttribute.IncreasedKarmaLoss] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpellFocusing
        {
            get => this[AosAttribute.SpellFocusing];
            set
            {
                if (SpellFocusing == value)
                {
                    return;
                }

                this[AosAttribute.SpellFocusing] = value;
                ResetSpellFocusing();
            }
        }

        internal void ResetSpellFocusing()
        {
            _spellFocusingOwner = null;
            _spellFocusingTarget = null;
            _spellFocusingCount = 0;
        }

        internal int GetSpellFocusingOffset(Mobile caster, Mobile target)
        {
            if (_spellFocusingOwner != caster || _spellFocusingTarget?.Deleted != false || !_spellFocusingTarget.Alive)
            {
                ResetSpellFocusing();
                _spellFocusingOwner = caster;
                _spellFocusingTarget = target;
            }
            else if (_spellFocusingTarget != target)
            {
                ResetSpellFocusing();
                _spellFocusingOwner = caster;
                _spellFocusingTarget = target;
            }

            var offset = GetSpellFocusingOffset(_spellFocusingCount, _spellFocusingTarget.Player);

            _spellFocusingCount++;

            if (_spellFocusingCount >= 21)
            {
                ResetSpellFocusing();
            }

            return offset;
        }

        private static int GetSpellFocusingOffset(int castCount, bool playerTarget)
        {
            var offset = castCount < 6 ? -30 + castCount * 6 : (castCount - 5) * 2;
            return Math.Min(offset, playerTarget ? 20 : 30);
        }

        public void GetProperties(IPropertyList list, int damageBonus = 0, int hitChanceBonus = 0, int luckBonus = 0)
        {
            int prop;

            if ((prop = WeaponDamage + damageBonus) != 0)
            {
                list.Add(1060401, prop); // damage increase ~1_val~%
            }

            if ((prop = DefendChance) != 0)
            {
                list.Add(1060408, prop); // defense chance increase ~1_val~%
            }

            if ((prop = BonusDex) != 0)
            {
                list.Add(1060409, prop); // dexterity bonus ~1_val~
            }

            if ((prop = EnhancePotions) != 0)
            {
                list.Add(1060411, prop); // enhance potions ~1_val~%
            }

            if ((prop = CastRecovery) != 0)
            {
                list.Add(1060412, prop); // faster cast recovery ~1_val~
            }

            if ((prop = CastSpeed) != 0)
            {
                list.Add(1060413, prop); // faster casting ~1_val~
            }

            if ((prop = AttackChance + hitChanceBonus) != 0)
            {
                list.Add(1060415, prop); // hit chance increase ~1_val~%
            }

            if ((prop = BonusHits) != 0)
            {
                list.Add(1060431, prop); // hit point increase ~1_val~
            }

            if ((prop = BonusInt) != 0)
            {
                list.Add(1060432, prop); // intelligence bonus ~1_val~
            }

            if ((prop = LowerManaCost) != 0)
            {
                list.Add(1060433, prop); // lower mana cost ~1_val~%
            }

            if ((prop = LowerRegCost) != 0)
            {
                list.Add(1060434, prop); // lower reagent cost ~1_val~%
            }

            if ((prop = Luck + luckBonus) != 0)
            {
                list.Add(1060436, prop); // luck ~1_val~
            }

            if ((prop = BonusMana) != 0)
            {
                list.Add(1060439, prop); // mana increase ~1_val~
            }

            if ((prop = RegenMana) != 0)
            {
                list.Add(1060440, prop); // mana regeneration ~1_val~
            }

            if (NightSight != 0)
            {
                list.Add(1060441); // night sight
            }

            if ((prop = ReflectPhysical) != 0)
            {
                list.Add(1060442, prop); // reflect physical damage ~1_val~%
            }

            if ((prop = RegenStam) != 0)
            {
                list.Add(1060443, prop); // stamina regeneration ~1_val~
            }

            if ((prop = RegenHits) != 0)
            {
                list.Add(1060444, prop); // hit point regeneration ~1_val~
            }

            if (SpellChanneling != 0)
            {
                list.Add(1060482); // spell channeling
            }

            if ((prop = SpellDamage) != 0)
            {
                list.Add(1060483, prop); // spell damage increase ~1_val~%
            }

            if ((prop = BonusStam) != 0)
            {
                list.Add(1060484, prop); // stamina increase ~1_val~
            }

            if ((prop = BonusStr) != 0)
            {
                list.Add(1060485, prop); // strength bonus ~1_val~
            }

            if ((prop = WeaponSpeed) != 0)
            {
                list.Add(1060486, prop); // swing speed increase ~1_val~%
            }

            if (Core.ML && (prop = IncreasedKarmaLoss) != 0)
            {
                list.Add(1075210, prop); // Increased Karma Loss ~1val~%
            }

            if (SpellFocusing != 0)
            {
                list.Add(1150058); // Spell Focusing
            }
        }

        public static int GetValue(Mobile m, AosAttribute attribute)
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var items = m.Items;
            var value = 0;

            for (var i = 0; i < items.Count; ++i)
            {
                var obj = items[i];

                if (obj is BaseWeapon weapon)
                {
                    var attrs = weapon.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }

                    if (attribute == AosAttribute.Luck)
                    {
                        value += weapon.GetLuckBonus();
                    }
                }
                else if (obj is BaseArmor armor)
                {
                    var attrs = armor.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }

                    if (attribute == AosAttribute.Luck)
                    {
                        value += armor.GetLuckBonus();
                    }
                }
                else if (obj is BaseJewel jewel)
                {
                    var attrs = jewel.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
                else if (obj is BaseClothing clothing)
                {
                    var attrs = clothing.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
                else if (obj is Spellbook spellbook)
                {
                    var attrs = spellbook.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
                else if (obj is BaseQuiver quiver)
                {
                    var attrs = quiver.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
                else if (obj is BaseTalisman talisman)
                {
                    var attrs = talisman.Attributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
            }

            return value;
        }

        public override string ToString() => "...";

        public void AddStatBonuses(Mobile to)
        {
            var strBonus = BonusStr;
            var dexBonus = BonusDex;
            var intBonus = BonusInt;

            if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
            {
                var hashCode = GetHashCode();
                if (strBonus != 0)
                {
                    to.AddStatMod(new StatMod(StatType.Str, $"{hashCode}Str", strBonus, TimeSpan.Zero));
                }

                if (dexBonus != 0)
                {
                    to.AddStatMod(new StatMod(StatType.Dex, $"{hashCode}Dex", dexBonus, TimeSpan.Zero));
                }

                if (intBonus != 0)
                {
                    to.AddStatMod(new StatMod(StatType.Int, $"{hashCode}Int", intBonus, TimeSpan.Zero));
                }
            }

            to.CheckStatTimers();
        }

        public void RemoveStatBonuses(Mobile from)
        {
            var serial = Owner.Serial;

            from.RemoveStatMod($"{serial}Str");
            from.RemoveStatMod($"{serial}Dex");
            from.RemoveStatMod($"{serial}Int");

            from.CheckStatTimers();
        }
    }

    [Flags]
    public enum AosWeaponAttribute
    {
        LowerStatReq = 0x00000001,
        SelfRepair = 0x00000002,
        HitLeechHits = 0x00000004,
        HitLeechStam = 0x00000008,
        HitLeechMana = 0x00000010,
        HitLowerAttack = 0x00000020,
        HitLowerDefend = 0x00000040,
        HitMagicArrow = 0x00000080,
        HitHarm = 0x00000100,
        HitFireball = 0x00000200,
        HitLightning = 0x00000400,
        HitDispel = 0x00000800,
        HitColdArea = 0x00001000,
        HitFireArea = 0x00002000,
        HitPoisonArea = 0x00004000,
        HitEnergyArea = 0x00008000,
        HitPhysicalArea = 0x00010000,
        ResistPhysicalBonus = 0x00020000,
        ResistFireBonus = 0x00040000,
        ResistColdBonus = 0x00080000,
        ResistPoisonBonus = 0x00100000,
        ResistEnergyBonus = 0x00200000,
        UseBestSkill = 0x00400000,
        MageWeapon = 0x00800000,
        DurabilityBonus = 0x01000000
    }

    public sealed class AosWeaponAttributes : BaseAttributes
    {
        public AosWeaponAttributes(Item owner) : base(owner)
        {
        }

        public AosWeaponAttributes(Item owner, AosWeaponAttributes other) : base(owner, other)
        {
        }

        public int this[AosWeaponAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LowerStatReq
        {
            get => this[AosWeaponAttribute.LowerStatReq];
            set => this[AosWeaponAttribute.LowerStatReq] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SelfRepair
        {
            get => this[AosWeaponAttribute.SelfRepair];
            set => this[AosWeaponAttribute.SelfRepair] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitLeechHits
        {
            get => this[AosWeaponAttribute.HitLeechHits];
            set => this[AosWeaponAttribute.HitLeechHits] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitLeechStam
        {
            get => this[AosWeaponAttribute.HitLeechStam];
            set => this[AosWeaponAttribute.HitLeechStam] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitLeechMana
        {
            get => this[AosWeaponAttribute.HitLeechMana];
            set => this[AosWeaponAttribute.HitLeechMana] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitLowerAttack
        {
            get => this[AosWeaponAttribute.HitLowerAttack];
            set => this[AosWeaponAttribute.HitLowerAttack] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitLowerDefend
        {
            get => this[AosWeaponAttribute.HitLowerDefend];
            set => this[AosWeaponAttribute.HitLowerDefend] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitMagicArrow
        {
            get => this[AosWeaponAttribute.HitMagicArrow];
            set => this[AosWeaponAttribute.HitMagicArrow] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitHarm
        {
            get => this[AosWeaponAttribute.HitHarm];
            set => this[AosWeaponAttribute.HitHarm] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitFireball
        {
            get => this[AosWeaponAttribute.HitFireball];
            set => this[AosWeaponAttribute.HitFireball] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitLightning
        {
            get => this[AosWeaponAttribute.HitLightning];
            set => this[AosWeaponAttribute.HitLightning] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitDispel
        {
            get => this[AosWeaponAttribute.HitDispel];
            set => this[AosWeaponAttribute.HitDispel] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitColdArea
        {
            get => this[AosWeaponAttribute.HitColdArea];
            set => this[AosWeaponAttribute.HitColdArea] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitFireArea
        {
            get => this[AosWeaponAttribute.HitFireArea];
            set => this[AosWeaponAttribute.HitFireArea] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPoisonArea
        {
            get => this[AosWeaponAttribute.HitPoisonArea];
            set => this[AosWeaponAttribute.HitPoisonArea] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitEnergyArea
        {
            get => this[AosWeaponAttribute.HitEnergyArea];
            set => this[AosWeaponAttribute.HitEnergyArea] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPhysicalArea
        {
            get => this[AosWeaponAttribute.HitPhysicalArea];
            set => this[AosWeaponAttribute.HitPhysicalArea] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResistPhysicalBonus
        {
            get => this[AosWeaponAttribute.ResistPhysicalBonus];
            set => this[AosWeaponAttribute.ResistPhysicalBonus] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResistFireBonus
        {
            get => this[AosWeaponAttribute.ResistFireBonus];
            set => this[AosWeaponAttribute.ResistFireBonus] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResistColdBonus
        {
            get => this[AosWeaponAttribute.ResistColdBonus];
            set => this[AosWeaponAttribute.ResistColdBonus] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResistPoisonBonus
        {
            get => this[AosWeaponAttribute.ResistPoisonBonus];
            set => this[AosWeaponAttribute.ResistPoisonBonus] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResistEnergyBonus
        {
            get => this[AosWeaponAttribute.ResistEnergyBonus];
            set => this[AosWeaponAttribute.ResistEnergyBonus] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UseBestSkill
        {
            get => this[AosWeaponAttribute.UseBestSkill];
            set => this[AosWeaponAttribute.UseBestSkill] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MageWeapon
        {
            get => this[AosWeaponAttribute.MageWeapon];
            set => this[AosWeaponAttribute.MageWeapon] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DurabilityBonus
        {
            get => this[AosWeaponAttribute.DurabilityBonus];
            set => this[AosWeaponAttribute.DurabilityBonus] = value;
        }

        public static int GetValue(Mobile m, AosWeaponAttribute attribute)
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var items = m.Items;
            var value = 0;

            for (var i = 0; i < items.Count; ++i)
            {
                var obj = items[i];

                if (obj is BaseWeapon weapon)
                {
                    var attrs = weapon.WeaponAttributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
                else if (obj is ElvenGlasses glasses)
                {
                    var attrs = glasses._weaponAttributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
            }

            return value;
        }

        public void GetProperties(IPropertyList list)
        {
            int prop;

            if (UseBestSkill != 0)
            {
                list.Add(1060400); // use best weapon skill
            }

            if ((prop = HitColdArea) != 0)
            {
                list.Add(1060416, prop); // hit cold area ~1_val~%
            }

            if ((prop = HitDispel) != 0)
            {
                list.Add(1060417, prop); // hit dispel ~1_val~%
            }

            if ((prop = HitEnergyArea) != 0)
            {
                list.Add(1060418, prop); // hit energy area ~1_val~%
            }

            if ((prop = HitFireArea) != 0)
            {
                list.Add(1060419, prop); // hit fire area ~1_val~%
            }

            if ((prop = HitFireball) != 0)
            {
                list.Add(1060420, prop); // hit fireball ~1_val~%
            }

            if ((prop = HitHarm) != 0)
            {
                list.Add(1060421, prop); // hit harm ~1_val~%
            }

            if ((prop = HitLeechHits) != 0)
            {
                list.Add(1060422, prop); // hit life leech ~1_val~%
            }

            if ((prop = HitLightning) != 0)
            {
                list.Add(1060423, prop); // hit lightning ~1_val~%
            }

            if ((prop = HitLowerAttack) != 0)
            {
                list.Add(1060424, prop); // hit lower attack ~1_val~%
            }

            if ((prop = HitLowerDefend) != 0)
            {
                list.Add(1060425, prop); // hit lower defense ~1_val~%
            }

            if ((prop = HitMagicArrow) != 0)
            {
                list.Add(1060426, prop); // hit magic arrow ~1_val~%
            }

            if ((prop = HitLeechMana) != 0)
            {
                list.Add(1060427, prop); // hit mana leech ~1_val~%
            }

            if ((prop = HitPhysicalArea) != 0)
            {
                list.Add(1060428, prop); // hit physical area ~1_val~%
            }

            if ((prop = HitPoisonArea) != 0)
            {
                list.Add(1060429, prop); // hit poison area ~1_val~%
            }

            if ((prop = HitLeechStam) != 0)
            {
                list.Add(1060430, prop); // hit stamina leech ~1_val~%
            }

            if ((prop = MageWeapon) != 0)
            {
                list.Add(1060438, 30 - prop); // mage weapon -~1_val~ skill
            }

            if ((prop = SelfRepair) != 0)
            {
                list.Add(1060450, prop); // self repair ~1_val~
            }
        }

        public override string ToString() => "...";
    }

    [Flags]
    public enum ExtendedWeaponAttribute
    {
        Bane = 0x00000001,
        BattleLust = 0x00000002,
        HitSparks = 0x00000004,
        BloodDrinker = 0x00000008,
        HitSwarm = 0x00000010,
        SplinteringWeapon = 0x00000020,
        Focus = 0x00000040
    }

    public sealed class ExtendedWeaponAttributes : BaseAttributes
    {
        public ExtendedWeaponAttributes(Item owner) : base(owner)
        {
        }

        public ExtendedWeaponAttributes(Item owner, ExtendedWeaponAttributes other) : base(owner, other)
        {
        }

        public int this[ExtendedWeaponAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Bane
        {
            get => this[ExtendedWeaponAttribute.Bane];
            set => this[ExtendedWeaponAttribute.Bane] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BattleLust
        {
            get => this[ExtendedWeaponAttribute.BattleLust];
            set
            {
                var hadBattleLust = BattleLust != 0;

                this[ExtendedWeaponAttribute.BattleLust] = value;

                if (hadBattleLust && value == 0 && Owner is BaseWeapon { Parent: Mobile m })
                {
                    Server.Items.BattleLust.Clear(m);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitSparks
        {
            get => this[ExtendedWeaponAttribute.HitSparks];
            set => this[ExtendedWeaponAttribute.HitSparks] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BloodDrinker
        {
            get => this[ExtendedWeaponAttribute.BloodDrinker];
            set => this[ExtendedWeaponAttribute.BloodDrinker] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitSwarm
        {
            get => this[ExtendedWeaponAttribute.HitSwarm];
            set => this[ExtendedWeaponAttribute.HitSwarm] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SplinteringWeapon
        {
            get => this[ExtendedWeaponAttribute.SplinteringWeapon];
            set => this[ExtendedWeaponAttribute.SplinteringWeapon] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Focus
        {
            get => this[ExtendedWeaponAttribute.Focus];
            set
            {
                var hadFocus = Focus != 0;

                this[ExtendedWeaponAttribute.Focus] = value;

                if (hadFocus && value == 0 && Owner is BaseWeapon weapon)
                {
                    FocusContext.Clear(weapon);
                }
            }
        }

        public void GetProperties(IPropertyList list)
        {
            if (Core.HS && Bane != 0)
            {
                list.Add(1154671); // Bane
            }

            if (Core.HS && Focus != 0)
            {
                list.Add(1150018); // Focus
            }

            if (Core.SA && BattleLust != 0)
            {
                list.Add(1113710); // Battle Lust
            }

            if (Core.SA && BloodDrinker != 0)
            {
                list.Add(1113591); // Blood Drinker
            }

            if (Core.SA && SplinteringWeapon != 0)
            {
                list.Add(1112857, SplinteringWeapon); // splintering weapon ~1_val~%
            }

            if (Core.TOL && HitSparks != 0)
            {
                list.Add(1157326, HitSparks); // Sparks ~1_val~%
            }

            if (Core.TOL && HitSwarm != 0)
            {
                list.Add(1157325, HitSwarm); // Swarm ~1_val~%
            }
        }

        public override string ToString() => "...";
    }

    [Flags]
    public enum AbsorptionAttribute
    {
        CastingFocus = 0x00000001,
        DamageEater = 0x00000002,
        KineticEater = 0x00000004,
        FireEater = 0x00000008,
        ColdEater = 0x00000010,
        PoisonEater = 0x00000020,
        EnergyEater = 0x00000040,
        FireResonance = 0x00000080,
        ColdResonance = 0x00000100,
        PoisonResonance = 0x00000200,
        EnergyResonance = 0x00000400,
        KineticResonance = 0x00000800
    }

    public sealed class AbsorptionAttributes : BaseAttributes
    {
        public AbsorptionAttributes(Item owner) : base(owner)
        {
        }

        public AbsorptionAttributes(Item owner, AbsorptionAttributes other) : base(owner, other)
        {
        }

        public int this[AbsorptionAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CastingFocus
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.CastingFocus] : 0;
            set => this[AbsorptionAttribute.CastingFocus] = Owner is BaseArmor ? value : 0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamageEater
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.DamageEater] : 0;
            set => SetEaterValue(AbsorptionAttribute.DamageEater, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int KineticEater
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.KineticEater] : 0;
            set => SetEaterValue(AbsorptionAttribute.KineticEater, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FireEater
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.FireEater] : 0;
            set => SetEaterValue(AbsorptionAttribute.FireEater, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ColdEater
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.ColdEater] : 0;
            set => SetEaterValue(AbsorptionAttribute.ColdEater, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonEater
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.PoisonEater] : 0;
            set => SetEaterValue(AbsorptionAttribute.PoisonEater, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EnergyEater
        {
            get => Owner is BaseArmor ? this[AbsorptionAttribute.EnergyEater] : 0;
            set => SetEaterValue(AbsorptionAttribute.EnergyEater, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FireResonance
        {
            get => CanHaveResonance ? this[AbsorptionAttribute.FireResonance] : 0;
            set => SetResonanceValue(AbsorptionAttribute.FireResonance, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ColdResonance
        {
            get => CanHaveResonance ? this[AbsorptionAttribute.ColdResonance] : 0;
            set => SetResonanceValue(AbsorptionAttribute.ColdResonance, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonResonance
        {
            get => CanHaveResonance ? this[AbsorptionAttribute.PoisonResonance] : 0;
            set => SetResonanceValue(AbsorptionAttribute.PoisonResonance, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EnergyResonance
        {
            get => CanHaveResonance ? this[AbsorptionAttribute.EnergyResonance] : 0;
            set => SetResonanceValue(AbsorptionAttribute.EnergyResonance, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int KineticResonance
        {
            get => CanHaveResonance ? this[AbsorptionAttribute.KineticResonance] : 0;
            set => SetResonanceValue(AbsorptionAttribute.KineticResonance, value);
        }

        internal bool HasDamageEater =>
            DamageEater != 0 || KineticEater != 0 || FireEater != 0 || ColdEater != 0 || PoisonEater != 0 || EnergyEater != 0;

        private void SetEaterValue(AbsorptionAttribute attribute, int value)
        {
            this[attribute] = Owner is BaseArmor ? value : 0;

            if (Owner is Item { Parent: Mobile mobile })
            {
                Server.Items.DamageEater.ClearIfInactive(mobile);
            }
        }

        private bool CanHaveResonance => Owner is BaseShield or BaseWeapon { Layer: Layer.TwoHanded };

        private void SetResonanceValue(AbsorptionAttribute attribute, int value) =>
            this[attribute] = CanHaveResonance ? value : 0;

        public static int GetValue(Mobile m, AbsorptionAttribute attribute)
        {
            if (!Core.SA || m == null)
            {
                return 0;
            }

            var items = m.Items;
            var value = 0;

            for (var i = 0; i < items.Count; ++i)
            {
                var obj = items[i];

                if (IsResonance(attribute))
                {
                    if (obj is BaseShield shield)
                    {
                        value += shield.AbsorptionAttributes[attribute];
                    }
                    else if (obj is BaseWeapon { Layer: Layer.TwoHanded } weapon)
                    {
                        value += weapon.AbsorptionAttributes[attribute];
                    }
                }
                else if (obj is BaseArmor armor)
                {
                    var attrs = armor.AbsorptionAttributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
            }

            return value;
        }

        internal static int GetResonanceValue(Mobile m, DamageType damageType)
        {
            AbsorptionAttribute attribute = damageType switch
            {
                DamageType.Physical => AbsorptionAttribute.KineticResonance,
                DamageType.Fire     => AbsorptionAttribute.FireResonance,
                DamageType.Cold     => AbsorptionAttribute.ColdResonance,
                DamageType.Poison   => AbsorptionAttribute.PoisonResonance,
                DamageType.Energy   => AbsorptionAttribute.EnergyResonance,
                _                   => (AbsorptionAttribute)0
            };

            return attribute == 0 ? 0 : Math.Min(AOS.ResonanceChanceCap, Math.Max(0, GetValue(m, attribute)));
        }

        private static bool IsResonance(AbsorptionAttribute attribute) => attribute is
            AbsorptionAttribute.FireResonance or AbsorptionAttribute.ColdResonance or
            AbsorptionAttribute.PoisonResonance or AbsorptionAttribute.EnergyResonance or
            AbsorptionAttribute.KineticResonance;

        internal static bool HasDamageEaterOn(Mobile m)
        {
            if (!Core.SA || m == null)
            {
                return false;
            }

            var items = m.Items;
            for (var i = 0; i < items.Count; ++i)
            {
                if (items[i] is BaseArmor armor && armor.AbsorptionAttributes?.HasDamageEater == true)
                {
                    return true;
                }
            }

            return false;
        }

        internal static int GetEaterValue(Mobile m, AbsorptionAttribute attribute)
        {
            var value = Math.Max(0, GetValue(m, attribute));
            var cap = attribute == AbsorptionAttribute.DamageEater
                ? Server.Items.DamageEater.AllTypesCap
                : Server.Items.DamageEater.SpecificCap;

            return Math.Min(value, cap);
        }

        public void GetProperties(IPropertyList list)
        {
            var castingFocus = CastingFocus;
            if (Core.SA && castingFocus != 0)
            {
                list.Add(1113696, castingFocus); // Casting Focus ~1_val~%
            }

            if (Core.SA)
            {
                if (DamageEater != 0)
                {
                    list.Add(1113598, DamageEater); // Damage Eater ~1_val~%
                }

                if (KineticEater != 0)
                {
                    list.Add(1113597, KineticEater); // Kinetic Eater ~1_val~%
                }

                if (FireEater != 0)
                {
                    list.Add(1113593, FireEater); // Fire Eater ~1_val~%
                }

                if (ColdEater != 0)
                {
                    list.Add(1113594, ColdEater); // Cold Eater ~1_val~%
                }

                if (PoisonEater != 0)
                {
                    list.Add(1113595, PoisonEater); // Poison Eater ~1_val~%
                }

                if (EnergyEater != 0)
                {
                    list.Add(1113596, EnergyEater); // Energy Eater ~1_val~%
                }

                if (FireResonance != 0)
                {
                    list.Add(1154655, FireResonance); // Fire Resonance ~1_val~%
                }

                if (ColdResonance != 0)
                {
                    list.Add(1154656, ColdResonance); // Cold Resonance ~1_val~%
                }

                if (PoisonResonance != 0)
                {
                    list.Add(1154657, PoisonResonance); // Poison Resonance ~1_val~%
                }

                if (EnergyResonance != 0)
                {
                    list.Add(1154658, EnergyResonance); // Energy Resonance ~1_val~%
                }

                if (KineticResonance != 0)
                {
                    list.Add(1154659, KineticResonance); // Kinetic Resonance ~1_val~%
                }
            }
        }

        public override string ToString() => "...";
    }

    [Flags]
    public enum NegativeAttribute
    {
        Prized = 0x00000001,
        Massive = 0x00000002,
        Brittle = 0x00000004,
        Antique = 0x00000008,
        Unwieldy = 0x00000010
    }

    public sealed class NegativeAttributes : BaseAttributes
    {
        public NegativeAttributes(Item owner) : base(owner)
        {
        }

        public NegativeAttributes(Item owner, NegativeAttributes other) : base(owner, other)
        {
        }

        public int this[NegativeAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Prized
        {
            get => this[NegativeAttribute.Prized];
            set => this[NegativeAttribute.Prized] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Massive
        {
            get => Owner is BaseWeapon or BaseArmor ? this[NegativeAttribute.Massive] : 0;
            set => this[NegativeAttribute.Massive] = Owner is BaseWeapon or BaseArmor ? value : 0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Antique
        {
            get => Owner is BaseWeapon or BaseArmor or BaseJewel ? this[NegativeAttribute.Antique] : 0;
            set => this[NegativeAttribute.Antique] = Owner is BaseWeapon or BaseArmor or BaseJewel ? value : 0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Brittle
        {
            get => Owner is BaseWeapon or BaseArmor ? this[NegativeAttribute.Brittle] : 0;
            set => this[NegativeAttribute.Brittle] = Owner is BaseWeapon or BaseArmor ? value : 0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Unwieldy
        {
            get => Owner is BaseWeapon or BaseArmor ? this[NegativeAttribute.Unwieldy] : 0;
            set
            {
                switch (Owner)
                {
                    case BaseWeapon weapon:
                        weapon.SetUnwieldy(value);
                        break;
                    case BaseArmor armor:
                        armor.SetUnwieldy(value);
                        break;
                }
            }
        }

        public static bool IsAntique(Item item)
        {
            if (!Core.HS)
            {
                return false;
            }

            return item switch
            {
                BaseWeapon weapon => weapon.NegativeAttributes.Antique != 0,
                BaseArmor armor   => armor.NegativeAttributes.Antique != 0,
                BaseJewel jewel   => jewel.NegativeAttributes.Antique != 0,
                _                 => false
            };
        }

        public static void ApplyAntiqueWear(Mobile wearer)
        {
            if (!wearer.Alive || !Core.HS)
            {
                return;
            }

            using var equipped = PooledRefList<Item>.Create(wearer.Items.Count);

            foreach (var item in wearer.Items)
            {
                equipped.Add(item);
            }

            foreach (var item in equipped)
            {
                ApplyAntiqueWear(item);
            }
        }

        private static void ApplyAntiqueWear(Item item)
        {
            if (!IsAntique(item) || Utility.Random(100) >= 2 || item is not IDurability durability)
            {
                return;
            }

            if (durability.HitPoints > 0)
            {
                --durability.HitPoints;
            }
            else if (durability.MaxHitPoints > 1)
            {
                --durability.MaxHitPoints;

                if (item.Parent is Mobile mobile)
                {
                    mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061121);
                }
            }
            else
            {
                item.Delete();
            }
        }

        public static bool IsBrittle(Item item)
        {
            if (!Core.HS)
            {
                return false;
            }

            return item switch
            {
                BaseWeapon weapon => weapon.NegativeAttributes.Brittle != 0,
                BaseArmor armor   => armor.NegativeAttributes.Brittle != 0,
                _                 => false
            };
        }

        public static bool IsMassive(Item item)
        {
            if (!Core.HS)
            {
                return false;
            }

            return item switch
            {
                BaseWeapon weapon => weapon.NegativeAttributes.Massive != 0,
                BaseArmor armor   => armor.NegativeAttributes.Massive != 0,
                _                 => false
            };
        }

        public static bool IsPrized(Item item)
        {
            if (!Core.HS)
            {
                return false;
            }

            return item switch
            {
                BaseWeapon weapon => weapon.NegativeAttributes.Prized != 0,
                BaseArmor armor   => armor.NegativeAttributes.Prized != 0,
                BaseJewel jewel   => jewel.NegativeAttributes.Prized != 0,
                _                 => false
            };
        }

        public void GetProperties(IPropertyList list)
        {
            if (Core.HS && Antique != 0)
            {
                list.Add(1076187); // Antique
            }

            if (Core.HS && Prized != 0)
            {
                list.Add(1154910); // Prized
            }

            if (Core.HS && Brittle != 0)
            {
                list.Add(1116209); // Brittle
            }
        }

        public override string ToString() => "...";
    }

    [Flags]
    public enum AosArmorAttribute
    {
        LowerStatReq = 0x00000001,
        SelfRepair = 0x00000002,
        MageArmor = 0x00000004,
        DurabilityBonus = 0x00000008,
        SoulCharge = 0x00000010
    }

    public sealed class AosArmorAttributes : BaseAttributes
    {
        public AosArmorAttributes(Item owner) : base(owner)
        {
        }

        public AosArmorAttributes(Item owner, AosArmorAttributes other) : base(owner, other)
        {
        }

        public int this[AosArmorAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LowerStatReq
        {
            get => this[AosArmorAttribute.LowerStatReq];
            set => this[AosArmorAttribute.LowerStatReq] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SelfRepair
        {
            get => this[AosArmorAttribute.SelfRepair];
            set => this[AosArmorAttribute.SelfRepair] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MageArmor
        {
            get => this[AosArmorAttribute.MageArmor];
            set => this[AosArmorAttribute.MageArmor] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DurabilityBonus
        {
            get => this[AosArmorAttribute.DurabilityBonus];
            set => this[AosArmorAttribute.DurabilityBonus] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoulCharge
        {
            get => Owner is BaseShield ? this[AosArmorAttribute.SoulCharge] : 0;
            set => this[AosArmorAttribute.SoulCharge] = Owner is BaseShield ? value : 0;
        }

        public static int GetValue(Mobile m, AosArmorAttribute attribute)
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var items = m.Items;
            var value = 0;

            for (var i = 0; i < items.Count; ++i)
            {
                var obj = items[i];

                if (obj is BaseArmor armor)
                {
                    var attrs = armor.ArmorAttributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
                else if (obj is BaseClothing clothing)
                {
                    var attrs = clothing.ClothingAttributes;

                    if (attrs != null)
                    {
                        value += attrs[attribute];
                    }
                }
            }

            return value;
        }

        public void GetProperties(IPropertyList list)
        {
            int prop;

            if (MageArmor != 0)
            {
                list.Add(1060437); // mage armor
            }

            if ((prop = SelfRepair) != 0)
            {
                list.Add(1060450, prop); // self repair ~1_val~
            }

            if (Core.SA && Owner is BaseShield && (prop = SoulCharge) != 0)
            {
                list.Add(1113630, prop); // Soul Charge ~1_val~%
            }
        }

        public override string ToString() => "...";
    }

    public sealed class AosSkillBonuses : BaseAttributes
    {
        private HashSet<SkillMod> m_Mods;

        public AosSkillBonuses(Item owner) : base(owner)
        {
        }

        public AosSkillBonuses(Item owner, AosSkillBonuses other) : base(owner, other)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Skill_1_Value
        {
            get => GetBonus(0);
            set => SetBonus(0, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill_1_Name
        {
            get => GetSkill(0);
            set => SetSkill(0, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Skill_2_Value
        {
            get => GetBonus(1);
            set => SetBonus(1, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill_2_Name
        {
            get => GetSkill(1);
            set => SetSkill(1, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Skill_3_Value
        {
            get => GetBonus(2);
            set => SetBonus(2, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill_3_Name
        {
            get => GetSkill(2);
            set => SetSkill(2, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Skill_4_Value
        {
            get => GetBonus(3);
            set => SetBonus(3, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill_4_Name
        {
            get => GetSkill(3);
            set => SetSkill(3, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Skill_5_Value
        {
            get => GetBonus(4);
            set => SetBonus(4, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill_5_Name
        {
            get => GetSkill(4);
            set => SetSkill(4, value);
        }

        public void GetProperties(IPropertyList list)
        {
            for (var i = 0; i < 5; ++i)
            {
                if (GetValues(i, out var skill, out var bonus))
                {
                    list.Add(1060451 + i, $"{GetLabel(skill):#}\t{bonus}");
                }
            }
        }

        public static int GetLabel(SkillName skill)
        {
            return skill switch
            {
                SkillName.EvalInt     => 1002070, // Evaluate Intelligence
                SkillName.Forensics   => 1002078, // Forensic Evaluation
                SkillName.Lockpicking => 1002097, // Lockpicking
                _                     => 1044060 + (int)skill
            };
        }

        public static int GetLowercaseLabel(SkillName skill)
        {
            return skill switch
            {
                SkillName.Provocation  => 1049473, // provocation
                SkillName.MagicResist  => 1049471, // resisting spells
                SkillName.AnimalTaming => 1049472, // animal taming
                SkillName.Macing       => 1049470, // mace fighting
                SkillName.Necromancy   => 1060842, // necromancy
                SkillName.Focus        => 1061613, // focus
                SkillName.Chivalry     => 1061615, // chivalry
                SkillName.Bushido      => 1062935, // bushido
                SkillName.Ninjitsu     => 1062936, // ninjitsu
                SkillName.Spellweaving => 1074397, // spellweaving
                SkillName.Mysticism    => 1112544, // mysticism
                SkillName.Imbuing      => 1112545, // imbuing
                SkillName.Throwing     => 1112553, // throwing
                _                      => 1042347 + (int)skill
            };
        }

        public void AddTo(Mobile m)
        {
            Remove();

            for (var i = 0; i < 5; ++i)
            {
                if (!GetValues(i, out var skill, out var bonus))
                {
                    continue;
                }

                m_Mods ??= new HashSet<SkillMod>();

                SkillMod sk = new DefaultSkillMod(skill, $"{GetHashCode()}{skill}", true, bonus);
                sk.ObeyCap = true;
                m.AddSkillMod(sk);
                m_Mods.Add(sk);
            }
        }

        public void Remove()
        {
            if (m_Mods == null)
            {
                return;
            }

            foreach (var mod in m_Mods)
            {
                var m = mod.Owner;
                mod.Remove();

                if (Core.ML)
                {
                    CheckCancelMorph(m);
                }
            }

            m_Mods = null;
        }

        public bool GetValues(int index, out SkillName skill, out double bonus)
        {
            var v = GetValue(1 << index);
            var vSkill = 0;
            var vBonus = 0;

            for (var i = 0; i < 16; ++i)
            {
                vSkill <<= 1;
                vSkill |= v & 1;
                v >>= 1;

                vBonus <<= 1;
                vBonus |= v & 1;
                v >>= 1;
            }

            skill = (SkillName)vSkill;
            bonus = (double)vBonus / 10;

            return bonus != 0;
        }

        public void SetValues(int index, SkillName skill, double bonus)
        {
            var v = 0;
            var vSkill = (int)skill;
            var vBonus = (int)(bonus * 10);

            for (var i = 0; i < 16; ++i)
            {
                v <<= 1;
                v |= vBonus & 1;
                vBonus >>= 1;

                v <<= 1;
                v |= vSkill & 1;
                vSkill >>= 1;
            }

            SetValue(1 << index, v);
        }

        public SkillName GetSkill(int index)
        {
            GetValues(index, out var skill, out var _);

            return skill;
        }

        public void SetSkill(int index, SkillName skill)
        {
            SetValues(index, skill, GetBonus(index));
        }

        public double GetBonus(int index)
        {
            GetValues(index, out var _, out var bonus);

            return bonus;
        }

        public void SetBonus(int index, double bonus)
        {
            SetValues(index, GetSkill(index), bonus);
        }

        public override string ToString() => "...";

        public static void CheckCancelMorph(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            var acontext = AnimalForm.GetContext(m);
            var context = TransformationSpellHelper.GetContext(m);

            if (context?.Spell is Spell spell)
            {
                spell.GetCastSkills(out var minSkill, out _);
                if (m.Skills[spell.CastSkill].Value < minSkill)
                {
                    TransformationSpellHelper.RemoveContext(m, context, true);
                }
            }

            if (acontext != null)
            {
                int i;
                for (i = 0; i < AnimalForm.Entries.Length; ++i)
                {
                    if (AnimalForm.Entries[i].Type == acontext.Type)
                    {
                        break;
                    }
                }

                if (m.Skills.Ninjitsu.Value < AnimalForm.Entries[i].ReqSkill)
                {
                    AnimalForm.RemoveContext(m);
                }
            }

            if (m.Skills.Magery.Value < 66.1)
            {
                PolymorphSpell.EndPolymorph(m);
            }

            if (m.Skills.Magery.Value < 38.1)
            {
                IncognitoSpell.EndIncognito(m);
            }
        }
    }

    [Flags]
    public enum AosElementAttribute
    {
        Physical = 0x00000001,
        Fire = 0x00000002,
        Cold = 0x00000004,
        Poison = 0x00000008,
        Energy = 0x00000010,
        Chaos = 0x00000020,
        Direct = 0x00000040
    }

    public sealed class AosElementAttributes : BaseAttributes
    {
        public AosElementAttributes(Item owner) : base(owner)
        {
        }

        public AosElementAttributes(Item owner, AosElementAttributes other) : base(owner, other)
        {
        }

        public int this[AosElementAttribute attribute]
        {
            get => GetValue((int)attribute);
            set => SetValue((int)attribute, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Physical
        {
            get => this[AosElementAttribute.Physical];
            set => this[AosElementAttribute.Physical] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Fire
        {
            get => this[AosElementAttribute.Fire];
            set => this[AosElementAttribute.Fire] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Cold
        {
            get => this[AosElementAttribute.Cold];
            set => this[AosElementAttribute.Cold] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Poison
        {
            get => this[AosElementAttribute.Poison];
            set => this[AosElementAttribute.Poison] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Energy
        {
            get => this[AosElementAttribute.Energy];
            set => this[AosElementAttribute.Energy] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Chaos
        {
            get => this[AosElementAttribute.Chaos];
            set => this[AosElementAttribute.Chaos] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Direct
        {
            get => this[AosElementAttribute.Direct];
            set => this[AosElementAttribute.Direct] = value;
        }

        public override string ToString() => "...";
    }

    [PropertyObject]
    [SerializationGenerator(0)]
    public abstract partial class BaseAttributes
    {
        [SerializableField(0, setter: "private")]
        private uint _names;

        [EncodedInt]
        [SerializableField(1, setter: "private")]
        private int[] _values;

        public BaseAttributes(Item owner)
        {
            _owner = owner;
            _values = Array.Empty<int>();
        }

        public BaseAttributes(Item owner, BaseAttributes other)
        {
            _owner = owner;
            _values = new int[other._values.Length];
            other._values.CopyTo(_values, 0);
            _names = other._names;
        }

        public bool IsEmpty => _names == 0;

        private IEntity _owner;

        [DirtyTrackingEntity]
        public IEntity Owner => _owner;

        public int GetValue(int bitmask)
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var mask = (uint)bitmask;

            if ((_names & mask) == 0)
            {
                return 0;
            }

            var index = GetIndex(mask);

            if (index >= 0 && index < _values.Length)
            {
                return _values[index];
            }

            return 0;
        }

        public void SetValue(int bitmask, int value)
        {
            if (bitmask == (int)AosWeaponAttribute.DurabilityBonus && this is AosWeaponAttributes)
            {
                if (Owner is BaseWeapon weapon)
                {
                    weapon.UnscaleDurability();
                }
            }
            else if (bitmask == (int)AosArmorAttribute.DurabilityBonus && this is AosArmorAttributes)
            {
                if (Owner is BaseArmor armor)
                {
                    armor.UnscaleDurability();
                }
                else if (Owner is BaseClothing clothing)
                {
                    clothing.UnscaleDurability();
                }
            }

            var mask = (uint)bitmask;

            if (value != 0)
            {
                if ((_names & mask) != 0)
                {
                    var index = GetIndex(mask);

                    if (index >= 0 && index < _values.Length)
                    {
                        _values[index] = value;
                    }
                }
                else
                {
                    var index = GetIndex(mask);

                    if (index >= 0 && index <= _values.Length)
                    {
                        var old = _values;
                        _values = new int[old.Length + 1];

                        for (var i = 0; i < index; ++i)
                        {
                            _values[i] = old[i];
                        }

                        _values[index] = value;

                        for (var i = index; i < old.Length; ++i)
                        {
                            _values[i + 1] = old[i];
                        }

                        _names |= mask;
                    }
                }
            }
            else if ((_names & mask) != 0)
            {
                var index = GetIndex(mask);

                if (index >= 0 && index < _values.Length)
                {
                    _names &= ~mask;

                    if (_values.Length == 1)
                    {
                        _values = Array.Empty<int>();
                    }
                    else
                    {
                        var old = _values;
                        _values = new int[old.Length - 1];

                        for (var i = 0; i < index; ++i)
                        {
                            _values[i] = old[i];
                        }

                        for (var i = index + 1; i < old.Length; ++i)
                        {
                            _values[i - 1] = old[i];
                        }
                    }
                }
            }

            if (bitmask == (int)AosWeaponAttribute.DurabilityBonus && this is AosWeaponAttributes)
            {
                if (Owner is BaseWeapon weapon)
                {
                    weapon.ScaleDurability();
                }
            }
            else if (bitmask == (int)AosArmorAttribute.DurabilityBonus && this is AosArmorAttributes)
            {
                if (Owner is BaseArmor armor)
                {
                    armor.ScaleDurability();
                }
                else if (Owner is BaseClothing clothing)
                {
                    clothing.ScaleDurability();
                }
            }

            if (Owner is Item item)
            {
                if (item.Parent is Mobile m)
                {
                    m.CheckStatTimers();
                    m.UpdateResistances();
                    m.Delta(
                        MobileDelta.Stat | MobileDelta.WeaponDamage | MobileDelta.Hits | MobileDelta.Stam |
                        MobileDelta.Mana
                    );

                    if (this is AosSkillBonuses skillBonuses)
                    {
                        skillBonuses.Remove();
                        skillBonuses.AddTo(m);
                    }
                }

                item.InvalidateProperties();
            }
            else if (Owner is Mobile mob)
            {
                mob.InvalidateProperties();
            }
        }

        private int GetIndex(uint mask)
        {
            var index = 0;
            var ourNames = _names;
            uint currentBit = 1;

            while (currentBit != mask)
            {
                if ((ourNames & currentBit) != 0)
                {
                    ++index;
                }

                if (currentBit == 0x80000000)
                {
                    return -1;
                }

                currentBit <<= 1;
            }

            return index;
        }
    }
}
