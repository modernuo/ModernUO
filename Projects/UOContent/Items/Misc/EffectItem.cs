using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class EffectItem : Item
    {
        private static readonly List<EffectItem> m_Free = new(); // List of available EffectItems

        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(5.0);

        private EffectItem() : base(1) // nodraw
            =>
                Movable = false;

        public EffectItem(Serial serial) : base(serial)
        {
        }

        public override bool Decays => true;

        public static EffectItem Create(Point3D p, Map map, TimeSpan duration)
        {
            EffectItem item = null;

            for (var i = m_Free.Count - 1; item == null && i >= 0; --i) // We reuse new entries first so decay works better
            {
                var free = m_Free[i];

                m_Free.RemoveAt(i);

                if (!free.Deleted && free.Map == Map.Internal)
                {
                    item = free;
                }
            }

            if (item == null)
            {
                item = new EffectItem();
            }
            else
            {
                item.ItemID = 1;
            }

            item.MoveToWorld(p, map);
            item.BeginFree(duration);

            return item;
        }

        public void BeginFree(TimeSpan duration)
        {
            new FreeTimer(this, duration).Start();
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

            Delete();
        }

        private class FreeTimer : Timer
        {
            private readonly EffectItem m_Item;

            public FreeTimer(EffectItem item, TimeSpan delay) : base(delay)
            {
                m_Item = item;
            }

            protected override void OnTick()
            {
                m_Item.Internalize();

                m_Free.Add(m_Item);
            }
        }
    }
}
