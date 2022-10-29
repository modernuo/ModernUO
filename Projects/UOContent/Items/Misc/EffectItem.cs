using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EffectItem : Item
{
    private static Queue<EffectItem> _free = new(); // List of available EffectItems

    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(5.0);

    private EffectItem() : base(1) => Movable = false;

    public override bool Decays => true;

    public static EffectItem Create(Point3D p, Map map, TimeSpan duration)
    {
        EffectItem item = null;

        while (_free.Count > 0)
        {
            var free = _free.Dequeue();
            if (!free.Deleted && free.Map == Map.Internal)
            {
                item = free;
                break;
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

    public void BeginFree(TimeSpan duration) => new FreeTimer(this, duration).Start();

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }

    private class FreeTimer : Timer
    {
        private EffectItem _item;

        public FreeTimer(EffectItem item, TimeSpan delay) : base(delay) => _item = item;

        protected override void OnTick()
        {
            _item.Internalize();
            _free.Enqueue(_item);
        }
    }
}
