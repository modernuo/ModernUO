using System.Collections.Generic;
using ModernUO.Serialization;
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

    [SerializationGenerator(3, false)]
    public abstract partial class BaseAddon : Item, IChoppable, IAddon
    {
        public BaseAddon() : base(1)
        {
            Movable = false;
            Visible = false;

            Components = new List<AddonComponent>();
        }

        public virtual bool RetainDeedHue => false;

        public virtual BaseAddonDeed Deed => null;

        [SerializableField(0, setter: "private")]
        private List<AddonComponent> _components;

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
                    {
                        foreach (var c in Components)
                        {
                            c.Hue = value;
                        }
                    }
                }
            }
        }

        [SerializableProperty(1)]
        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => _resource;
            set
            {
                if (_resource != value)
                {
                    _resource = value;
                    Hue = CraftResources.GetHue(_resource);

                    InvalidateProperties();
                    this.MarkDirty();
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
            var house = BaseHouse.FindHouseAt(this);

            if (house?.IsOwner(from) == true && house.Addons.Contains(this))
            {
                Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.

                var hue = 0;

                if (RetainDeedHue)
                {
                    for (var i = 0; hue == 0 && i < Components.Count; ++i)
                    {
                        var c = Components[i];

                        if (c.Hue != 0)
                        {
                            hue = c.Hue;
                        }
                    }
                }

                Delete();

                house.Addons.Remove(this);

                var deed = Deed;

                if (deed != null)
                {
                    if (RetainDeedHue)
                    {
                        deed.Hue = hue;
                    }

                    from.AddToBackpack(deed);
                }
            }
        }

        public void AddComponent(AddonComponent c, int x, int y, int z)
        {
            if (Deleted)
            {
                return;
            }

            this.Add(Components, c);

            c.Addon = this;
            c.Offset = new Point3D(x, y, z);
            c.MoveToWorld(new Point3D(X + x, Y + y, Z + z), Map);
        }

        public virtual AddonFitResult CouldFit(IPoint3D p, Map map, Mobile from, ref BaseHouse house)
        {
            if (Deleted)
            {
                return AddonFitResult.Blocked;
            }

            foreach (var c in Components)
            {
                var p3D = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);

                if (!map.CanFit(p3D.X, p3D.Y, p3D.Z, c.ItemData.Height, false, true, c.Z == 0))
                {
                    return AddonFitResult.Blocked;
                }

                if (!CheckHouse(from, p3D, map, c.ItemData.Height, out house))
                {
                    return AddonFitResult.NotInHouse;
                }

                if (c.NeedsWall)
                {
                    var wall = c.WallPosition;

                    if (!IsWall(p3D.X + wall.X, p3D.Y + wall.Y, p3D.Z + wall.Z, map))
                    {
                        return AddonFitResult.NoWall;
                    }
                }
            }

            var doors = house.Doors;

            for (var i = 0; i < doors.Count; ++i)
            {
                var door = doors[i];

                var doorLoc = door.GetWorldLocation();
                var doorHeight = door.ItemData.CalcHeight;

                foreach (var c in Components)
                {
                    var addonLoc = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);
                    var addonHeight = c.ItemData.CalcHeight;

                    if (Utility.InRange(doorLoc, addonLoc, 1) &&
                        (addonLoc.Z == doorLoc.Z ||
                         addonLoc.Z + addonHeight > doorLoc.Z && doorLoc.Z + doorHeight > addonLoc.Z))
                    {
                        return AddonFitResult.DoorTooClose;
                    }
                }
            }

            return AddonFitResult.Valid;
        }

        public static bool CheckHouse(Mobile from, Point3D p, Map map, int height, out BaseHouse house)
        {
            house = BaseHouse.FindHouseAt(p, map, height);

            return house != null && (from == null || house.IsOwner(from));
        }

        public static bool IsWall(int x, int y, int z, Map map)
        {
            if (map == null)
            {
                return false;
            }

            var tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (var i = 0; i < tiles.Length; ++i)
            {
                var t = tiles[i];
                var id = TileData.ItemTable[t.ID & TileData.MaxItemValue];

                if ((id.Flags & TileFlag.Wall) != 0 && z + 16 > t.Z && t.Z + t.Height > z)
                {
                    return true;
                }
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
            {
                return;
            }

            foreach (var c in Components)
            {
                c.Location = new Point3D(X + c.Offset.X, Y + c.Offset.Y, Z + c.Offset.Z);
            }
        }

        public override void OnMapChange()
        {
            if (Deleted)
            {
                return;
            }

            foreach (var c in Components)
            {
                c.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            foreach (var c in Components)
            {
                c.Delete();
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _components = reader.ReadEntityList<AddonComponent>();

            if (version == 2)
            {
                _resource = (CraftResource)reader.ReadEncodedInt();
            }
        }
    }
}
