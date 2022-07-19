using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;

namespace Server
{
    public static class AOS
    {
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

            BaseQuiver quiver = null;

            if (archer && from != null)
            {
                quiver = from.FindItemOnLayer(Layer.Cloak) as BaseQuiver;
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

                totalDamage = damage * phys * (100 - resPhys);
                totalDamage += damage * fire * (100 - resFire);
                totalDamage += damage * cold * (100 - resCold);
                totalDamage += damage * pois * (100 - resPois);
                totalDamage += damage * nrgy * (100 - resNrgy);

                totalDamage /= 10000;

                if (Core.ML)
                {
                    totalDamage += damage * direct / 100;

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
                var reflectPhysAbility = bcFrom?.GetAbility(MonsterAbilityType.ReflectPhysicalDamage) as ReflectPhysicalDamage;

                if (reflectPhysAbility
                        ?.CanTrigger(bcFrom, MonsterAbilityTrigger.CombatAction) == true)
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

            m.Damage(totalDamage, from);
            return totalDamage;
        }

        public static void Fix(ref int val)
        {
            if (val < 0)
            {
                val = 0;
            }
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
        IncreasedKarmaLoss = 0x00800000
    }

    public sealed class AosAttributes : BaseAttributes
    {
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

        public override string ToString() => "...";
    }

    [Flags]
    public enum AosArmorAttribute
    {
        LowerStatReq = 0x00000001,
        SelfRepair = 0x00000002,
        MageArmor = 0x00000004,
        DurabilityBonus = 0x00000008
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

        public void CheckCancelMorph(Mobile m)
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
                    AnimalForm.RemoveContext(m, true);
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
