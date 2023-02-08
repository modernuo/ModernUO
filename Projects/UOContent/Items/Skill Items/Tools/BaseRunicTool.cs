using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public abstract class BaseRunicTool : BaseTool
    {
        private const int MaxProperties = 32;

        private static bool m_IsRunicTool;
        private static int m_LuckChance;

        private static readonly SkillName[] m_PossibleBonusSkills =
        {
            SkillName.Swords,
            SkillName.Fencing,
            SkillName.Macing,
            SkillName.Archery,
            SkillName.Wrestling,
            SkillName.Parry,
            SkillName.Tactics,
            SkillName.Anatomy,
            SkillName.Healing,
            SkillName.Magery,
            SkillName.Meditation,
            SkillName.EvalInt,
            SkillName.MagicResist,
            SkillName.AnimalTaming,
            SkillName.AnimalLore,
            SkillName.Veterinary,
            SkillName.Musicianship,
            SkillName.Provocation,
            SkillName.Discordance,
            SkillName.Peacemaking,
            SkillName.Chivalry,
            SkillName.Focus,
            SkillName.Necromancy,
            SkillName.Stealing,
            SkillName.Stealth,
            SkillName.SpiritSpeak,
            SkillName.Bushido,
            SkillName.Ninjitsu
        };

        private static readonly SkillName[] m_PossibleSpellbookSkills =
        {
            SkillName.Magery,
            SkillName.Meditation,
            SkillName.EvalInt,
            SkillName.MagicResist
        };

        private static readonly BitArray m_Props = new(MaxProperties);
        private static readonly int[] m_Possible = new int[MaxProperties];
        private CraftResource m_Resource;

        public BaseRunicTool(CraftResource resource, int itemID) : base(itemID) => m_Resource = resource;

        public BaseRunicTool(CraftResource resource, int uses, int itemID) : base(uses, itemID) => m_Resource = resource;

        public BaseRunicTool(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                m_Resource = value;
                Hue = CraftResources.GetHue(m_Resource);
                InvalidateProperties();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write((int)m_Resource);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }
        }

        private static int Scale(int min, int max, int low, int high)
        {
            int percent;

            if (m_IsRunicTool)
            {
                percent = Utility.RandomMinMax(min, max);
            }
            else
            {
                // Behold, the worst system ever!
                var v = Utility.RandomMinMax(0, 10000);

                v = (int)Math.Sqrt(v);
                v = 100 - v;

                if (LootPack.CheckLuck(m_LuckChance))
                {
                    v += 10;
                }

                if (v < min)
                {
                    v = min;
                }
                else if (v > max)
                {
                    v = max;
                }

                percent = v;
            }

            percent *= 10000 + 10000 / ((high - low).Abs() + 1);

            return low + (high - low) * percent / 1000001;
        }

        private static void ApplyAttribute(
            AosAttributes attrs, int min, int max, AosAttribute attr, int low, int high,
            int scale = 1
        )
        {
            if (attr == AosAttribute.CastSpeed)
            {
                attrs[attr] += Scale(min, max, low / scale, high / scale) * scale;
            }
            else
            {
                attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
            }

            if (attr == AosAttribute.SpellChanneling)
            {
                attrs[AosAttribute.CastSpeed] -= 1;
            }
        }

        private static void ApplyAttribute(
            AosArmorAttributes attrs, int min, int max, AosArmorAttribute attr, int low,
            int high
        )
        {
            attrs[attr] = Scale(min, max, low, high);
        }

        private static void ApplyAttribute(
            AosArmorAttributes attrs, int min, int max, AosArmorAttribute attr, int low,
            int high, int scale
        )
        {
            attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
        }

        private static void ApplyAttribute(
            AosWeaponAttributes attrs, int min, int max, AosWeaponAttribute attr, int low,
            int high
        )
        {
            attrs[attr] = Scale(min, max, low, high);
        }

        private static void ApplyAttribute(
            AosWeaponAttributes attrs, int min, int max, AosWeaponAttribute attr, int low,
            int high, int scale
        )
        {
            attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
        }

        private static void ApplyAttribute(
            AosElementAttributes attrs, int min, int max, AosElementAttribute attr, int low,
            int high
        )
        {
            attrs[attr] = Scale(min, max, low, high);
        }

        private static void ApplyAttribute(
            AosElementAttributes attrs, int min, int max, AosElementAttribute attr, int low,
            int high, int scale
        )
        {
            attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
        }

        private static void ApplySkillBonus(AosSkillBonuses attrs, int min, int max, int index, int low, int high)
        {
            var isSpellBook = attrs.Owner is Spellbook;
            var possibleSkills =
                new List<SkillName>(isSpellBook ? m_PossibleSpellbookSkills : m_PossibleBonusSkills);
            var count = Core.SE || isSpellBook ? possibleSkills.Count : possibleSkills.Count - 2;

            SkillName sk;
            bool found;

            do
            {
                found = false;
                sk = possibleSkills[Utility.Random(count--)];
                possibleSkills.Remove(sk);

                for (var i = 0; !found && i < 5; ++i)
                {
                    found = attrs.GetValues(i, out var check, out _) && check == sk;
                }
            } while (found && count > 0);

            attrs.SetValues(index, sk, Scale(min, max, low, high));
        }

        private static void ApplyResistance(BaseArmor ar, int min, int max, ResistanceType res, int low, int high)
        {
            switch (res)
            {
                case ResistanceType.Physical:
                    {
                        ar.PhysicalBonus += Scale(min, max, low, high);
                        break;
                    }
                case ResistanceType.Fire:
                    {
                        ar.FireBonus += Scale(min, max, low, high);
                        break;
                    }
                case ResistanceType.Cold:
                    {
                        ar.ColdBonus += Scale(min, max, low, high);
                        break;
                    }
                case ResistanceType.Poison:
                    {
                        ar.PoisonBonus += Scale(min, max, low, high);
                        break;
                    }
                case ResistanceType.Energy:
                    {
                        ar.EnergyBonus += Scale(min, max, low, high);
                        break;
                    }
            }
        }

        public static int GetUniqueRandom(int count)
        {
            var avail = 0;

            for (var i = 0; i < count; ++i)
            {
                if (!m_Props[i])
                {
                    m_Possible[avail++] = i;
                }
            }

            if (avail == 0)
            {
                return -1;
            }

            var v = m_Possible[Utility.Random(avail)];

            m_Props.Set(v, true);

            return v;
        }

        public void ApplyAttributesTo(BaseWeapon weapon)
        {
            var resInfo = CraftResources.GetInfo(m_Resource);

            var attrs = resInfo?.AttributeInfo;

            if (attrs == null)
            {
                return;
            }

            var attributeCount = Utility.RandomMinMax(attrs.RunicMinAttributes, attrs.RunicMaxAttributes);
            var min = attrs.RunicMinIntensity;
            var max = attrs.RunicMaxIntensity;

            ApplyAttributesTo(weapon, true, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseWeapon weapon, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(weapon, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(
            BaseWeapon weapon, bool isRunicTool, int luckChance, int attributeCount,
            int min, int max
        )
        {
            m_IsRunicTool = isRunicTool;
            m_LuckChance = luckChance;

            var primary = weapon.Attributes;
            var secondary = weapon.WeaponAttributes;

            m_Props.SetAll(false);

            if (weapon is BaseRanged)
            {
                m_Props.Set(2, true); // ranged weapons cannot be ubws or mageweapon
            }

            for (var i = 0; i < attributeCount; ++i)
            {
                var random = GetUniqueRandom(25);

                if (random == -1)
                {
                    break;
                }

                switch (random)
                {
                    case 0:
                        {
                            switch (Utility.Random(5))
                            {
                                case 0:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitPhysicalArea, 2, 50, 2);
                                        break;
                                    }
                                case 1:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitFireArea, 2, 50, 2);
                                        break;
                                    }
                                case 2:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitColdArea, 2, 50, 2);
                                        break;
                                    }
                                case 3:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitPoisonArea, 2, 50, 2);
                                        break;
                                    }
                                case 4:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitEnergyArea, 2, 50, 2);
                                        break;
                                    }
                            }

                            break;
                        }
                    case 1:
                        {
                            switch (Utility.Random(4))
                            {
                                case 0:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitMagicArrow, 2, 50, 2);
                                        break;
                                    }
                                case 1:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitHarm, 2, 50, 2);
                                        break;
                                    }
                                case 2:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitFireball, 2, 50, 2);
                                        break;
                                    }
                                case 3:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitLightning, 2, 50, 2);
                                        break;
                                    }
                            }

                            break;
                        }
                    case 2:
                        {
                            switch (Utility.Random(2))
                            {
                                case 0:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.UseBestSkill, 1, 1);
                                        break;
                                    }
                                case 1:
                                    {
                                        ApplyAttribute(secondary, min, max, AosWeaponAttribute.MageWeapon, 1, 10);
                                        break;
                                    }
                            }

                            break;
                        }
                    case 3:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.WeaponDamage, 1, 50);
                            break;
                        }
                    case 4:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.DefendChance, 1, 15);
                            break;
                        }
                    case 5:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.CastSpeed, 1, 1);
                            break;
                        }
                    case 6:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.AttackChance, 1, 15);
                            break;
                        }
                    case 7:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.Luck, 1, 100);
                            break;
                        }
                    case 8:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.WeaponSpeed, 5, 30, 5);
                            break;
                        }
                    case 9:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.SpellChanneling, 1, 1);
                            break;
                        }
                    case 10:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitDispel, 2, 50, 2);
                            break;
                        }
                    case 11:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitLeechHits, 2, 50, 2);
                            break;
                        }
                    case 12:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitLowerAttack, 2, 50, 2);
                            break;
                        }
                    case 13:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitLowerDefend, 2, 50, 2);
                            break;
                        }
                    case 14:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitLeechMana, 2, 50, 2);
                            break;
                        }
                    case 15:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.HitLeechStam, 2, 50, 2);
                            break;
                        }
                    case 16:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.LowerStatReq, 10, 100, 10);
                            break;
                        }
                    case 17:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.ResistPhysicalBonus, 1, 15);
                            break;
                        }
                    case 18:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.ResistFireBonus, 1, 15);
                            break;
                        }
                    case 19:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.ResistColdBonus, 1, 15);
                            break;
                        }
                    case 20:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.ResistPoisonBonus, 1, 15);
                            break;
                        }
                    case 21:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.ResistEnergyBonus, 1, 15);
                            break;
                        }
                    case 22:
                        {
                            ApplyAttribute(secondary, min, max, AosWeaponAttribute.DurabilityBonus, 10, 100, 10);
                            break;
                        }
                    case 23:
                        {
                            weapon.Slayer = GetRandomSlayer();
                            break;
                        }
                    case 24:
                        {
                            GetElementalDamages(weapon);
                            break;
                        }
                }
            }
        }

        public static void GetElementalDamages(BaseWeapon weapon)
        {
            GetElementalDamages(weapon, true);
        }

        public static void GetElementalDamages(BaseWeapon weapon, bool randomizeOrder)
        {
            weapon.GetDamageTypes(null, out var phys, out _, out _, out _, out _, out _, out _);

            var totalDamage = phys;

            AosElementAttribute[] attrs =
            {
                AosElementAttribute.Cold,
                AosElementAttribute.Energy,
                AosElementAttribute.Fire,
                AosElementAttribute.Poison
            };

            if (randomizeOrder)
            {
                for (var i = 0; i < attrs.Length; i++)
                {
                    var temp = attrs[i];
                    var rand = Utility.Random(attrs.Length);

                    attrs[i] = attrs[rand];
                    attrs[rand] = temp;
                }
            }

            /*
              totalDamage = AssignElementalDamage( weapon, AosElementAttribute.Cold, totalDamage );
              totalDamage = AssignElementalDamage( weapon, AosElementAttribute.Energy, totalDamage );
              totalDamage = AssignElementalDamage( weapon, AosElementAttribute.Fire, totalDamage );
              totalDamage = AssignElementalDamage( weapon, AosElementAttribute.Poison, totalDamage );

              weapon.AosElementDamages[AosElementAttribute.Physical] = 100 - totalDamage;
             * */

            for (var i = 0; i < attrs.Length; i++)
            {
                totalDamage = AssignElementalDamage(weapon, attrs[i], totalDamage);
            }

            // Order is Cold, Energy, Fire, Poison -> Physical left
            // Cannot be looped, AoselementAttribute is 'out of order'

            weapon.Hue = weapon.GetElementalDamageHue();
        }

        private static int AssignElementalDamage(BaseWeapon weapon, AosElementAttribute attr, int totalDamage)
        {
            if (totalDamage <= 0)
            {
                return 0;
            }

            var random = Utility.Random(totalDamage / 10 + 1) * 10;
            weapon.AosElementDamages[attr] = random;

            return totalDamage - random;
        }

        public static SlayerName GetRandomSlayer()
        {
            // TODO: Check random algorithm on OSI

            var groups = SlayerGroup.Groups;

            if (groups.Length == 0)
            {
                return SlayerName.None;
            }

            // 10% chance of a super - skip fey
            if (Utility.RandomDouble() < 0.10)
            {
                return groups[Utility.Random(groups.Length - 1)].Super.Name;
            }

            // Minor slayer - skip fey and undead
            return groups[Utility.Random(groups.Length - 2)].Entries.RandomElement().Name;
        }

        public void ApplyAttributesTo(BaseArmor armor)
        {
            var resInfo = CraftResources.GetInfo(m_Resource);

            var attrs = resInfo?.AttributeInfo;

            if (attrs == null)
            {
                return;
            }

            var attributeCount = Utility.RandomMinMax(attrs.RunicMinAttributes, attrs.RunicMaxAttributes);
            var min = attrs.RunicMinIntensity;
            var max = attrs.RunicMaxIntensity;

            ApplyAttributesTo(armor, true, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseArmor armor, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(armor, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(
            BaseArmor armor, bool isRunicTool, int luckChance, int attributeCount, int min,
            int max
        )
        {
            m_IsRunicTool = isRunicTool;
            m_LuckChance = luckChance;

            var primary = armor.Attributes;
            var secondary = armor.ArmorAttributes;

            m_Props.SetAll(false);

            var isShield = armor is BaseShield;
            var baseCount = isShield ? 7 : 20;
            var baseOffset = isShield ? 0 : 4;

            if (!isShield && armor.MeditationAllowance == ArmorMeditationAllowance.All)
            {
                m_Props.Set(3, true); // remove mage armor from possible properties
            }

            if (armor.Resource >= CraftResource.RegularLeather && armor.Resource <= CraftResource.BarbedLeather)
            {
                m_Props.Set(0, true); // remove lower requirements from possible properties for leather armor
                m_Props.Set(2, true); // remove durability bonus from possible properties
            }

            if (armor.RequiredRaces == Race.AllowElvesOnly)
            {
                // elves inherently have night sight and elf only armor doesn't get night sight as a mod
                m_Props.Set(7, true);
            }

            for (var i = 0; i < attributeCount; ++i)
            {
                var random = GetUniqueRandom(baseCount);

                if (random == -1)
                {
                    break;
                }

                random += baseOffset;

                switch (random)
                {
                    /* Begin Shields */
                    case 0:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.SpellChanneling, 1, 1);
                            break;
                        }
                    case 1:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.DefendChance, 1, 15);
                            break;
                        }
                    case 2:
                        {
                            if (Core.ML)
                            {
                                ApplyAttribute(primary, min, max, AosAttribute.ReflectPhysical, 1, 15);
                            }
                            else
                            {
                                ApplyAttribute(primary, min, max, AosAttribute.AttackChance, 1, 15);
                            }

                            break;
                        }
                    case 3:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.CastSpeed, 1, 1);
                            break;
                        }
                    /* Begin Armor */
                    case 4:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.LowerStatReq, 10, 100, 10);
                            break;
                        }
                    case 5:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.SelfRepair, 1, 5);
                            break;
                        }
                    case 6:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.DurabilityBonus, 10, 100, 10);
                            break;
                        }
                    /* End Shields */
                    case 7:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.MageArmor, 1, 1);
                            break;
                        }
                    case 8:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenHits, 1, 2);
                            break;
                        }
                    case 9:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenStam, 1, 3);
                            break;
                        }
                    case 10:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenMana, 1, 2);
                            break;
                        }
                    case 11:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.NightSight, 1, 1);
                            break;
                        }
                    case 12:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusHits, 1, 5);
                            break;
                        }
                    case 13:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusStam, 1, 8);
                            break;
                        }
                    case 14:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusMana, 1, 8);
                            break;
                        }
                    case 15:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerManaCost, 1, 8);
                            break;
                        }
                    case 16:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerRegCost, 1, 20);
                            break;
                        }
                    case 17:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.Luck, 1, 100);
                            break;
                        }
                    case 18:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.ReflectPhysical, 1, 15);
                            break;
                        }
                    case 19:
                        {
                            ApplyResistance(armor, min, max, ResistanceType.Physical, 1, 15);
                            break;
                        }
                    case 20:
                        {
                            ApplyResistance(armor, min, max, ResistanceType.Fire, 1, 15);
                            break;
                        }
                    case 21:
                        {
                            ApplyResistance(armor, min, max, ResistanceType.Cold, 1, 15);
                            break;
                        }
                    case 22:
                        {
                            ApplyResistance(armor, min, max, ResistanceType.Poison, 1, 15);
                            break;
                        }
                    case 23:
                        {
                            ApplyResistance(armor, min, max, ResistanceType.Energy, 1, 15);
                            break;
                        }
                    /* End Armor */
                }
            }
        }

        public static void ApplyAttributesTo(BaseHat hat, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(hat, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(
            BaseHat hat, bool isRunicTool, int luckChance, int attributeCount, int min,
            int max
        )
        {
            m_IsRunicTool = isRunicTool;
            m_LuckChance = luckChance;

            var primary = hat.Attributes;
            var secondary = hat.ClothingAttributes;
            var resists = hat.Resistances;

            m_Props.SetAll(false);

            for (var i = 0; i < attributeCount; ++i)
            {
                var random = GetUniqueRandom(19);

                if (random == -1)
                {
                    break;
                }

                switch (random)
                {
                    case 0:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.ReflectPhysical, 1, 15);
                            break;
                        }
                    case 1:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenHits, 1, 2);
                            break;
                        }
                    case 2:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenStam, 1, 3);
                            break;
                        }
                    case 3:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenMana, 1, 2);
                            break;
                        }
                    case 4:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.NightSight, 1, 1);
                            break;
                        }
                    case 5:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusHits, 1, 5);
                            break;
                        }
                    case 6:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusStam, 1, 8);
                            break;
                        }
                    case 7:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusMana, 1, 8);
                            break;
                        }
                    case 8:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerManaCost, 1, 8);
                            break;
                        }
                    case 9:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerRegCost, 1, 20);
                            break;
                        }
                    case 10:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.Luck, 1, 100);
                            break;
                        }
                    case 11:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.LowerStatReq, 10, 100, 10);
                            break;
                        }
                    case 12:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.SelfRepair, 1, 5);
                            break;
                        }
                    case 13:
                        {
                            ApplyAttribute(secondary, min, max, AosArmorAttribute.DurabilityBonus, 10, 100, 10);
                            break;
                        }
                    case 14:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Physical, 1, 15);
                            break;
                        }
                    case 15:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Fire, 1, 15);
                            break;
                        }
                    case 16:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Cold, 1, 15);
                            break;
                        }
                    case 17:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Poison, 1, 15);
                            break;
                        }
                    case 18:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Energy, 1, 15);
                            break;
                        }
                }
            }
        }

        public static void ApplyAttributesTo(BaseJewel jewelry, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(jewelry, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(
            BaseJewel jewelry, bool isRunicTool, int luckChance, int attributeCount,
            int min, int max
        )
        {
            m_IsRunicTool = isRunicTool;
            m_LuckChance = luckChance;

            var primary = jewelry.Attributes;
            var resists = jewelry.Resistances;
            var skills = jewelry.SkillBonuses;

            m_Props.SetAll(false);

            for (var i = 0; i < attributeCount; ++i)
            {
                var random = GetUniqueRandom(24);

                if (random == -1)
                {
                    break;
                }

                switch (random)
                {
                    case 0:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Physical, 1, 15);
                            break;
                        }
                    case 1:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Fire, 1, 15);
                            break;
                        }
                    case 2:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Cold, 1, 15);
                            break;
                        }
                    case 3:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Poison, 1, 15);
                            break;
                        }
                    case 4:
                        {
                            ApplyAttribute(resists, min, max, AosElementAttribute.Energy, 1, 15);
                            break;
                        }
                    case 5:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.WeaponDamage, 1, 25);
                            break;
                        }
                    case 6:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.DefendChance, 1, 15);
                            break;
                        }
                    case 7:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.AttackChance, 1, 15);
                            break;
                        }
                    case 8:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusStr, 1, 8);
                            break;
                        }
                    case 9:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusDex, 1, 8);
                            break;
                        }
                    case 10:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusInt, 1, 8);
                            break;
                        }
                    case 11:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.EnhancePotions, 5, 25, 5);
                            break;
                        }
                    case 12:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.CastSpeed, 1, 1);
                            break;
                        }
                    case 13:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.CastRecovery, 1, 3);
                            break;
                        }
                    case 14:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerManaCost, 1, 8);
                            break;
                        }
                    case 15:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerRegCost, 1, 20);
                            break;
                        }
                    case 16:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.Luck, 1, 100);
                            break;
                        }
                    case 17:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.SpellDamage, 1, 12);
                            break;
                        }
                    case 18:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.NightSight, 1, 1);
                            break;
                        }
                    case 19:
                        {
                            ApplySkillBonus(skills, min, max, 0, 1, 15);
                            break;
                        }
                    case 20:
                        {
                            ApplySkillBonus(skills, min, max, 1, 1, 15);
                            break;
                        }
                    case 21:
                        {
                            ApplySkillBonus(skills, min, max, 2, 1, 15);
                            break;
                        }
                    case 22:
                        {
                            ApplySkillBonus(skills, min, max, 3, 1, 15);
                            break;
                        }
                    case 23:
                        {
                            ApplySkillBonus(skills, min, max, 4, 1, 15);
                            break;
                        }
                }
            }
        }

        public static void ApplyAttributesTo(Spellbook spellbook, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(spellbook, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(
            Spellbook spellbook, bool isRunicTool, int luckChance, int attributeCount,
            int min, int max
        )
        {
            m_IsRunicTool = isRunicTool;
            m_LuckChance = luckChance;

            var primary = spellbook.Attributes;
            var skills = spellbook.SkillBonuses;

            m_Props.SetAll(false);

            for (var i = 0; i < attributeCount; ++i)
            {
                var random = GetUniqueRandom(16);

                if (random == -1)
                {
                    break;
                }

                switch (random)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusInt, 1, 8);

                            for (var j = 0; j < 4; ++j)
                            {
                                m_Props.Set(j, true);
                            }

                            break;
                        }
                    case 4:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.BonusMana, 1, 8);
                            break;
                        }
                    case 5:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.CastSpeed, 1, 1);
                            break;
                        }
                    case 6:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.CastRecovery, 1, 3);
                            break;
                        }
                    case 7:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.SpellDamage, 1, 12);
                            break;
                        }
                    case 8:
                        {
                            ApplySkillBonus(skills, min, max, 0, 1, 15);
                            break;
                        }
                    case 9:
                        {
                            ApplySkillBonus(skills, min, max, 1, 1, 15);
                            break;
                        }
                    case 10:
                        {
                            ApplySkillBonus(skills, min, max, 2, 1, 15);
                            break;
                        }
                    case 11:
                        {
                            ApplySkillBonus(skills, min, max, 3, 1, 15);
                            break;
                        }
                    case 12:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerRegCost, 1, 20);
                            break;
                        }
                    case 13:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.LowerManaCost, 1, 8);
                            break;
                        }
                    case 14:
                        {
                            ApplyAttribute(primary, min, max, AosAttribute.RegenMana, 1, 2);
                            break;
                        }
                    case 15:
                        {
                            spellbook.Slayer = GetRandomSlayer();
                            break;
                        }
                }
            }
        }
    }
}
