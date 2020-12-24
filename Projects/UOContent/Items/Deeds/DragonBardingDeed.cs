using System;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    [TypeAlias("Server.Items.DragonBarding")]
    public class DragonBardingDeed : Item, ICraftable
    {
        private Mobile m_Crafter;
        private bool m_Exceptional;
        private CraftResource m_Resource;

        public DragonBardingDeed() : base(0x14F0) => Weight = 1.0;

        public DragonBardingDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => m_Exceptional ? 1053181 : 1053012; // dragon barding deed

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
        public bool Exceptional
        {
            get => m_Exceptional;
            set
            {
                m_Exceptional = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                m_Resource = value;
                Hue = CraftResources.GetHue(value);
                InvalidateProperties();
            }
        }

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            Exceptional = quality >= 2;

            if (makersMark)
            {
                Crafter = from;
            }

            var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            var context = craftSystem.GetContext(from);

            if (context?.DoNotColor == true)
            {
                Hue = 0;
            }

            return quality;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Exceptional && m_Crafter != null)
            {
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.BeginTarget(6, false, TargetFlags.None, OnTarget);
                from.SendLocalizedMessage(1053024); // Select the swamp dragon you wish to place the barding on.
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public virtual void OnTarget(Mobile from, object obj)
        {
            if (Deleted)
            {
                return;
            }

            if (!(obj is SwampDragon pet) || pet.HasBarding)
            {
                from.SendLocalizedMessage(1053025); // That is not an unarmored swamp dragon.
            }
            else if (!pet.Controlled || pet.ControlMaster != from)
            {
                from.SendLocalizedMessage(1053026); // You can only put barding on a tamed swamp dragon that you own.
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
            }
            else
            {
                pet.BardingExceptional = Exceptional;
                pet.BardingCrafter = Crafter;
                pet.BardingHP = pet.BardingMaxHP;
                pet.BardingResource = Resource;
                pet.HasBarding = true;
                pet.Hue = Hue;

                Delete();

                from.SendLocalizedMessage(
                    1053027
                ); // You place the barding on your swamp dragon.  Use a bladed item on your dragon to remove the armor.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_Exceptional);
            writer.Write(m_Crafter);
            writer.Write((int)m_Resource);
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
                        m_Exceptional = reader.ReadBool();
                        m_Crafter = reader.ReadEntity<Mobile>();

                        if (version < 1)
                        {
                            reader.ReadInt();
                        }

                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }
        }
    }
}
