using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Network;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AddonContainerComponent : Item, IChoppable
    {
        [Constructible]
        public AddonContainerComponent(int itemID) : base(itemID)
        {
            Movable = false;

            AddonComponent.ApplyLightTo(this);
        }

        public virtual bool NeedsWall => false;
        public virtual Point3D WallPosition => Point3D.Zero;

        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private BaseAddonContainer _addon;

        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private Point3D _offset;

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;

                if (_addon?.ShareHue == true)
                {
                    _addon.Hue = value;
                }
            }
        }

        public virtual void OnChop(Mobile from)
        {
            if (_addon != null && from.InRange(GetWorldLocation(), 3))
            {
                _addon.OnChop(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped) => _addon?.OnDragDrop(from, dropped) == true;

        public override void OnDoubleClick(Mobile from) => _addon?.OnComponentUsed(this, from);

        public override void OnLocationChange(Point3D old)
        {
            if (_addon != null)
            {
                _addon.Location = new Point3D(X - _offset.X, Y - _offset.Y, Z - _offset.Z);
            }
        }

        public override void OnMapChange()
        {
            if (_addon != null)
            {
                _addon.Map = Map;
            }
        }

        public override void GetProperties(IPropertyList list) => _addon?.GetProperties(list);

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list) =>
            _addon?.GetContextMenuEntries(from, list);

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _addon?.Delete();
        }

        public override void SendWorldPacketTo(NetState ns, ReadOnlySpan<byte> world = default)
        {
            Span<byte> buffer = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength].InitializePacket();
            var length = OutgoingItemPackets.CreateWorldItem(buffer, this);
            // Use an itemid of a real container
            BinaryPrimitives.WriteUInt16BigEndian(buffer[7..9], (ushort)(_addon?.ItemID ?? 0x9AB));
            ns.Send(buffer[..length]);

            base.SendWorldPacketTo(ns, world);
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            _addon?.OnComponentLoaded(this);
            AddonComponent.ApplyLightTo(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class LocalizedContainerComponent : AddonContainerComponent
    {
        [SerializableField(0, setter: "private")]
        private int _number;

        public LocalizedContainerComponent(int itemID, int labelNumber) : base(itemID) => _number = labelNumber;

        public override int LabelNumber => _number > 0 ? _number : base.LabelNumber;
    }
}
