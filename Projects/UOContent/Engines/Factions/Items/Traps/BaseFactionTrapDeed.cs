using System;
using Server.Engines.Craft;
using Server.Items;
using Server.Utilities;

namespace Server.Factions
{
    public abstract class BaseFactionTrapDeed : Item, ICraftable
    {
        private Faction m_Faction;

        public BaseFactionTrapDeed(int itemID = 0x14F0) : base(itemID)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public BaseFactionTrapDeed(Serial serial) : base(serial)
        {
        }

        public abstract Type TrapType { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Faction Faction
        {
            get => m_Faction;
            set
            {
                m_Faction = value;

                if (m_Faction != null)
                {
                    Hue = m_Faction.Definition.HuePrimary;
                }
            }
        }

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            ItemID = 0x14F0;
            Faction = Faction.Find(from);

            return 1;
        }

        public virtual BaseFactionTrap Construct(Mobile from)
        {
            try
            {
                return TrapType.CreateInstance<BaseFactionTrap>(m_Faction, from);
            }
            catch
            {
                return null;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            var faction = Faction.Find(from);

            if (faction == null)
            {
                from.SendLocalizedMessage(1010353, "", 0x23); // Only faction members may place faction traps
            }
            else if (faction != m_Faction)
            {
                from.SendLocalizedMessage(1010354, "", 0x23); // You may only place faction traps created by your faction
            }
            else if (faction.Traps.Count >= faction.MaximumTraps)
            {
                from.SendLocalizedMessage(1010358, "", 0x23); // Your faction already has the maximum number of traps placed
            }
            else
            {
                var trap = Construct(from);

                if (trap == null)
                {
                    return;
                }

                var message = trap.IsValidLocation(from.Location, from.Map);

                if (message > 0)
                {
                    from.SendLocalizedMessage(message, "", 0x23);
                    trap.Delete();
                }
                else
                {
                    from.SendLocalizedMessage(1010360); // You arm the trap and carefully hide it from view
                    trap.MoveToWorld(from.Location, from.Map);
                    faction.Traps.Add(trap);
                    Delete();
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            Faction.WriteReference(writer, m_Faction);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Faction = Faction.ReadReference(reader);
        }
    }
}
