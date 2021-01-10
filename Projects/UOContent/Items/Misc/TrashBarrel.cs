using System;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class TrashBarrel : Container, IChoppable
    {
        private Timer m_Timer;

        [Constructible]
        public TrashBarrel() : base(0xE77)
        {
            Hue = 0x3B2;
            Movable = false;
        }

        public TrashBarrel(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041064; // a trash barrel

        public override int DefaultMaxWeight => 0; // A value of 0 signals unlimited weight

        public override bool IsDecoContainer => false;

        public void OnChop(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(from);

            if (house?.IsCoOwner(from) == true)
            {
                Effects.PlaySound(Location, Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.
                Destroy();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Items.Count > 0)
            {
                m_Timer = new EmptyTimer(this);
                m_Timer.Start();
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!base.OnDragDrop(from, dropped))
            {
                return false;
            }

            if (TotalItems >= 50)
            {
                Empty(501478); // The trash is full!  Emptying!
            }
            else
            {
                SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

                if (m_Timer != null)
                {
                    m_Timer.Stop();
                }
                else
                {
                    m_Timer = new EmptyTimer(this);
                }

                m_Timer.Start();
            }

            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!base.OnDragDropInto(from, item, p))
            {
                return false;
            }

            if (TotalItems >= 50)
            {
                Empty(501478); // The trash is full!  Emptying!
            }
            else
            {
                SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

                if (m_Timer != null)
                {
                    m_Timer.Stop();
                }
                else
                {
                    m_Timer = new EmptyTimer(this);
                }

                m_Timer.Start();
            }

            return true;
        }

        public void Empty(int message)
        {
            var items = Items;

            if (items.Count > 0)
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, message);

                for (var i = items.Count - 1; i >= 0; --i)
                {
                    if (i >= items.Count)
                    {
                        continue;
                    }

                    items[i].Delete();
                }
            }

            m_Timer?.Stop();

            m_Timer = null;
        }

        private class EmptyTimer : Timer
        {
            private readonly TrashBarrel m_Barrel;

            public EmptyTimer(TrashBarrel barrel) : base(TimeSpan.FromMinutes(3.0))
            {
                m_Barrel = barrel;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                m_Barrel.Empty(501479); // Emptying the trashcan!
            }
        }
    }
}
