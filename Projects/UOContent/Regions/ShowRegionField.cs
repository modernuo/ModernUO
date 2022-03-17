using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Regions
{
    public class ShowRegionField : Item
    {
        private DateTime m_End;
        private Timer m_Timer;

        public ShowRegionField(
            int itemID, Point3D loc,  Map map, TimeSpan duration
        ) : base(itemID)
        {
            Visible = false;
            Movable = false;
            Light = LightType.Circle300;

            MoveToWorld(loc, map);

            m_End = Core.Now + duration;

            m_Timer = new InternalTimer(this, duration);
            m_Timer.Start();
        }

        public ShowRegionField(Serial serial) : base(serial)
        {
        }

        public override bool BlocksFit => true;

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Timer?.Stop();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
            writer.WriteDeltaTime(m_End);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            m_End = reader.ReadDeltaTime();
            m_Timer = new InternalTimer(this, TimeSpan.Zero);
            m_Timer.Start();
        }

        private class InternalTimer : Timer
        {
            private readonly ShowRegionField m_Item;

            public InternalTimer(ShowRegionField item, TimeSpan delay) : base(delay, TimeSpan.FromSeconds(1.0))
            {
                m_Item = item;
            }

            protected override void OnTick()
            {
               if (Core.Now > m_Item.m_End)
                {
                    m_Item.Delete();
                    Stop();
                }
            }
        }
    }
}

