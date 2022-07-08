using System;
using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x27AC, 0x27F7)]
    public class Shuriken : Item, ICraftable, INinjaAmmo
    {
        private Poison m_Poison;
        private int m_PoisonCharges;
        private int m_UsesRemaining;

        [Constructible]
        public Shuriken(int amount = 1) : base(0x27AC)
        {
            Weight = 1.0;

            m_UsesRemaining = amount;
        }

        public Shuriken(Serial serial) : base(serial)
        {
        }

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            if (quality == 2)
            {
                UsesRemaining *= 2;
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

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get => m_Poison;
            set
            {
                m_Poison = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonCharges
        {
            get => m_PoisonCharges;
            set
            {
                m_PoisonCharges = value;
                InvalidateProperties();
            }
        }

        bool IUsesRemaining.ShowUsesRemaining
        {
            get => true;
            set { }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060584, m_UsesRemaining); // uses remaining: ~1_val~

            if (m_Poison != null && m_PoisonCharges > 0)
            {
                list.Add(1062412 + m_Poison.Level, m_PoisonCharges);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_UsesRemaining);

            writer.Write(m_Poison);
            writer.Write(m_PoisonCharges);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt();

                        m_Poison = reader.ReadPoison();
                        m_PoisonCharges = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}
