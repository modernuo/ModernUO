using System;
using System.Collections.Generic;
using Server.Multis;

namespace Server.Items
{
    [Flippable(0xE80, 0x9A8)]
    public class StrongBox : BaseContainer, IChoppable
    {
        private BaseHouse m_House;
        private Mobile m_Owner;

        public StrongBox(Mobile owner, BaseHouse house) : base(0xE80)
        {
            m_Owner = owner;
            m_House = house;

            MaxItems = 25;
        }

        public StrongBox(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 100;
        public override int LabelNumber => 1023712;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get => m_Owner;
            set
            {
                m_Owner = value;
                InvalidateProperties();
            }
        }

        public override int DefaultMaxWeight => 0;

        public override bool Decays => m_House == null || m_Owner?.Deleted != false || !m_House.IsCoOwner(m_Owner);

        public override TimeSpan DecayTime => TimeSpan.FromMinutes(30.0);

        public void OnChop(Mobile from)
        {
            if (m_House?.Deleted != false || m_Owner?.Deleted != false || from == m_Owner || m_House.IsOwner(from))
            {
                Chop(from);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Owner);
            writer.Write(m_House);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Owner = reader.ReadEntity<Mobile>();
                        m_House = reader.ReadEntity<BaseHouse>();

                        break;
                    }
            }

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), Validate);
        }

        private void Validate()
        {
            if (m_Owner != null && m_House?.IsCoOwner(m_Owner) == false)
            {
                Console.WriteLine("Warning: Destroying strongbox of {0}", m_Owner.Name);
                Destroy();
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Owner != null)
            {
                list.Add(1042887, m_Owner.Name); // a strong box owned by ~1_OWNER_NAME~
            }
            else
            {
                base.AddNameProperty(list);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Owner != null)
            {
                LabelTo(from, 1042887, m_Owner.Name); // a strong box owned by ~1_OWNER_NAME~

                if (CheckContentDisplay(from))
                {
                    LabelTo(from, "({0} items, {1} stones)", TotalItems, TotalWeight);
                }
            }
            else
            {
                base.OnSingleClick(from);
            }
        }

        public override bool IsAccessibleTo(Mobile m) =>
            m_Owner?.Deleted != false || m_House?.Deleted != false ||
            m.AccessLevel >= AccessLevel.GameMaster ||
            m == m_Owner && m_House.IsCoOwner(m) && base.IsAccessibleTo(m);

        private void Chop(Mobile from)
        {
            Effects.PlaySound(Location, Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Destroy();
        }

        public Container ConvertToStandardContainer()
        {
            Container metalBox = new MetalBox();
            var subItems = new List<Item>(Items);

            foreach (var subItem in subItems)
            {
                metalBox.AddItem(subItem);
            }

            Delete();

            return metalBox;
        }
    }
}
