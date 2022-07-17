using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Engines.Harvest;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public interface IUsesRemaining
    {
        int UsesRemaining { get; set; }
        bool ShowUsesRemaining { get; set; }
    }

    public abstract class BaseHarvestTool : Item, IUsesRemaining, ICraftable
    {
        private Mobile m_Crafter;
        private ToolQuality m_Quality;
        private int m_UsesRemaining;

        public BaseHarvestTool(int itemID, int usesRemaining = 50) : base(itemID)
        {
            m_UsesRemaining = usesRemaining;
            m_Quality = ToolQuality.Regular;
        }

        public BaseHarvestTool(Serial serial) : base(serial)
        {
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
        public ToolQuality Quality
        {
            get => m_Quality;
            set
            {
                UnscaleUses();
                m_Quality = value;
                InvalidateProperties();
                ScaleUses();
            }
        }

        public abstract HarvestSystem HarvestSystem { get; }

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            Quality = (ToolQuality)quality;

            if (makersMark)
            {
                Crafter = from;
            }

            return quality;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => m_UsesRemaining;
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        bool IUsesRemaining.ShowUsesRemaining
        {
            get => true;
            set { }
        }

        public void ScaleUses()
        {
            m_UsesRemaining = m_UsesRemaining * GetUsesScalar() / 100;
            InvalidateProperties();
        }

        public void UnscaleUses()
        {
            m_UsesRemaining = m_UsesRemaining * 100 / GetUsesScalar();
        }

        public int GetUsesScalar()
        {
            if (m_Quality == ToolQuality.Exceptional)
            {
                return 200;
            }

            return 100;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            // Makers mark not displayed on OSI
            // if (m_Crafter != null)
            // list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

            if (m_Quality == ToolQuality.Exceptional)
            {
                list.Add(1060636); // exceptional
            }

            list.Add(1060584, m_UsesRemaining); // uses remaining: ~1_val~
        }

        public virtual void DisplayDurabilityTo(Mobile m)
        {
            LabelToAffix(m, 1017323, AffixType.Append, $": {m_UsesRemaining}"); // Durability
        }

        public override void OnSingleClick(Mobile from)
        {
            DisplayDurabilityTo(from);

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) || Parent == from)
            {
                HarvestSystem.BeginHarvesting(from, this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            AddContextMenuEntries(from, this, list, HarvestSystem);
        }

        public static void AddContextMenuEntries(Mobile from, Item item, List<ContextMenuEntry> list, HarvestSystem system)
        {
            if (system != Mining.System)
            {
                return;
            }

            if (!item.IsChildOf(from.Backpack) && item.Parent != from)
            {
                return;
            }

            if (from is not PlayerMobile pm)
            {
                return;
            }

            var miningEntry = new ContextMenuEntry(pm.ToggleMiningStone ? 6179 : 6178);
            miningEntry.Color = 0x421F;
            list.Add(miningEntry);

            list.Add(new ToggleMiningStoneEntry(pm, false, 6176));
            list.Add(new ToggleMiningStoneEntry(pm, true, 6177));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_Crafter);
            writer.Write((int)m_Quality);

            writer.Write(m_UsesRemaining);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Crafter = reader.ReadEntity<Mobile>();
                        m_Quality = (ToolQuality)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt();
                        break;
                    }
            }
        }

        private class ToggleMiningStoneEntry : ContextMenuEntry
        {
            private readonly PlayerMobile m_Mobile;
            private readonly bool m_Value;

            public ToggleMiningStoneEntry(PlayerMobile mobile, bool value, int number) : base(number)
            {
                m_Mobile = mobile;
                m_Value = value;

                var stoneMining = mobile.StoneMining && mobile.Skills.Mining.Base >= 100.0;

                if (mobile.ToggleMiningStone == value || value && !stoneMining)
                {
                    Flags |= CMEFlags.Disabled;
                }
            }

            public override void OnClick()
            {
                var oldValue = m_Mobile.ToggleMiningStone;

                if (m_Value)
                {
                    if (oldValue)
                    {
                        m_Mobile.SendLocalizedMessage(1054023); // You are already set to mine both ore and stone!
                    }
                    else if (!m_Mobile.StoneMining || m_Mobile.Skills.Mining.Base < 100.0)
                    {
                        m_Mobile.SendLocalizedMessage(
                            1054024
                        ); // You have not learned how to mine stone or you do not have enough skill!
                    }
                    else
                    {
                        m_Mobile.ToggleMiningStone = true;
                        m_Mobile.SendLocalizedMessage(1054022); // You are now set to mine both ore and stone.
                    }
                }
                else
                {
                    if (oldValue)
                    {
                        m_Mobile.ToggleMiningStone = false;
                        m_Mobile.SendLocalizedMessage(1054020); // You are now set to mine only ore.
                    }
                    else
                    {
                        m_Mobile.SendLocalizedMessage(1054021); // You are already set to mine only ore!
                    }
                }
            }
        }
    }
}
