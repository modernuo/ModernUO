using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Items
{
    [Serializable(0, false)]
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
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        public BaseAddonContainer _addon;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        public Point3D _offset;

        [Hue, CommandProperty(AccessLevel.GameMaster)]
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

        public override void OnDoubleClick(Mobile from) => _addon?.OnComponentUsed(this, @from);

        public override void OnLocationChange(Point3D old)
        {
            if (_addon != null)
            {
                _addon.Location = new Point3D(X - _offset.X, Y - _offset.Y, Z - _offset.Z);
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list) =>
            _addon?.GetContextMenuEntries(from, list);

        public override void OnMapChange()
        {
            if (_addon != null)
            {
                _addon.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _addon?.Delete();
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            _addon?.OnComponentLoaded(this);
            AddonComponent.ApplyLightTo(this);
        }
    }

    public class LocalizedContainerComponent : AddonContainerComponent
    {
        // TODO: Requires private access modifiers
        private int m_LabelNumber;

        public LocalizedContainerComponent(int itemID, int labelNumber) : base(itemID) => m_LabelNumber = labelNumber;

        public LocalizedContainerComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => m_LabelNumber > 0 ? m_LabelNumber : base.LabelNumber;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_LabelNumber);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_LabelNumber = reader.ReadInt();
        }
    }
}
