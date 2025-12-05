using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class PreviewHouse : BaseMulti
{
    [SerializableField(0)]
    private List<Item> _previewComponents;

    private Timer _timer;

    public PreviewHouse(int multiID) : base(multiID)
    {
        var mcl = Components;

        for (var i = 1; i < mcl.List.Length; ++i)
        {
            var entry = mcl.List[i];

            if (entry.Flags == 0)
            {
                Item item = new Static(entry.ItemId);
                item.MoveToWorld(new Point3D(X + entry.OffsetX, Y + entry.OffsetY, Z + entry.OffsetZ), Map);

                _previewComponents ??= [];
                _previewComponents.Add(item);
            }
        }

        _timer = new DecayTimer(this);
        _timer.Start();
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        if (_previewComponents == null)
        {
            return;
        }

        var xOffset = X - oldLocation.X;
        var yOffset = Y - oldLocation.Y;
        var zOffset = Z - oldLocation.Z;

        for (var i = 0; i < _previewComponents.Count; ++i)
        {
            var item = _previewComponents[i];

            item.MoveToWorld(new Point3D(item.X + xOffset, item.Y + yOffset, item.Z + zOffset), Map);
        }
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (_previewComponents == null)
        {
            return;
        }

        for (var i = 0; i < _previewComponents.Count; ++i)
        {
            _previewComponents[i].Map = Map;
        }
    }

    public override void OnDelete()
    {
        base.OnDelete();

        if (_previewComponents == null)
        {
            return;
        }

        for (var i = 0; i < _previewComponents.Count; ++i)
        {
            _previewComponents[i]?.Delete();
        }
    }

    public override void OnAfterDelete()
    {
        _timer?.Stop();
        _timer = null;

        base.OnAfterDelete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }

    private class DecayTimer : Timer
    {
        private readonly Item _item;

        public DecayTimer(Item item) : base(TimeSpan.FromSeconds(20.0))
        {
            _item = item;
        }

        protected override void OnTick()
        {
            _item.Delete();
        }
    }
}
