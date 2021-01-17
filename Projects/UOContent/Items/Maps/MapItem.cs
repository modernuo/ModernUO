using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items
{
    [Flippable(0x14EB, 0x14EC)]
    public class MapItem : Item, ICraftable
    {
        private const int MaxUserPins = 50;
        private bool m_Editable;

        [Constructible]
        public MapItem(Map facet = null) : base(0x14EC)
        {
            Weight = 1.0;

            Width = 200;
            Height = 200;
            Facet = facet;
        }

        public MapItem(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Protected { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Bounds { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Width { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Height { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map Facet { get; set; }

        public List<Point2D> Pins { get; } = new();

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            CraftInit(from);
            return 1;
        }

        public virtual void CraftInit(Mobile from)
        {
        }

        public void SetDisplay(int x1, int y1, int x2, int y2, int w, int h)
        {
            Width = w;
            Height = h;

            if (x1 < 0)
            {
                x1 = 0;
            }

            if (y1 < 0)
            {
                y1 = 0;
            }

            if (x2 >= 5120)
            {
                x2 = 5119;
            }

            if (y2 >= 4096)
            {
                y2 = 4095;
            }

            Bounds = new Rectangle2D(x1, y1, x2 - x1, y2 - y1);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                DisplayTo(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public virtual void DisplayTo(Mobile from)
        {
            var ns = from.NetState;

            if (!ns.NewCharacterList && Facet != null && Facet != Map.Felucca && Facet != Map.Trammel)
            {
                from.SendMessage("You must have client 7.0.13.0 or higher to display this map.");
                return;
            }

            ns.SendMapDetails(this);
            ns.SendMapDisplay(this);

            for (var i = 0; i < Pins.Count; ++i)
            {
                ns.SendMapAddPin(this, Pins[i]);
            }

            ns.SendMapSetEditable(this, ValidateEdit(from));
        }

        public virtual void OnAddPin(Mobile from, int x, int y)
        {
            if (!ValidateEdit(from))
            {
                return;
            }

            if (Pins.Count >= MaxUserPins)
            {
                return;
            }

            Validate(ref x, ref y);
            AddPin(x, y);
        }

        public virtual void OnRemovePin(Mobile from, int number)
        {
            if (!ValidateEdit(from))
            {
                return;
            }

            RemovePin(number);
        }

        public virtual void OnChangePin(Mobile from, int number, int x, int y)
        {
            if (!ValidateEdit(from))
            {
                return;
            }

            Validate(ref x, ref y);
            ChangePin(number, x, y);
        }

        public virtual void OnInsertPin(Mobile from, int number, int x, int y)
        {
            if (!ValidateEdit(from))
            {
                return;
            }

            if (Pins.Count >= MaxUserPins)
            {
                return;
            }

            Validate(ref x, ref y);
            InsertPin(number, x, y);
        }

        public virtual void OnClearPins(Mobile from)
        {
            if (!ValidateEdit(from))
            {
                return;
            }

            ClearPins();
        }

        public virtual void OnToggleEditable(Mobile from)
        {
            if (Validate(from))
            {
                m_Editable = !m_Editable;
            }

            from.NetState.SendMapSetEditable(this, m_Editable && Validate(from));
        }

        public virtual void Validate(ref int x, ref int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
        }

        public virtual bool ValidateEdit(Mobile from) => m_Editable && Validate(from);

        public virtual bool Validate(Mobile from)
        {
            if (!from.CanSee(this) || from.Map != Map || !from.Alive || InSecureTrade)
            {
                return false;
            }

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (!Movable || Protected || !from.InRange(GetWorldLocation(), 2))
            {
                return false;
            }

            return !(RootParent is Mobile && RootParent != from);
        }

        public void ConvertToWorld(int x, int y, out int worldX, out int worldY)
        {
            worldX = Bounds.Width * x / Width + Bounds.X;
            worldY = Bounds.Height * y / Height + Bounds.Y;
        }

        public void ConvertToMap(int x, int y, out int mapX, out int mapY)
        {
            mapX = (x - Bounds.X) * Width / Bounds.Width;
            mapY = (y - Bounds.Y) * Width / Bounds.Height;
        }

        public virtual void AddWorldPin(int x, int y)
        {
            ConvertToMap(x, y, out var mapX, out var mapY);
            AddPin(mapX, mapY);
        }

        public virtual void AddPin(int x, int y)
        {
            Pins.Add(new Point2D(x, y));
        }

        public virtual void RemovePin(int index)
        {
            if (index > 0 && index < Pins.Count)
            {
                Pins.RemoveAt(index);
            }
        }

        public virtual void InsertPin(int index, int x, int y)
        {
            if (index < 0 || index >= Pins.Count)
            {
                Pins.Add(new Point2D(x, y));
            }
            else
            {
                Pins.Insert(index, new Point2D(x, y));
            }
        }

        public virtual void ChangePin(int index, int x, int y)
        {
            if (index >= 0 && index < Pins.Count)
            {
                Pins[index] = new Point2D(x, y);
            }
        }

        public virtual void ClearPins()
        {
            Pins.Clear();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(Bounds);

            writer.Write(Width);
            writer.Write(Height);

            writer.Write(Protected);

            writer.Write(Pins.Count);
            for (var i = 0; i < Pins.Count; ++i)
            {
                writer.Write(Pins[i]);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Bounds = reader.ReadRect2D();

                        Width = reader.ReadInt();
                        Height = reader.ReadInt();

                        Protected = reader.ReadBool();

                        var count = reader.ReadInt();
                        for (var i = 0; i < count; i++)
                        {
                            Pins.Add(reader.ReadPoint2D());
                        }

                        break;
                    }
            }
        }
    }
}
