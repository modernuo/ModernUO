using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items
{
    [SerializationGenerator(2, false)]
    public abstract partial class BaseAddonContainer : BaseContainer, IChoppable, IAddon
    {
        [SerializableField(0, setter: "private")]
        private List<AddonContainerComponent> _components;

        [SerializableField(1, "private", "private")]
        private CraftResource _rawResource;

        public BaseAddonContainer(int itemID) : base(itemID)
        {
            AddonComponent.ApplyLightTo(this);

            _components = new List<AddonContainerComponent>();
        }

        public override bool DisplayWeight => false;

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

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => _rawResource;
            set
            {
                if (_rawResource != value)
                {
                    RawResource = value;
                    Hue = CraftResources.GetHue(_rawResource);

                    InvalidateProperties();
                    this.MarkDirty();
                }
            }
        }

        public virtual bool RetainDeedHue => false;
        public virtual bool NeedsWall => false;
        public virtual bool ShareHue => true;
        public virtual Point3D WallPosition => Point3D.Zero;
        public virtual BaseAddonContainerDeed Deed => null;

        Item IAddon.Deed => Deed;

        public bool CouldFit(IPoint3D p, Map map)
        {
            BaseHouse house = null;

            return CouldFit(p, map, null, ref house) == AddonFitResult.Valid;
        }

        public virtual void OnChop(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house?.IsOwner(from) == true)
            {
                if (!IsSecure)
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

                    DropItemsToGround();

                    Delete();

                    house.Addons.Remove(this);

                    var deed = Deed;

                    if (deed != null)
                    {
                        deed.Resource = Resource;

                        if (RetainDeedHue)
                        {
                            deed.Hue = hue;
                        }

                        from.AddToBackpack(deed);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1074870); // This item must be unlocked/unsecured before re-deeding it.
                }
            }
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            base.OnLocationChange(oldLoc);

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
            base.OnMapChange();

            if (Deleted)
            {
                return;
            }

            foreach (var c in Components)
            {
                c.Map = Map;
            }
        }

        public override void OnDelete()
        {
            var house = BaseHouse.FindHouseAt(this);

            house?.Addons.Remove(this);

            base.OnDelete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (!CraftResources.IsStandard(_rawResource))
            {
                list.Add(CraftResources.GetLocalizationNumber(_rawResource));
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

        public override void InvalidateProperties()
        {
            base.InvalidateProperties();

            if (_components != null)
            {
                foreach (var component in _components)
                {
                    component.InvalidateProperties();
                }
            }
        }

        // Handles v0 with old Enum -> Int casting
        private void Deserialize(IGenericReader reader, int version)
        {
            _components = reader.ReadEntityList<AddonContainerComponent>();
            _rawResource = version == 1 ? reader.ReadEnum<CraftResource>() : (CraftResource)reader.ReadInt();
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            AddonComponent.ApplyLightTo(this);
        }

        public virtual void DropItemsToGround()
        {
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                Items[i].MoveToWorld(Location);
            }
        }

        public void AddComponent(AddonContainerComponent c, int x, int y, int z)
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

        public AddonFitResult CouldFit(IPoint3D p, Map map, Mobile from, ref BaseHouse house)
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

                if (!BaseAddon.CheckHouse(from, p3D, map, c.ItemData.Height, out house))
                {
                    return AddonFitResult.NotInHouse;
                }

                if (c.NeedsWall)
                {
                    var wall = c.WallPosition;

                    if (!BaseAddon.IsWall(p3D.X + wall.X, p3D.Y + wall.Y, p3D.Z + wall.Z, map))
                    {
                        return AddonFitResult.NoWall;
                    }
                }
            }

            var p3 = new Point3D(p.X, p.Y, p.Z);

            if (!map.CanFit(p3.X, p3.Y, p3.Z, ItemData.Height, false, true, Z == 0))
            {
                return AddonFitResult.Blocked;
            }

            if (!BaseAddon.CheckHouse(from, p3, map, ItemData.Height, out house))
            {
                return AddonFitResult.NotInHouse;
            }

            if (NeedsWall)
            {
                var wall = WallPosition;

                if (!BaseAddon.IsWall(p3.X + wall.X, p3.Y + wall.Y, p3.Z + wall.Z, map))
                {
                    return AddonFitResult.NoWall;
                }
            }

            if (house != null)
            {
                var doors = house.Doors;

                for (var i = 0; i < doors.Count; ++i)
                {
                    var door = doors[i];

                    if (door == null)
                    {
                        continue;
                    }

                    if (door.Open)
                    {
                        return AddonFitResult.DoorsNotClosed;
                    }

                    var doorLoc = door.GetWorldLocation();
                    var doorHeight = door.ItemData.CalcHeight;
                    int addonHeight;

                    foreach (var c in Components)
                    {
                        var addonLoc = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);
                        addonHeight = c.ItemData.CalcHeight;

                        if (Utility.InRange(doorLoc, addonLoc, 1) &&
                            (addonLoc.Z == doorLoc.Z ||
                             addonLoc.Z + addonHeight > doorLoc.Z && doorLoc.Z + doorHeight > addonLoc.Z))
                        {
                            return AddonFitResult.DoorTooClose;
                        }
                    }

                    var addonLo = new Point3D(p.X, p.Y, p.Z);
                    addonHeight = ItemData.CalcHeight;

                    if (Utility.InRange(doorLoc, addonLo, 1) &&
                        (addonLo.Z == doorLoc.Z ||
                         addonLo.Z + addonHeight > doorLoc.Z && doorLoc.Z + doorHeight > addonLo.Z))
                    {
                        return AddonFitResult.DoorTooClose;
                    }
                }
            }

            return AddonFitResult.Valid;
        }

        public virtual void OnComponentLoaded(AddonContainerComponent c)
        {
        }

        public virtual void OnComponentUsed(AddonContainerComponent c, Mobile from)
        {
            if (!Deleted)
            {
                OnDoubleClick(from);
            }
        }
    }
}
