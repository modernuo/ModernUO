using System;
using Server.Engines.Craft;

namespace Server.Items
{
    public enum GemType
    {
        None,
        StarSapphire,
        Emerald,
        Sapphire,
        Ruby,
        Citrine,
        Amethyst,
        Tourmaline,
        Amber,
        Diamond
    }

    public abstract class BaseJewel : Item, ICraftable
    {
        private GemType m_GemType;
        private int m_HitPoints;
        private int m_MaxHitPoints;
        private CraftResource m_Resource;

        public BaseJewel(int itemID, Layer layer) : base(itemID)
        {
            Attributes = new AosAttributes(this);
            Resistances = new AosElementAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);
            m_Resource = CraftResource.Iron;
            m_GemType = GemType.None;

            Layer = layer;

            m_HitPoints = m_MaxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);
        }

        public BaseJewel(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHitPoints
        {
            get => m_MaxHitPoints;
            set
            {
                m_MaxHitPoints = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPoints
        {
            get => m_HitPoints;
            set
            {
                if (value != m_HitPoints && MaxHitPoints > 0)
                {
                    m_HitPoints = value;

                    if (m_HitPoints < 0)
                    {
                        Delete();
                    }
                    else if (m_HitPoints > MaxHitPoints)
                    {
                        m_HitPoints = MaxHitPoints;
                    }

                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosAttributes Attributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosElementAttributes Resistances { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosSkillBonuses SkillBonuses { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                m_Resource = value;
                Hue = CraftResources.GetHue(m_Resource);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public GemType GemType
        {
            get => m_GemType;
            set
            {
                m_GemType = value;
                InvalidateProperties();
            }
        }

        public override int PhysicalResistance => Resistances.Physical;
        public override int FireResistance => Resistances.Fire;
        public override int ColdResistance => Resistances.Cold;
        public override int PoisonResistance => Resistances.Poison;
        public override int EnergyResistance => Resistances.Energy;
        public virtual int BaseGemTypeNumber => 0;

        public virtual int InitMinHits => 0;
        public virtual int InitMaxHits => 0;

        public override int LabelNumber
        {
            get
            {
                if (m_GemType == GemType.None)
                {
                    return base.LabelNumber;
                }

                return BaseGemTypeNumber + (int)m_GemType - 1;
            }
        }

        public virtual int ArtifactRarity => 0;

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            var context = craftSystem.GetContext(from);

            if (context?.DoNotColor == true)
            {
                Hue = 0;
            }

            if (craftItem.Resources.Count > 1)
            {
                resourceType = craftItem.Resources[1].ItemType;

                if (resourceType == typeof(StarSapphire))
                {
                    GemType = GemType.StarSapphire;
                }
                else if (resourceType == typeof(Emerald))
                {
                    GemType = GemType.Emerald;
                }
                else if (resourceType == typeof(Sapphire))
                {
                    GemType = GemType.Sapphire;
                }
                else if (resourceType == typeof(Ruby))
                {
                    GemType = GemType.Ruby;
                }
                else if (resourceType == typeof(Citrine))
                {
                    GemType = GemType.Citrine;
                }
                else if (resourceType == typeof(Amethyst))
                {
                    GemType = GemType.Amethyst;
                }
                else if (resourceType == typeof(Tourmaline))
                {
                    GemType = GemType.Tourmaline;
                }
                else if (resourceType == typeof(Amber))
                {
                    GemType = GemType.Amber;
                }
                else if (resourceType == typeof(Diamond))
                {
                    GemType = GemType.Diamond;
                }
            }

            return 1;
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (!(newItem is BaseJewel jewel))
            {
                return;
            }

            jewel.Attributes = new AosAttributes(newItem, Attributes);
            jewel.Resistances = new AosElementAttributes(newItem, Resistances);
            jewel.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
        }

        public override void OnAdded(IEntity parent)
        {
            if (Core.AOS && parent is Mobile from)
            {
                SkillBonuses.AddTo(from);

                var strBonus = Attributes.BonusStr;
                var dexBonus = Attributes.BonusDex;
                var intBonus = Attributes.BonusInt;

                if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
                {
                    var modName = Serial.ToString();

                    if (strBonus != 0)
                    {
                        from.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));
                    }

                    if (dexBonus != 0)
                    {
                        from.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));
                    }

                    if (intBonus != 0)
                    {
                        from.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
                    }
                }

                from.CheckStatTimers();
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            if (Core.AOS && parent is Mobile from)
            {
                SkillBonuses.Remove();

                var modName = Serial.ToString();

                from.RemoveStatMod($"{modName}Str");
                from.RemoveStatMod($"{modName}Dex");
                from.RemoveStatMod($"{modName}Int");

                from.CheckStatTimers();
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            SkillBonuses.GetProperties(list);

            int prop;

            if ((prop = ArtifactRarity) > 0)
            {
                list.Add(1061078, prop.ToString()); // artifact rarity ~1_val~
            }

            if ((prop = Attributes.WeaponDamage) != 0)
            {
                list.Add(1060401, prop.ToString()); // damage increase ~1_val~%
            }

            if ((prop = Attributes.DefendChance) != 0)
            {
                list.Add(1060408, prop.ToString()); // defense chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusDex) != 0)
            {
                list.Add(1060409, prop.ToString()); // dexterity bonus ~1_val~
            }

            if ((prop = Attributes.EnhancePotions) != 0)
            {
                list.Add(1060411, prop.ToString()); // enhance potions ~1_val~%
            }

            if ((prop = Attributes.CastRecovery) != 0)
            {
                list.Add(1060412, prop.ToString()); // faster cast recovery ~1_val~
            }

            if ((prop = Attributes.CastSpeed) != 0)
            {
                list.Add(1060413, prop.ToString()); // faster casting ~1_val~
            }

            if ((prop = Attributes.AttackChance) != 0)
            {
                list.Add(1060415, prop.ToString()); // hit chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusHits) != 0)
            {
                list.Add(1060431, prop.ToString()); // hit point increase ~1_val~
            }

            if ((prop = Attributes.BonusInt) != 0)
            {
                list.Add(1060432, prop.ToString()); // intelligence bonus ~1_val~
            }

            if ((prop = Attributes.LowerManaCost) != 0)
            {
                list.Add(1060433, prop.ToString()); // lower mana cost ~1_val~%
            }

            if ((prop = Attributes.LowerRegCost) != 0)
            {
                list.Add(1060434, prop.ToString()); // lower reagent cost ~1_val~%
            }

            if ((prop = Attributes.Luck) != 0)
            {
                list.Add(1060436, prop.ToString()); // luck ~1_val~
            }

            if ((prop = Attributes.BonusMana) != 0)
            {
                list.Add(1060439, prop.ToString()); // mana increase ~1_val~
            }

            if ((prop = Attributes.RegenMana) != 0)
            {
                list.Add(1060440, prop.ToString()); // mana regeneration ~1_val~
            }

            if (Attributes.NightSight != 0)
            {
                list.Add(1060441); // night sight
            }

            if ((prop = Attributes.ReflectPhysical) != 0)
            {
                list.Add(1060442, prop.ToString()); // reflect physical damage ~1_val~%
            }

            if ((prop = Attributes.RegenStam) != 0)
            {
                list.Add(1060443, prop.ToString()); // stamina regeneration ~1_val~
            }

            if ((prop = Attributes.RegenHits) != 0)
            {
                list.Add(1060444, prop.ToString()); // hit point regeneration ~1_val~
            }

            if (Attributes.SpellChanneling != 0)
            {
                list.Add(1060482); // spell channeling
            }

            if ((prop = Attributes.SpellDamage) != 0)
            {
                list.Add(1060483, prop.ToString()); // spell damage increase ~1_val~%
            }

            if ((prop = Attributes.BonusStam) != 0)
            {
                list.Add(1060484, prop.ToString()); // stamina increase ~1_val~
            }

            if ((prop = Attributes.BonusStr) != 0)
            {
                list.Add(1060485, prop.ToString()); // strength bonus ~1_val~
            }

            if ((prop = Attributes.WeaponSpeed) != 0)
            {
                list.Add(1060486, prop.ToString()); // swing speed increase ~1_val~%
            }

            if (Core.ML && (prop = Attributes.IncreasedKarmaLoss) != 0)
            {
                list.Add(1075210, prop.ToString()); // Increased Karma Loss ~1val~%
            }

            AddResistanceProperties(list);

            if (m_HitPoints >= 0 && m_MaxHitPoints > 0)
            {
                list.Add(1060639, "{0}\t{1}", m_HitPoints, m_MaxHitPoints); // durability ~1_val~ / ~2_val~
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3); // version

            writer.WriteEncodedInt(m_MaxHitPoints);
            writer.WriteEncodedInt(m_HitPoints);

            writer.WriteEncodedInt((int)m_Resource);
            writer.WriteEncodedInt((int)m_GemType);

            Attributes.Serialize(writer);
            Resistances.Serialize(writer);
            SkillBonuses.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_MaxHitPoints = reader.ReadEncodedInt();
                        m_HitPoints = reader.ReadEncodedInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Resource = (CraftResource)reader.ReadEncodedInt();
                        m_GemType = (GemType)reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        Attributes = new AosAttributes(this, reader);
                        Resistances = new AosElementAttributes(this, reader);
                        SkillBonuses = new AosSkillBonuses(this, reader);

                        var m = Parent as Mobile;

                        if (Core.AOS && m != null)
                        {
                            SkillBonuses.AddTo(m);
                        }

                        var strBonus = Attributes.BonusStr;
                        var dexBonus = Attributes.BonusDex;
                        var intBonus = Attributes.BonusInt;

                        if (m != null && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
                        {
                            var modName = Serial.ToString();

                            if (strBonus != 0)
                            {
                                m.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));
                            }

                            if (dexBonus != 0)
                            {
                                m.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));
                            }

                            if (intBonus != 0)
                            {
                                m.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
                            }
                        }

                        m?.CheckStatTimers();

                        break;
                    }
                case 0:
                    {
                        Attributes = new AosAttributes(this);
                        Resistances = new AosElementAttributes(this);
                        SkillBonuses = new AosSkillBonuses(this);

                        break;
                    }
            }

            if (version < 2)
            {
                m_Resource = CraftResource.Iron;
                m_GemType = GemType.None;
            }
        }
    }
}
