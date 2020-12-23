using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Items
{
    public class AddonContainerComponent : Item, IChoppable
    {
        [Constructible]
        public AddonContainerComponent(int itemID) : base(itemID)
        {
            Movable = false;

            AddonComponent.ApplyLightTo(this);
        }

        public AddonContainerComponent(Serial serial) : base(serial)
        {
        }

        public virtual bool NeedsWall => false;
        public virtual Point3D WallPosition => Point3D.Zero;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseAddonContainer Addon { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset { get; set; }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;

                if (Addon?.ShareHue == true)
                {
                    Addon.Hue = value;
                }
            }
        }

        public virtual void OnChop(Mobile from)
        {
            if (Addon != null && from.InRange(GetWorldLocation(), 3))
            {
                Addon.OnChop(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (Addon != null)
            {
                return Addon.OnDragDrop(from, dropped);
            }

            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            Addon?.OnComponentUsed(this, from);
        }

        public override void OnLocationChange(Point3D old)
        {
            if (Addon != null)
            {
                Addon.Location = new Point3D(X - Offset.X, Y - Offset.Y, Z - Offset.Z);
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            Addon?.GetContextMenuEntries(from, list);
        }

        public override void OnMapChange()
        {
            if (Addon != null)
            {
                Addon.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Addon?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Addon);
            writer.Write(Offset);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Addon = reader.ReadEntity<BaseAddonContainer>();
            Offset = reader.ReadPoint3D();

            Addon?.OnComponentLoaded(this);

            AddonComponent.ApplyLightTo(this);
        }
    }

    public class LocalizedContainerComponent : AddonContainerComponent
    {
        private int m_LabelNumber;

        public LocalizedContainerComponent(int itemID, int labelNumber) : base(itemID) => m_LabelNumber = labelNumber;

        public LocalizedContainerComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber
        {
            get
            {
                if (m_LabelNumber > 0)
                {
                    return m_LabelNumber;
                }

                return base.LabelNumber;
            }
        }

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
