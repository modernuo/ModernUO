using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Multis
{
    public class PreviewHouse : BaseMulti
    {
        private List<Item> m_Components;
        private Timer m_Timer;

        public PreviewHouse(int multiID) : base(multiID)
        {
            m_Components = new List<Item>();

            var mcl = Components;

            for (var i = 1; i < mcl.List.Length; ++i)
            {
                var entry = mcl.List[i];

                if (entry.Flags == 0)
                {
                    Item item = new Static(entry.ItemId);

                    item.MoveToWorld(new Point3D(X + entry.OffsetX, Y + entry.OffsetY, Z + entry.OffsetZ), Map);

                    m_Components.Add(item);
                }
            }

            m_Timer = new DecayTimer(this);
            m_Timer.Start();
        }

        public PreviewHouse(Serial serial) : base(serial)
        {
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            if (m_Components == null)
            {
                return;
            }

            var xOffset = X - oldLocation.X;
            var yOffset = Y - oldLocation.Y;
            var zOffset = Z - oldLocation.Z;

            for (var i = 0; i < m_Components.Count; ++i)
            {
                var item = m_Components[i];

                item.MoveToWorld(new Point3D(item.X + xOffset, item.Y + yOffset, item.Z + zOffset), Map);
            }
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Components == null)
            {
                return;
            }

            for (var i = 0; i < m_Components.Count; ++i)
            {
                var item = m_Components[i];

                item.Map = Map;
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (m_Components == null)
            {
                return;
            }

            for (var i = 0; i < m_Components.Count; ++i)
            {
                var item = m_Components[i];

                item.Delete();
            }
        }

        public override void OnAfterDelete()
        {
            m_Timer?.Stop();

            m_Timer = null;

            base.OnAfterDelete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Components);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Components = reader.ReadEntityList<Item>();

                        break;
                    }
            }

            Timer.StartTimer(Delete);
        }

        private class DecayTimer : Timer
        {
            private readonly Item m_Item;

            public DecayTimer(Item item) : base(TimeSpan.FromSeconds(20.0))
            {
                m_Item = item;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
            }
        }
    }
}
