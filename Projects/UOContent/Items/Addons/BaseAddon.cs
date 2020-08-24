using System.Collections.Generic;
using Server.Multis;

namespace Server.Items
{
    public enum AddonFitResult
    {
        Valid,
        Blocked,
        NotInHouse,
        DoorTooClose,
        NoWall,
        DoorsNotClosed
    }

    public interface IAddon
    {
        Item Deed { get; }

        bool CouldFit(IPoint3D p, Map map);
    }

    public abstract class BaseAddon : Item, IChoppable, IAddon
    {
        public BaseAddon() : base(1)
        {
            Movable = false;
            Visible = false;

            Components = new List<AddonComponent>();
        }

        public BaseAddon(Serial serial) : base(serial)
        {
        }

        public virtual bool RetainDeedHue => false;

        public virtual BaseAddonDeed Deed => null;

        public List<AddonComponent> Components { get; private set; }

        public virtual bool ShareHue => true;

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                if (base.Hue != value)
                {
                    base.Hue = value;

                    if (!Deleted && ShareHue && Components != null)
                        foreach (AddonComponent c in Components)
                            c.Hue = value;
                }
            }
        }

        Item IAddon.Deed => Deed;

        public bool CouldFit(IPoint3D p, Map map)
        {
            BaseHouse h = null;
            return CouldFit(p, map, null, ref h) == AddonFitResult.Valid;
        }

        public virtual void OnChop(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house?.IsOwner(from) == true && house.Addons.Contains(this))
            {
                Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.

                int hue = 0;

                if (RetainDeedHue)
                    for (int i = 0; hue == 0 && i < Components.Count; ++i)
                    {
                        AddonComponent c = Components[i];

                        if (c.Hue != 0)
                            hue = c.Hue;
                    }

                Delete();

                house.Addons.Remove(this);

                BaseAddonDeed deed = Deed;

                if (deed != null)
                {
                    if (RetainDeedHue)
                        deed.Hue = hue;

                    from.AddToBackpack(deed);
                }
            }
        }

        public void AddComponent(AddonComponent c, int x, int y, int z)
        {
            if (Deleted)
                return;

            Components.Add(c);

            c.Addon = this;
            c.Offset = new Point3D(x, y, z);
            c.MoveToWorld(new Point3D(X + x, Y + y, Z + z), Map);
        }

        public virtual AddonFitResult CouldFit(IPoint3D p, Map map, Mobile from, ref BaseHouse house)
        {
            if (Deleted)
                return AddonFitResult.Blocked;

            foreach (AddonComponent c in Components)
            {
                Point3D p3D = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);

                if (!map.CanFit(p3D.X, p3D.Y, p3D.Z, c.ItemData.Height, false, true, c.Z == 0))
                    return AddonFitResult.Blocked;
                if (!CheckHouse(from, p3D, map, c.ItemData.Height, ref house))
                    return AddonFitResult.NotInHouse;

                if (c.NeedsWall)
                {
                    Point3D wall = c.WallPosition;

                    if (!IsWall(p3D.X + wall.X, p3D.Y + wall.Y, p3D.Z + wall.Z, map))
                        return AddonFitResult.NoWall;
                }
            }

            List<BaseDoor> doors = house.Doors;

            for (int i = 0; i < doors.Count; ++i)
            {
                BaseDoor door = doors[i];

                Point3D doorLoc = door.GetWorldLocation();
                int doorHeight = door.ItemData.CalcHeight;

                foreach (AddonComponent c in Components)
                {
                    Point3D addonLoc = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);
                    int addonHeight = c.ItemData.CalcHeight;

                    if (Utility.InRange(doorLoc, addonLoc, 1) &&
                        (addonLoc.Z == doorLoc.Z ||
                         (addonLoc.Z + addonHeight > doorLoc.Z && doorLoc.Z + doorHeight > addonLoc.Z)))
                        return AddonFitResult.DoorTooClose;
                }
            }

            return AddonFitResult.Valid;
        }

        public static bool CheckHouse(Mobile from, Point3D p, Map map, int height, ref BaseHouse house) =>
            from == null || BaseHouse.FindHouseAt(p, map, height)?.IsOwner(from) == true;

        public static bool IsWall(int x, int y, int z, Map map)
        {
            if (map == null)
                return false;

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile t = tiles[i];
                ItemData id = TileData.ItemTable[t.ID & TileData.MaxItemValue];

                if ((id.Flags & TileFlag.Wall) != 0 && z + 16 > t.Z && t.Z + t.Height > z)
                    return true;
            }

            return false;
        }

        public virtual void OnComponentLoaded(AddonComponent c)
        {
        }

        public virtual void OnComponentUsed(AddonComponent c, Mobile from)
        {
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (Deleted)
                return;

            foreach (AddonComponent c in Components)
                c.Location = new Point3D(X + c.Offset.X, Y + c.Offset.Y, Z + c.Offset.Z);
        }

        public override void OnMapChange()
        {
            if (Deleted)
                return;

            foreach (AddonComponent c in Components)
                c.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            foreach (AddonComponent c in Components)
                c.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.WriteItemList(Components);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        Components = reader.ReadStrongItemList<AddonComponent>();
                        break;
                    }
            }

            if (version < 1 && Weight == 0)
                Weight = -1;
        }

        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                if (m_Resource != value)
                {
                    m_Resource = value;
                    Hue = CraftResources.GetHue(m_Resource);

                    InvalidateProperties();
                }
            }
        }
    }
}
