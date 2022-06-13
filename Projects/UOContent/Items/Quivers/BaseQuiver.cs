using System;
using Server.Engines.Craft;

namespace Server.Items
{
    public class BaseQuiver : Container, ICraftable
    {
        private static readonly Type[] m_Ammo =
        {
            typeof(Arrow), typeof(Bolt)
        };

        private int m_Capacity;

        private Mobile m_Crafter;
        private int m_DamageIncrease;
        private int m_LowerAmmoCost;
        private ClothingQuality m_Quality;
        private int m_WeightReduction;

        public BaseQuiver(int itemID = 0x2FB7) : base(itemID)
        {
            Weight = 2.0;
            Capacity = 500;
            Layer = Layer.Cloak;

            Attributes = new AosAttributes(this);

            DamageIncrease = 10;
        }

        public BaseQuiver(Serial serial) : base(serial)
        {
        }

        public override int DefaultGumpID => 0x108;
        public override int DefaultMaxItems => 1;
        public override int DefaultMaxWeight => 50;
        public override double DefaultWeight => 2.0;

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosAttributes Attributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Capacity
        {
            get => m_Capacity;
            set
            {
                m_Capacity = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LowerAmmoCost
        {
            get => m_LowerAmmoCost;
            set
            {
                m_LowerAmmoCost = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeightReduction
        {
            get => m_WeightReduction;
            set
            {
                m_WeightReduction = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamageIncrease
        {
            get => m_DamageIncrease;
            set
            {
                m_DamageIncrease = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get => m_Crafter;
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ClothingQuality Quality
        {
            get => m_Quality;
            set
            {
                m_Quality = value;
                InvalidateProperties();
            }
        }

        public Item Ammo => Items.Count > 0 ? Items[0] : null;

        public virtual int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            BaseTool tool, CraftItem craftItem, int resHue
        )
        {
            Quality = (ClothingQuality)quality;

            if (makersMark)
            {
                Crafter = from;
            }

            return quality;
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not BaseQuiver quiver)
            {
                return;
            }

            quiver.Attributes = new AosAttributes(newItem, Attributes);
        }

        public override void UpdateTotal(Item sender, TotalType type, int delta)
        {
            InvalidateProperties();

            base.UpdateTotal(sender, type, delta);
        }

        public override int GetTotal(TotalType type)
        {
            var total = base.GetTotal(type);

            if (type == TotalType.Weight)
            {
                total -= total * m_WeightReduction / 100;
            }

            return total;
        }

        public bool CheckType(Item item)
        {
            var type = item.GetType();
            var ammo = Ammo;

            if (ammo != null)
            {
                if (ammo.GetType() == type)
                {
                    return true;
                }
            }
            else
            {
                for (var i = 0; i < m_Ammo.Length; i++)
                {
                    if (type == m_Ammo[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (!CheckType(item))
            {
                if (message)
                {
                    m.SendLocalizedMessage( 1074836 ); // The container can not hold that type of object.
                }

                return false;
            }

            if (Items.Count < DefaultMaxItems)
            {
                return item.Amount <= m_Capacity && base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
            }

            if (checkItems)
            {
                return false;
            }

            Item ammo = Ammo;

            return ammo?.Deleted == false && ammo.Amount + item.Amount <= m_Capacity;
        }

        public override void AddItem(Item dropped)
        {
            base.AddItem(dropped);

            InvalidateWeight();
        }

        public override void RemoveItem(Item dropped)
        {
            base.RemoveItem(dropped);

            InvalidateWeight();
        }

        public override void OnAdded(IEntity parent)
        {
            if (parent is Mobile mob)
            {
                Attributes.AddStatBonuses(mob);
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            if (parent is Mobile mob)
            {
                Attributes.RemoveStatBonuses(mob);
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Crafter != null)
            {
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~
            }

            if (m_Quality == ClothingQuality.Exceptional)
            {
                list.Add(1063341); // exceptional
            }

            var ammo = Ammo;

            if (ammo != null)
            {
                if (ammo is Arrow)
                {
                    list.Add(1075265, $"{ammo.Amount}\t{Capacity}"); // Ammo: ~1_QUANTITY~/~2_CAPACITY~ arrows
                }
                else if (ammo is Bolt)
                {
                    list.Add(1075266, $"{ammo.Amount}\t{Capacity}"); // Ammo: ~1_QUANTITY~/~2_CAPACITY~ bolts
                }
            }
            else
            {
                list.Add(1075265, $"0\t{Capacity}"); // Ammo: ~1_QUANTITY~/~2_CAPACITY~ arrows
            }

            int prop;

            if ((prop = m_DamageIncrease) != 0)
            {
                list.Add(1074762, prop); // Damage modifier: ~1_PERCENT~%
            }

            int phys = 0, fire = 0, cold = 0, pois = 0, nrgy = 0, chaos = 0, direct = 0;

            AlterBowDamage(ref phys, ref fire, ref cold, ref pois, ref nrgy, ref chaos, ref direct);

            if (phys != 0)
            {
                list.Add(1060403, phys); // physical damage ~1_val~%
            }

            if (fire != 0)
            {
                list.Add(1060405, fire); // fire damage ~1_val~%
            }

            if (cold != 0)
            {
                list.Add(1060404, cold); // cold damage ~1_val~%
            }

            if (pois != 0)
            {
                list.Add(1060406, pois); // poison damage ~1_val~%
            }

            if (nrgy != 0)
            {
                list.Add(1060407, nrgy); // energy damage ~1_val
            }

            if (chaos != 0)
            {
                list.Add(1072846, chaos); // chaos damage ~1_val~%
            }

            if (direct != 0)
            {
                list.Add(1079978, direct); // Direct Damage: ~1_PERCENT~%
            }

            list.Add(1075085); // Requirement: Mondain's Legacy

            if ((prop = Attributes.DefendChance) != 0)
            {
                list.Add(1060408, prop); // defense chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusDex) != 0)
            {
                list.Add(1060409, prop); // dexterity bonus ~1_val~
            }

            if ((prop = Attributes.EnhancePotions) != 0)
            {
                list.Add(1060411, prop); // enhance potions ~1_val~%
            }

            if ((prop = Attributes.CastRecovery) != 0)
            {
                list.Add(1060412, prop); // faster cast recovery ~1_val~
            }

            if ((prop = Attributes.CastSpeed) != 0)
            {
                list.Add(1060413, prop); // faster casting ~1_val~
            }

            if ((prop = Attributes.AttackChance) != 0)
            {
                list.Add(1060415, prop); // hit chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusHits) != 0)
            {
                list.Add(1060431, prop); // hit point increase ~1_val~
            }

            if ((prop = Attributes.BonusInt) != 0)
            {
                list.Add(1060432, prop); // intelligence bonus ~1_val~
            }

            if ((prop = Attributes.LowerManaCost) != 0)
            {
                list.Add(1060433, prop); // lower mana cost ~1_val~%
            }

            if ((prop = Attributes.LowerRegCost) != 0)
            {
                list.Add(1060434, prop); // lower reagent cost ~1_val~%
            }

            if ((prop = Attributes.Luck) != 0)
            {
                list.Add(1060436, prop); // luck ~1_val~
            }

            if ((prop = Attributes.BonusMana) != 0)
            {
                list.Add(1060439, prop); // mana increase ~1_val~
            }

            if ((prop = Attributes.RegenMana) != 0)
            {
                list.Add(1060440, prop); // mana regeneration ~1_val~
            }

            if (Attributes.NightSight != 0)
            {
                list.Add(1060441); // night sight
            }

            if ((prop = Attributes.ReflectPhysical) != 0)
            {
                list.Add(1060442, prop); // reflect physical damage ~1_val~%
            }

            if ((prop = Attributes.RegenStam) != 0)
            {
                list.Add(1060443, prop); // stamina regeneration ~1_val~
            }

            if ((prop = Attributes.RegenHits) != 0)
            {
                list.Add(1060444, prop); // hit point regeneration ~1_val~
            }

            if ((prop = Attributes.SpellDamage) != 0)
            {
                list.Add(1060483, prop); // spell damage increase ~1_val~%
            }

            if ((prop = Attributes.BonusStam) != 0)
            {
                list.Add(1060484, prop); // stamina increase ~1_val~
            }

            if ((prop = Attributes.BonusStr) != 0)
            {
                list.Add(1060485, prop); // strength bonus ~1_val~
            }

            if ((prop = Attributes.WeaponSpeed) != 0)
            {
                list.Add(1060486, prop); // swing speed increase ~1_val~%
            }

            if ((prop = m_LowerAmmoCost) > 0)
            {
                list.Add(1075208, prop); // Lower Ammo Cost ~1_Percentage~%
            }

            var weight = ammo != null ? ammo.Weight + ammo.Amount : 0;

            list.Add(
                1072241, // Contents: ~1_COUNT~/~2_MAXCOUNT items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
                $"{Items.Count}\t{DefaultMaxItems}\t{(int)weight}\t{DefaultMaxWeight}"
            );

            if ((prop = m_WeightReduction) != 0)
            {
                list.Add(1072210, prop); // Weight reduction: ~1_PERCENTAGE~%
            }
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
            {
                flags |= toSet;
            }
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            var flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.Attributes, !Attributes.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.LowerAmmoCost, m_LowerAmmoCost != 0);
            SetSaveFlag(ref flags, SaveFlag.WeightReduction, m_WeightReduction != 0);
            SetSaveFlag(ref flags, SaveFlag.DamageIncrease, m_DamageIncrease != 0);
            SetSaveFlag(ref flags, SaveFlag.Crafter, m_Crafter != null);
            SetSaveFlag(ref flags, SaveFlag.Quality, true);
            SetSaveFlag(ref flags, SaveFlag.Capacity, m_Capacity > 0);

            writer.WriteEncodedInt((int)flags);

            if (GetSaveFlag(flags, SaveFlag.Attributes))
            {
                Attributes.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.LowerAmmoCost))
            {
                writer.Write(m_LowerAmmoCost);
            }

            if (GetSaveFlag(flags, SaveFlag.WeightReduction))
            {
                writer.Write(m_WeightReduction);
            }

            if (GetSaveFlag(flags, SaveFlag.DamageIncrease))
            {
                writer.Write(m_DamageIncrease);
            }

            if (GetSaveFlag(flags, SaveFlag.Crafter))
            {
                writer.Write(m_Crafter);
            }

            if (GetSaveFlag(flags, SaveFlag.Quality))
            {
                writer.Write((int)m_Quality);
            }

            if (GetSaveFlag(flags, SaveFlag.Capacity))
            {
                writer.Write(m_Capacity);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            var flags = (SaveFlag)reader.ReadEncodedInt();

            Attributes = new AosAttributes(this);

            if (GetSaveFlag(flags, SaveFlag.Attributes))
            {
                Attributes.Deserialize(reader);
            }

            if (GetSaveFlag(flags, SaveFlag.LowerAmmoCost))
            {
                m_LowerAmmoCost = reader.ReadInt();
            }

            if (GetSaveFlag(flags, SaveFlag.WeightReduction))
            {
                m_WeightReduction = reader.ReadInt();
            }

            if (GetSaveFlag(flags, SaveFlag.DamageIncrease))
            {
                m_DamageIncrease = reader.ReadInt();
            }

            if (GetSaveFlag(flags, SaveFlag.Crafter))
            {
                m_Crafter = reader.ReadEntity<Mobile>();
            }

            if (GetSaveFlag(flags, SaveFlag.Quality))
            {
                m_Quality = (ClothingQuality)reader.ReadInt();
            }

            if (GetSaveFlag(flags, SaveFlag.Capacity))
            {
                m_Capacity = reader.ReadInt();
            }
        }

        public virtual void AlterBowDamage(
            ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy, ref int chaos, ref int direct
        )
        {
        }

        public void InvalidateWeight()
        {
            if (RootParent is Mobile m)
            {
                m.UpdateTotals();
            }
        }

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            Attributes = 0x00000001,
            DamageModifier = 0x00000002,
            LowerAmmoCost = 0x00000004,
            WeightReduction = 0x00000008,
            Crafter = 0x00000010,
            Quality = 0x00000020,
            Capacity = 0x00000040,
            DamageIncrease = 0x00000080
        }
    }
}
