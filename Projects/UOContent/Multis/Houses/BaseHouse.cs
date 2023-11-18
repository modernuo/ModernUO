using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Collections;
using Server.ContextMenus;
using Server.Ethics;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis.Deeds;
using Server.Regions;
using Server.Targeting;

namespace Server.Multis
{
    public abstract class BaseHouse : BaseMulti
    {
        public const int MaxCoOwners = 15;

        public const bool DecayEnabled = true;

        public const int MaximumBarkeepCount = 2;

        private static readonly Dictionary<Mobile, List<BaseHouse>> m_Table = new();

        private DecayLevel m_CurrentStage;

        private DecayLevel m_LastDecayLevel;

        private Mobile m_Owner;

        private bool m_Public;

        private HouseRegion m_Region;

        private Point3D m_RelativeBanLocation;
        private TrashBarrel m_Trash;

        public BaseHouse(int multiID, Mobile owner, int maxLockDown, int maxSecure) : base(multiID)
        {
            AllHouses.Add(this);

            LastRefreshed = Core.Now;

            BuiltOn = Core.Now;
            LastTraded = DateTime.MinValue;

            Doors = new List<BaseDoor>();
            LockDowns = new List<Item>();
            Secures = new List<SecureInfo>();
            Addons = new List<Item>();

            CoOwners = new List<Mobile>();
            Friends = new List<Mobile>();
            Bans = new List<Mobile>();
            Access = new List<Mobile>();

            VendorRentalContracts = new List<VendorRentalContract>();
            InternalizedVendors = new List<Mobile>();

            m_Owner = owner;

            MaxLockDowns = maxLockDown;
            MaxSecures = maxSecure;

            m_RelativeBanLocation = BaseBanLocation;

            UpdateRegion();

            if (owner != null)
            {
                if (!m_Table.TryGetValue(owner, out var list))
                {
                    m_Table[owner] = list = new List<BaseHouse>();
                }

                list.Add(this);
            }

            Movable = false;
        }

        public BaseHouse(Serial serial) : base(serial)
        {
            AllHouses.Add(this);
        }

        // Is new player vendor system enabled?
        public static bool NewVendorSystem => Core.AOS;

        public static int MaxFriends => !Core.AOS ? 50 : 140;
        public static int MaxBans => !Core.AOS ? 50 : 140;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastRefreshed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestrictDecay { get; set; }

        public virtual TimeSpan DecayPeriod => TimeSpan.FromDays(5.0);

        public virtual DecayType DecayType
        {
            get
            {
                if (RestrictDecay || !DecayEnabled || DecayPeriod == TimeSpan.Zero)
                {
                    return DecayType.Ageless;
                }

                if (m_Owner == null)
                {
                    return Core.AOS ? DecayType.Condemned : DecayType.ManualRefresh;
                }

                if (m_Owner.Account is not Account acct)
                {
                    return Core.AOS ? DecayType.Condemned : DecayType.ManualRefresh;
                }

                if (acct.AccessLevel >= AccessLevel.GameMaster)
                {
                    return DecayType.Ageless;
                }

                for (var i = 0; i < acct.Length; ++i)
                {
                    var mob = acct[i];

                    if (mob?.AccessLevel >= AccessLevel.GameMaster)
                    {
                        return DecayType.Ageless;
                    }
                }

                if (!Core.AOS)
                {
                    return DecayType.ManualRefresh;
                }

                if (acct.Inactive)
                {
                    return DecayType.Condemned;
                }

                var allHouses = new List<BaseHouse>();

                for (var i = 0; i < acct.Length; ++i)
                {
                    var mob = acct[i];

                    if (mob != null)
                    {
                        allHouses.AddRange(GetHouses(mob));
                    }
                }

                BaseHouse newest = null;

                for (var i = 0; i < allHouses.Count; ++i)
                {
                    var check = allHouses[i];

                    if (newest == null || IsNewer(check, newest))
                    {
                        newest = check;
                    }
                }

                if (this == newest)
                {
                    return DecayType.AutoRefresh;
                }

                return DecayType.ManualRefresh;
            }
        }

        public virtual bool CanDecay
        {
            get
            {
                var type = DecayType;

                return type is DecayType.Condemned or DecayType.ManualRefresh;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual DecayLevel DecayLevel
        {
            get
            {
                DecayLevel result;

                if (!CanDecay)
                {
                    if (DynamicDecay.Enabled)
                    {
                        ResetDynamicDecay();
                    }

                    LastRefreshed = Core.Now;
                    result = DecayLevel.Ageless;
                }
                else if (DynamicDecay.Enabled)
                {
                    var stage = m_CurrentStage;

                    if (stage == DecayLevel.Ageless || DynamicDecay.Decays(stage) && NextDecayStage <= Core.Now)
                    {
                        SetDynamicDecay(++stage);
                    }

                    if (stage == DecayLevel.Collapsed && (HasRentedVendors || VendorInventories.Count > 0))
                    {
                        result = DecayLevel.DemolitionPending;
                    }
                    else
                    {
                        result = stage;
                    }
                }
                else
                {
                    result = GetOldDecayLevel();
                }

                if (result != m_LastDecayLevel)
                {
                    m_LastDecayLevel = result;

                    if (Sign?.GettingProperties == false)
                    {
                        Sign.InvalidateProperties();
                    }
                }

                return result;
            }
        }

        public virtual TimeSpan RestrictedPlacingTime => TimeSpan.FromHours(1.0);

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double BonusStorageScalar => Core.ML ? 1.2 : 1.0;

        public virtual bool IsAosRules => Core.AOS;

        public virtual bool IsActive => true;

        public bool HasPersonalVendors
        {
            get
            {
                foreach (var vendor in PlayerVendors)
                {
                    if (vendor is not RentedVendor)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasRentedVendors
        {
            get
            {
                foreach (var vendor in PlayerVendors)
                {
                    if (vendor is RentedVendor)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasAddonContainers
        {
            get
            {
                foreach (var item in Addons)
                {
                    if (item is BaseAddonContainer)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static List<BaseHouse> AllHouses { get; } = new();

        public abstract Rectangle2D[] Area { get; }
        public abstract Point3D BaseBanLocation { get; }

        public override bool Decays => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get => m_Owner;
            set
            {
                if (m_Owner == value)
                {
                    return;
                }

                if (m_Owner != null)
                {
                    if (m_Table.TryGetValue(m_Owner, out var list) && list.Remove(this) && list.Count == 0)
                    {
                        m_Table.Remove(m_Owner);
                    }

                    m_Owner.Delta(MobileDelta.Noto);
                }

                m_Owner = value;

                if (m_Owner != null)
                {
                    if (!m_Table.TryGetValue(m_Owner, out var list))
                    {
                        m_Table[m_Owner] = list = new List<BaseHouse>();
                    }

                    list.Add(this);
                    m_Owner.Delta(MobileDelta.Noto);
                }

                Sign?.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Visits { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Public
        {
            get => m_Public;
            set
            {
                if (m_Public != value)
                {
                    m_Public = value;

                    if (!m_Public) // Privatizing the house, change to brass sign
                    {
                        ChangeSignType(0xBD2);
                    }

                    Sign?.InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSecures { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D BanLocation
        {
            get
            {
                if (m_Region != null)
                {
                    return m_Region.GoLocation;
                }

                var rel = m_RelativeBanLocation;
                return new Point3D(X + rel.X, Y + rel.Y, Z + rel.Z);
            }
            set => RelativeBanLocation = new Point3D(value.X - X, value.Y - Y, value.Z - Z);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D RelativeBanLocation
        {
            get => m_RelativeBanLocation;
            set
            {
                m_RelativeBanLocation = value;

                if (m_Region != null)
                {
                    m_Region.GoLocation = new Point3D(X + value.X, Y + value.Y, Z + value.Z);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockDowns { get; set; }

        public Region Region => m_Region;
        public List<Mobile> CoOwners { get; set; }

        public List<Mobile> Friends { get; set; }

        public List<Mobile> Access { get; set; }

        public List<Mobile> Bans { get; set; }

        public List<BaseDoor> Doors { get; set; }

        public int LockDownCount
        {
            get
            {
                var count = 0;

                count += GetLockdowns();

                if (Secures != null)
                {
                    for (var i = 0; i < Secures.Count; ++i)
                    {
                        var info = Secures[i];

                        if (info.Item.Deleted)
                        {
                            continue;
                        }

                        if (info.Item is StrongBox)
                        {
                            count += 1;
                        }
                        else
                        {
                            count += 125;
                        }
                    }
                }

                return count;
            }
        }

        public int SecureCount
        {
            get
            {
                var count = 0;

                if (Secures != null)
                {
                    for (var i = 0; i < Secures.Count; i++)
                    {
                        var info = Secures[i];

                        if (info.Item.Deleted)
                        {
                            continue;
                        }

                        if (info.Item is not StrongBox)
                        {
                            count += 1;
                        }
                    }
                }

                return count;
            }
        }

        public List<Item> Addons { get; set; }

        public List<Item> LockDowns { get; private set; }

        public List<SecureInfo> Secures { get; private set; }

        public HouseSign Sign { get; set; }

        public List<PlayerVendor> PlayerVendors { get; } = new();

        public List<PlayerBarkeeper> PlayerBarkeepers { get; } = new();

        public List<VendorRentalContract> VendorRentalContracts { get; private set; }

        public List<VendorInventory> VendorInventories { get; } = new();

        public List<RelocatedEntity> RelocatedEntities { get; } = new();

        public MovingCrate MovingCrate { get; set; }

        public List<Mobile> InternalizedVendors { get; private set; }

        public DateTime BuiltOn { get; set; }

        public DateTime LastTraded { get; set; }

        public virtual HousePlacementEntry ConvertEntry => null;
        public virtual int ConvertOffsetX => 0;
        public virtual int ConvertOffsetY => 0;
        public virtual int ConvertOffsetZ => 0;

        public virtual int DefaultPrice => 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Price { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextDecayStage { get; set; }

        public static void Decay_OnTick()
        {
            for (var i = 0; i < AllHouses.Count; ++i)
            {
                AllHouses[i].CheckDecay();
            }
        }

        public bool IsNewer(BaseHouse check, BaseHouse house)
        {
            var checkTime = check.LastTraded > check.BuiltOn ? check.LastTraded : check.BuiltOn;
            var houseTime = house.LastTraded > house.BuiltOn ? house.LastTraded : house.BuiltOn;

            return checkTime > houseTime;
        }

        public DecayLevel GetOldDecayLevel()
        {
            var timeAfterRefresh = Core.Now - LastRefreshed;
            var percent = (int)(timeAfterRefresh.Ticks * 1000 / DecayPeriod.Ticks);

            if (percent >= 1000) // 100.0%
            {
                return HasRentedVendors || VendorInventories.Count > 0 ? DecayLevel.DemolitionPending : DecayLevel.Collapsed;
            }

            if (percent >= 950) // 95.0% - 99.9%
            {
                return DecayLevel.IDOC;
            }

            if (percent >= 750) // 75.0% - 94.9%
            {
                return DecayLevel.Greatly;
            }

            if (percent >= 500) // 50.0% - 74.9%
            {
                return DecayLevel.Fairly;
            }

            if (percent >= 250) // 25.0% - 49.9%
            {
                return DecayLevel.Somewhat;
            }

            if (percent >= 005) // 00.5% - 24.9%
            {
                return DecayLevel.Slightly;
            }

            return DecayLevel.LikeNew;
        }

        public virtual bool RefreshDecay()
        {
            if (DecayType == DecayType.Condemned)
            {
                return false;
            }

            var oldLevel = DecayLevel;

            LastRefreshed = Core.Now;

            if (DynamicDecay.Enabled)
            {
                ResetDynamicDecay();
            }

            Sign?.InvalidateProperties();

            return oldLevel > DecayLevel.LikeNew;
        }

        public virtual bool CheckDecay()
        {
            if (!Deleted && DecayLevel == DecayLevel.Collapsed)
            {
                Timer.StartTimer(Decay_Sandbox);
                return true;
            }

            return false;
        }

        public virtual void KillVendors()
        {
            using var list = PooledRefList<Mobile>.Create();
            list.AddRange(PlayerVendors);

            foreach (var vendor in list)
            {
                ((PlayerVendor)vendor).Destroy(true);
            }

            list.Clear();
            list.AddRange(PlayerBarkeepers);

            foreach (var barkeeper in list)
            {
                barkeeper.Delete();
            }
        }

        public virtual void Decay_Sandbox()
        {
            if (Deleted)
            {
                return;
            }

            if (Core.ML)
            {
                new TempNoHousingRegion(this, null);
            }

            KillVendors();
            Delete();
        }

        public virtual HousePlacementEntry GetAosEntry() => HousePlacementEntry.Find(this);

        public virtual int GetAosMaxSecures()
        {
            var hpe = GetAosEntry();

            if (hpe == null)
            {
                return 0;
            }

            return (int)(hpe.Storage * BonusStorageScalar);
        }

        public virtual int GetAosMaxLockdowns()
        {
            var hpe = GetAosEntry();

            if (hpe == null)
            {
                return 0;
            }

            return (int)(hpe.Lockdowns * BonusStorageScalar);
        }

        public virtual int GetAosCurSecures(
            out int fromSecures, out int fromVendors, out int fromLockdowns,
            out int fromMovingCrate
        )
        {
            fromSecures = 0;
            fromVendors = 0;
            fromLockdowns = 0;
            fromMovingCrate = 0;

            var list = Secures;

            if (list != null)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var si = list[i];

                    fromSecures += si.Item.TotalItems;
                }

                fromLockdowns += list.Count;
            }

            fromLockdowns += GetLockdowns();

            if (!NewVendorSystem)
            {
                foreach (var vendor in PlayerVendors)
                {
                    if (vendor.Backpack != null)
                    {
                        fromVendors += vendor.Backpack.TotalItems;
                    }
                }
            }

            if (MovingCrate != null)
            {
                fromMovingCrate += MovingCrate.TotalItems;

                foreach (var item in MovingCrate.Items)
                {
                    if (item is PackingBox)
                    {
                        fromMovingCrate--;
                    }
                }
            }

            return fromSecures + fromVendors + fromLockdowns + fromMovingCrate;
        }

        public bool InRange(Point2D from, int range)
        {
            if (Region == null)
            {
                return false;
            }

            foreach (var rect in Region.Area)
            {
                // TODO: Convert this to 3D - https://github.com/modernuo/ModernUO/issues/29
                if (from.X >= rect.Start.X - range && from.Y >= rect.Start.Y - range && from.X < rect.End.X + range && from.Y < rect.End.Y + range)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual int GetNewVendorSystemMaxVendors()
        {
            var hpe = GetAosEntry();

            if (hpe == null)
            {
                return 0;
            }

            return (int)(hpe.Vendors * BonusStorageScalar);
        }

        public virtual bool CanPlaceNewVendor() =>
            !IsAosRules || (!NewVendorSystem
                ? CheckAosLockdowns(10)
                : PlayerVendors.Count + VendorRentalContracts.Count < GetNewVendorSystemMaxVendors());

        public virtual bool CanPlaceNewBarkeep() => PlayerBarkeepers.Count < MaximumBarkeepCount;

        public static void IsThereVendor(Point3D location, Map map, out bool vendor, out bool rentalContract)
        {
            vendor = false;
            rentalContract = false;

            foreach (var m in map.GetMobilesAt(location))
            {
                if ((location.Z - m.Z).Abs() <= 16 && m is PlayerVendor or PlayerBarkeeper)
                {
                    vendor = true;
                    return;
                }
            }

            foreach (var item in map.GetItemsAt(location))
            {
                if ((location.Z - item.Z).Abs() <= 16)
                {
                    if (item is PlayerVendorPlaceholder)
                    {
                        vendor = true;
                        return;
                    }

                    if (item is VendorRentalContract)
                    {
                        rentalContract = true;
                        return;
                    }
                }
            }
        }

        public List<Mobile> AvailableVendorsFor(Mobile m)
        {
            List<Mobile> list = new List<Mobile>();
            foreach (PlayerVendor vendor in PlayerVendors)
            {
                if (vendor.CanInteractWith(m, false))
                {
                    list.Add(vendor);
                }
            }

            return list;
        }

        public bool AreThereAvailableVendorsFor(Mobile m)
        {
            foreach (var vendor in PlayerVendors)
            {
                if (vendor.CanInteractWith(m, false))
                {
                    return true;
                }
            }

            return false;
        }

        public void MoveAllToCrate()
        {
            RelocatedEntities.Clear();

            MovingCrate?.Hide();

            if (m_Trash != null)
            {
                m_Trash.Delete();
                m_Trash = null;
            }

            foreach (var item in LockDowns)
            {
                if (!item.Deleted)
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;

                    if (item.Parent == null)
                    {
                        DropToMovingCrate(item);
                    }
                }
            }

            LockDowns.Clear();

            foreach (Item item in VendorRentalContracts)
            {
                if (!item.Deleted)
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;

                    if (item.Parent == null)
                    {
                        DropToMovingCrate(item);
                    }
                }
            }

            VendorRentalContracts.Clear();

            foreach (var info in Secures)
            {
                Item item = info.Item;

                if (!item.Deleted)
                {
                    if (item is StrongBox box)
                    {
                        item = box.ConvertToStandardContainer();
                    }

                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;

                    if (item.Parent == null)
                    {
                        DropToMovingCrate(item);
                    }
                }
            }

            Secures.Clear();

            foreach (var addon in Addons)
            {
                if (!addon.Deleted)
                {
                    Item deed = null;
                    var retainDeedHue = false; // if the items aren't hued but the deed itself is
                    var hue = 0;

                    var ba = addon as BaseAddon;

                    if (addon is IAddon baseAddon)
                    {
                        deed = baseAddon.Deed;

                        // There are things that are IAddon which aren't BaseAddon
                        if (ba?.RetainDeedHue == true)
                        {
                            retainDeedHue = true;

                            for (var i = 0; hue == 0 && i < ba.Components.Count; ++i)
                            {
                                var c = ba.Components[i];

                                if (c.Hue != 0)
                                {
                                    hue = c.Hue;
                                }
                            }
                        }
                    }

                    if (deed != null)
                    {
                        if (deed is BaseAddonContainerDeed containerDeed && addon is BaseAddonContainer c)
                        {
                            c.DropItemsToGround();
                            containerDeed.Resource = c.Resource;
                        }
                        else if (deed is BaseAddonDeed addonDeed && ba != null)
                        {
                            addonDeed.Resource = ba.Resource;
                        }

                        addon.Delete();

                        if (retainDeedHue)
                        {
                            deed.Hue = hue;
                        }

                        DropToMovingCrate(deed);
                    }
                    else
                    {
                        DropToMovingCrate(addon);
                    }
                }
            }

            Addons.Clear();

            foreach (var mobile in PlayerVendors)
            {
                mobile.Return();
                mobile.Internalize();
                InternalizedVendors.Add(mobile);
            }

            foreach (var mobile in PlayerBarkeepers)
            {
                mobile.Internalize();
                InternalizedVendors.Add(mobile);
            }
        }

        public PooledRefList<IEntity> GetHouseEntities()
        {
            var list = PooledRefList<IEntity>.Create(256);

            MovingCrate?.Hide();

            if (m_Trash != null && m_Trash.Map != Map.Internal)
            {
                list.Add(m_Trash);
            }

            for (var i = 0; i < LockDowns.Count; i++)
            {
                var item = LockDowns[i];
                if (item.Parent == null && item.Map != Map.Internal)
                {
                    list.Add(item);
                }
            }

            for (var i = 0; i < VendorRentalContracts.Count; i++)
            {
                var item = VendorRentalContracts[i];
                if (item.Parent == null && item.Map != Map.Internal)
                {
                    list.Add(item);
                }
            }

            for (var i = 0; i < Secures.Count; i++)
            {
                var item = Secures[i].Item;
                if (item.Parent == null && item.Map != Map.Internal)
                {
                    list.Add(item);
                }
            }

            for (var i = 0; i < Addons.Count; i++)
            {
                var item = Addons[i];
                if (item.Parent == null && item.Map != Map.Internal)
                {
                    list.Add(item);
                }
            }

            for (var i = 0; i < PlayerVendors.Count; i++)
            {
                var mobile = PlayerVendors[i];
                mobile.Return();

                if (mobile.Map != Map.Internal)
                {
                    list.Add(mobile);
                }
            }

            for (var i = 0; i < PlayerBarkeepers.Count; i++)
            {
                var mobile = PlayerBarkeepers[i];

                if (mobile.Map != Map.Internal)
                {
                    list.Add(mobile);
                }
            }

            return list;
        }

        public void RelocateEntities()
        {
            foreach (var entity in GetHouseEntities())
            {
                var relLoc = new Point3D(entity.X - X, entity.Y - Y, entity.Z - Z);
                var relocEntity = new RelocatedEntity(entity, relLoc);

                RelocatedEntities.Add(relocEntity);

                if (entity is Item item)
                {
                    item.Internalize();
                }
                else if (entity is Mobile mobile)
                {
                    mobile.Internalize();
                }
            }
        }

        public void RestoreRelocatedEntities()
        {
            foreach (var relocEntity in RelocatedEntities)
            {
                var relLoc = relocEntity.RelativeLocation;
                var location = new Point3D(relLoc.X + X, relLoc.Y + Y, relLoc.Z + Z);

                var entity = relocEntity.Entity;
                if (entity is Item item)
                {
                    if (!item.Deleted)
                    {
                        var addon = item as IAddon;
                        if (addon != null)
                        {
                            if (addon.CouldFit(location, Map))
                            {
                                item.MoveToWorld(location, Map);
                                continue;
                            }
                        }
                        else
                        {
                            int height;
                            bool requireSurface;
                            if (item is VendorRentalContract)
                            {
                                height = 16;
                                requireSurface = true;
                            }
                            else
                            {
                                height = item.ItemData.Height;
                                requireSurface = false;
                            }

                            if (Map.CanFit(location.X, location.Y, location.Z, height, false, false, requireSurface))
                            {
                                item.MoveToWorld(location, Map);
                                continue;
                            }
                        }

                        // The item can't fit

                        if (item is TrashBarrel)
                        {
                            item.Delete(); // Trash barrels don't go to the moving crate
                        }
                        else
                        {
                            SetLockdown(item, false);
                            item.IsSecure = false;
                            item.Movable = true;

                            var relocateItem = item;

                            if (item is StrongBox box)
                            {
                                relocateItem = box.ConvertToStandardContainer();
                            }

                            if (addon != null)
                            {
                                var deed = addon.Deed;
                                var retainDeedHue = false; // if the items aren't hued but the deed itself is
                                var hue = 0;

                                // There are things that are IAddon which aren't BaseAddon
                                if (item is BaseAddon ba && ba.RetainDeedHue)
                                {
                                    retainDeedHue = true;

                                    for (var i = 0; hue == 0 && i < ba.Components.Count; ++i)
                                    {
                                        var c = ba.Components[i];

                                        if (c.Hue != 0)
                                        {
                                            hue = c.Hue;
                                        }
                                    }
                                }

                                if (deed != null)
                                {
                                    if (deed is BaseAddonContainerDeed containerDeed && item is BaseAddonContainer c)
                                    {
                                        c.DropItemsToGround();

                                        containerDeed.Resource = c.Resource;
                                    }
                                    else if (deed is BaseAddonDeed addonDeed && item is BaseAddon baseAddon)
                                    {
                                        addonDeed.Resource = baseAddon.Resource;
                                    }

                                    if (retainDeedHue)
                                    {
                                        deed.Hue = hue;
                                    }
                                }

                                relocateItem = deed;
                                item.Delete();
                            }

                            if (relocateItem != null)
                            {
                                DropToMovingCrate(relocateItem);
                            }
                        }
                    }

                    if (m_Trash == item)
                    {
                        m_Trash = null;
                    }

                    LockDowns.Remove(item);
                    if (item is VendorRentalContract contract)
                    {
                        VendorRentalContracts.Remove(contract);
                    }

                    Addons.Remove(item);
                    for (var i = Secures.Count - 1; i >= 0; i--)
                    {
                        if (Secures[i].Item == item)
                        {
                            Secures.RemoveAt(i);
                        }
                    }
                }
                else if (entity is Mobile mobile && !mobile.Deleted)
                {
                    if (Map.CanFit(location, 16, false, false))
                    {
                        mobile.MoveToWorld(location, Map);
                    }
                    else
                    {
                        InternalizedVendors.Add(mobile);
                    }
                }
            }

            RelocatedEntities.Clear();
        }

        public void DropToMovingCrate(Item item)
        {
            MovingCrate ??= new MovingCrate(this);

            MovingCrate.DropItem(item);
        }

        // TODO: Convert to a ref struct enumerator
        public List<Item> GetItems()
        {
            if (Map == null || Map == Map.Internal)
            {
                return new List<Item>();
            }

            var start = new Point2D(X + Components.Min.X, Y + Components.Min.Y);
            var end = new Point2D(X + Components.Max.X + 1, Y + Components.Max.Y + 1);
            var rect = new Rectangle2D(start, end);

            var list = new List<Item>();
            foreach (var item in Map.GetItemsInBounds(rect))
            {
                if (item.Movable && IsInside(item))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public List<Mobile> GetMobiles()
        {
            if (Map == null || Map == Map.Internal)
            {
                return new List<Mobile>();
            }

            var list = new List<Mobile>();

            foreach (var mobile in Region.GetMobiles())
            {
                if (IsInside(mobile))
                {
                    list.Add(mobile);
                }
            }

            return list;
        }

        public virtual bool CheckAosLockdowns(int need) => GetAosCurLockdowns() + need <= GetAosMaxLockdowns();

        public virtual bool CheckAosStorage(int need) =>
            GetAosCurSecures(
                out var fromSecures,
                out var fromVendors,
                out var fromLockdowns,
                out var fromMovingCrate
            ) + need <=
            GetAosMaxSecures();

        public static void Configure()
        {
            LockedDownFlag = 1;
            SecureFlag = 2;
        }

        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), Decay_OnTick);
        }

        public virtual int GetAosCurLockdowns()
        {
            var v = 0;

            v += GetLockdowns();

            if (Secures != null)
            {
                v += Secures.Count;
            }

            if (!NewVendorSystem)
            {
                v += PlayerVendors.Count * 10;
            }

            return v;
        }

        public static bool CheckLockedDown(Item item) => FindHouseAt(item)?.HasLockedDownItem(item) == true;

        public static bool CheckSecured(Item item) => FindHouseAt(item)?.HasSecureItem(item) == true;

        public static bool CheckLockedDownOrSecured(Item item)
        {
            var house = FindHouseAt(item);
            return house != null && (house.HasSecureItem(item) || house.HasLockedDownItem(item));
        }

        public static List<BaseHouse> GetHouses(Mobile m)
        {
            var list = new List<BaseHouse>();

            if (m != null)
            {
                if (m_Table.TryGetValue(m, out var exists))
                {
                    for (var i = 0; i < exists.Count; ++i)
                    {
                        var house = exists[i];

                        if (house?.Deleted == false && house.Owner == m)
                        {
                            list.Add(house);
                        }
                    }
                }
            }

            return list;
        }

        public static bool CheckHold(
            Mobile m, Container cont, Item item, bool message, bool checkItems, int plusItems,
            int plusWeight
        )
        {
            var house = FindHouseAt(cont);

            if (house?.IsAosRules != true)
            {
                return true;
            }

            if (house.HasSecureItem(cont) && !house.CheckAosStorage(1 + item.TotalItems + plusItems))
            {
                if (message)
                {
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.
                }

                return false;
            }

            return true;
        }

        public static bool CheckAccessible(Mobile m, Item item)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                return true; // Staff can access anything
            }

            var house = FindHouseAt(item);

            if (house == null)
            {
                return true;
            }

            var res = house.CheckSecureAccess(m, item);

            switch (res)
            {
                case SecureAccessResult.Insecure:     break;
                case SecureAccessResult.Accessible:   return true;
                case SecureAccessResult.Inaccessible: return false;
            }

            if (house.HasLockedDownItem(item))
            {
                return house.IsCoOwner(m) && item is Container;
            }

            return true;
        }

        public static BaseHouse FindHouseAt(Mobile m)
        {
            if (m?.Deleted != false)
            {
                return null;
            }

            return FindHouseAt(m.Location, m.Map, 16);
        }

        public static BaseHouse FindHouseAt(Item item) =>
            item?.Deleted != false ? null : FindHouseAt(item.GetWorldLocation(), item.Map, item.ItemData.Height);

        public static BaseHouse FindHouseAt(Point3D loc, Map map, int height)
        {
            if (map == null || map == Map.Internal)
            {
                return null;
            }

            foreach (var house in map.GetMultisInSector<BaseHouse>(loc))
            {
                if (house.IsInside(loc, height))
                {
                    return house;
                }
            }

            return null;
        }

        public bool IsInside(Mobile m) => m?.Deleted == false && m.Map == Map && IsInside(m.Location, 16);

        public bool IsInside(Item item) =>
            item?.Deleted == false && item.Map == Map && IsInside(item.Location, item.ItemData.Height);

        public bool CheckAccessibility(Item item, Mobile from)
        {
            var res = CheckSecureAccess(from, item);

            switch (res)
            {
                case SecureAccessResult.Insecure:     break;
                case SecureAccessResult.Accessible:   return true;
                case SecureAccessResult.Inaccessible: return false;
            }

            if (!HasLockedDownItem(item))
            {
                return true;
            }

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (item is Runebook)
            {
                return true;
            }

            if (item is ISecurable securable)
            {
                return HasSecureAccess(from, securable.Level);
            }

            if (item is Container)
            {
                return IsCoOwner(from);
            }

            if (item.Stackable)
            {
                return true;
            }

            if (item is BaseLight)
            {
                return IsFriend(from);
            }

            if (item is PotionKeg)
            {
                return IsFriend(from);
            }

            return item is
                Dices or RecallRune or TreasureMap or Clock or
                BaseInstrument or Dyes or VendorRentalContract or RewardBrazier;
        }

        public virtual bool IsInside(Point3D p, int height)
        {
            if (Deleted)
            {
                return false;
            }

            var mcl = Components;

            var x = p.X - X - mcl.Min.X;
            var y = p.Y - Y - mcl.Min.Y;

            if (x < 0 || x >= mcl.Width || y < 0 || y >= mcl.Height)
            {
                return false;
            }

            if (this is HouseFoundation && y < mcl.Height - 1 && p.Z >= Z)
            {
                return true;
            }

            // TODO: Use ref struct
            var tiles = mcl.Tiles[x][y];

            for (var j = 0; j < tiles.Length; ++j)
            {
                var tile = tiles[j];
                var id = tile.ID & TileData.MaxItemValue;
                var data = TileData.ItemTable[id];

                // Slanted roofs do not count; they overhang blocking south and east sides of the multi
                if (data.Roof)
                {
                    continue;
                }

                // Signs and signposts are not considered part of the multi
                if (id >= 0xB95 && id <= 0xC0E || id >= 0xC43 && id <= 0xC44)
                {
                    continue;
                }

                var tileZ = tile.Z + Z;

                if (p.Z == tileZ || p.Z + height > tileZ)
                {
                    return true;
                }
            }

            return false;
        }

        public SecureAccessResult CheckSecureAccess(Mobile m, Item item)
        {
            if (Secures == null || item is not Container)
            {
                return SecureAccessResult.Insecure;
            }

            for (var i = 0; i < Secures.Count; ++i)
            {
                var info = Secures[i];

                if (info.Item == item)
                {
                    return HasSecureAccess(m, info.Level) ? SecureAccessResult.Accessible : SecureAccessResult.Inaccessible;
                }
            }

            return SecureAccessResult.Insecure;
        }

        public override void OnMapChange()
        {
            if (LockDowns == null)
            {
                return;
            }

            UpdateRegion();

            if (Sign?.Deleted == false)
            {
                Sign.Map = Map;
            }

            if (Doors != null)
            {
                foreach (var item in Doors)
                {
                    item.Map = Map;
                }
            }

            foreach (var entity in GetHouseEntities())
            {
                if (entity is Item item)
                {
                    item.Map = Map;
                }
                else if (entity is Mobile mobile)
                {
                    mobile.Map = Map;
                }
            }
        }

        public virtual void ChangeSignType(int itemID)
        {
            if (Sign != null)
            {
                Sign.ItemID = itemID;
            }
        }

        public virtual void UpdateRegion()
        {
            m_Region?.Unregister();

            if (Map != null)
            {
                m_Region = new HouseRegion(this);
                m_Region.Register();
            }
            else
            {
                m_Region = null;
            }
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (LockDowns == null)
            {
                return;
            }

            var x = Location.X - oldLocation.X;
            var y = Location.Y - oldLocation.Y;
            var z = Location.Z - oldLocation.Z;

            if (Sign?.Deleted == false)
            {
                Sign.Location = new Point3D(Sign.X + x, Sign.Y + y, Sign.Z + z);
            }

            UpdateRegion();

            if (Doors != null)
            {
                foreach (var item in Doors)
                {
                    if (!item.Deleted)
                    {
                        item.Location = new Point3D(item.X + x, item.Y + y, item.Z + z);
                    }
                }
            }

            foreach (var entity in GetHouseEntities())
            {
                var newLocation = new Point3D(entity.X + x, entity.Y + y, entity.Z + z);

                if (entity is Item item)
                {
                    item.Location = newLocation;
                }
                else if (entity is Mobile mobile)
                {
                    mobile.Location = newLocation;
                }
            }
        }

        public BaseDoor AddEastDoor(int x, int y, int z) => AddEastDoor(true, x, y, z);

        public BaseDoor AddEastDoor(bool wood, int x, int y, int z)
        {
            var door = MakeDoor(wood, DoorFacing.SouthCW);

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddSouthDoor(int x, int y, int z) => AddSouthDoor(true, x, y, z);

        public BaseDoor AddSouthDoor(bool wood, int x, int y, int z)
        {
            var door = MakeDoor(wood, DoorFacing.WestCW);

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddEastDoor(int x, int y, int z, uint k) => AddEastDoor(true, x, y, z, k);

        public BaseDoor AddEastDoor(bool wood, int x, int y, int z, uint k)
        {
            var door = MakeDoor(wood, DoorFacing.SouthCW);

            door.Locked = true;
            door.KeyValue = k;

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddSouthDoor(int x, int y, int z, uint k) => AddSouthDoor(true, x, y, z, k);

        public BaseDoor AddSouthDoor(bool wood, int x, int y, int z, uint k)
        {
            var door = MakeDoor(wood, DoorFacing.WestCW);

            door.Locked = true;
            door.KeyValue = k;

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor[] AddSouthDoors(int x, int y, int z, uint k) => AddSouthDoors(true, x, y, z, k);

        public BaseDoor[] AddSouthDoors(bool wood, int x, int y, int z, uint k)
        {
            var westDoor = MakeDoor(wood, DoorFacing.WestCW);
            var eastDoor = MakeDoor(wood, DoorFacing.EastCCW);

            westDoor.Locked = true;
            eastDoor.Locked = true;

            westDoor.KeyValue = k;
            eastDoor.KeyValue = k;

            westDoor.Link = eastDoor;
            eastDoor.Link = westDoor;

            AddDoor(westDoor, x, y, z);
            AddDoor(eastDoor, x + 1, y, z);

            return new[] { westDoor, eastDoor };
        }

        protected BaseDoor AddDoor(int itemID, int xOffset, int yOffset, int zOffset) =>
            AddDoor(null, itemID, xOffset, yOffset, zOffset);

        protected BaseDoor AddDoor(Mobile from, int itemID, int xOffset, int yOffset, int zOffset)
        {
            BaseDoor door = null;

            if (itemID >= 0x675 && itemID < 0x6F5)
            {
                var type = (itemID - 0x675) / 16;
                var facing = (DoorFacing)((itemID - 0x675) / 2 % 8);

                door = type switch
                {
                    0 => new GenericHouseDoor(facing, 0x675, 0xEC, 0xF3),
                    1 => new GenericHouseDoor(facing, 0x685, 0xEC, 0xF3),
                    2 => new GenericHouseDoor(facing, 0x695, 0xEB, 0xF2),
                    3 => new GenericHouseDoor(facing, 0x6A5, 0xEA, 0xF1),
                    4 => new GenericHouseDoor(facing, 0x6B5, 0xEA, 0xF1),
                    5 => new GenericHouseDoor(facing, 0x6C5, 0xEC, 0xF3),
                    6 => new GenericHouseDoor(facing, 0x6D5, 0xEA, 0xF1),
                    _ => new GenericHouseDoor(facing, 0x6E5, 0xEA, 0xF1) // 7
                };
            }
            else if (itemID >= 0x314 && itemID < 0x364)
            {
                var type = (itemID - 0x314) / 16;
                var facing = (DoorFacing)((itemID - 0x314) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x314 + type * 16, 0xED, 0xF4);
            }
            else if (itemID >= 0x824 && itemID < 0x834)
            {
                var facing = (DoorFacing)((itemID - 0x824) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x824, 0xEC, 0xF3);
            }
            else if (itemID >= 0x839 && itemID < 0x849)
            {
                var facing = (DoorFacing)((itemID - 0x839) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x839, 0xEB, 0xF2);
            }
            else if (itemID >= 0x84C && itemID < 0x85C)
            {
                var facing = (DoorFacing)((itemID - 0x84C) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x84C, 0xEC, 0xF3);
            }
            else if (itemID >= 0x866 && itemID < 0x876)
            {
                var facing = (DoorFacing)((itemID - 0x866) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x866, 0xEB, 0xF2);
            }
            else if (itemID >= 0xE8 && itemID < 0xF8)
            {
                var facing = (DoorFacing)((itemID - 0xE8) / 2 % 8);
                door = new GenericHouseDoor(facing, 0xE8, 0xED, 0xF4);
            }
            else if (itemID >= 0x1FED && itemID < 0x1FFD)
            {
                var facing = (DoorFacing)((itemID - 0x1FED) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x1FED, 0xEC, 0xF3);
            }
            else if (itemID >= 0x241F && itemID < 0x2421)
            {
                // DoorFacing facing = (DoorFacing)(((itemID - 0x241F) / 2) % 8);
                door = new GenericHouseDoor(DoorFacing.NorthCCW, 0x2415, -1, -1);
            }
            else if (itemID >= 0x2423 && itemID < 0x2425)
            {
                // DoorFacing facing = (DoorFacing)(((itemID - 0x241F) / 2) % 8);
                // This one and the above one are 'special' cases, ie: OSI had the ItemID pattern discombobulated for these
                door = new GenericHouseDoor(DoorFacing.WestCW, 0x2423, -1, -1);
            }
            else if (itemID >= 0x2A05 && itemID < 0x2A1D)
            {
                var facing = (DoorFacing)((itemID - 0x2A05) / 2 % 4 + 8);

                var sound = itemID >= 0x2A0D && itemID < 0x2a15 ? 0x539 : -1;

                door = new GenericHouseDoor(facing, 0x29F5 + 8 * ((itemID - 0x2A05) / 8), sound, sound);
            }
            else if (itemID == 0x2D46)
            {
                door = new GenericHouseDoor(DoorFacing.NorthCW, 0x2D46, 0xEA, 0xF1, false);
            }
            else if (itemID is 0x2D48 or 0x2FE2)
            {
                door = new GenericHouseDoor(DoorFacing.SouthCCW, itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x2D63 && itemID < 0x2D70)
            {
                var mod = (itemID - 0x2D63) / 2 % 2;
                var facing = mod == 0 ? DoorFacing.SouthCCW : DoorFacing.WestCCW;

                var type = (itemID - 0x2D63) / 4;

                door = new GenericHouseDoor(facing, 0x2D63 + 4 * type + mod * 2, 0xEA, 0xF1, false);
            }
            else if (itemID is 0x2FE4 or 0x31AE)
            {
                door = new GenericHouseDoor(DoorFacing.WestCCW, itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x319C && itemID < 0x31AE)
            {
                // special case for 0x31aa <-> 0x31a8 (a9)
                var mod = (itemID - 0x319C) / 2 % 2;

                var facing = itemID switch
                {
                    0x31AA => mod == 0 ? DoorFacing.NorthCW : DoorFacing.EastCW,
                    0x31A8 => mod == 0 ? DoorFacing.NorthCW : DoorFacing.EastCW,
                    _      => mod == 0 ? DoorFacing.EastCW : DoorFacing.NorthCW
                };

                var type = (itemID - 0x319C) / 4;

                door = new GenericHouseDoor(facing, 0x319C + 4 * type + mod * 2, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x367B && itemID < 0x369B)
            {
                var type = (itemID - 0x367B) / 16;
                var facing = (DoorFacing)((itemID - 0x367B) / 2 % 8);

                door = type switch
                {
                    0 => new GenericHouseDoor(facing, 0x367B, 0xED, 0xF4),
                    _ => new GenericHouseDoor(facing, 0x368B, 0xEC, 0x3E7) // 1
                };
            }
            else if (itemID >= 0x409B && itemID < 0x40A3)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x409B), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x410C && itemID < 0x4114)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x410C), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x41C2 && itemID < 0x41CA)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x41C2), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x41CF && itemID < 0x41D7)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x41CF), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x436E && itemID < 0x437E)
            {
                /* These ones had to be different...
                * Offset 0 2 4 6 8 10 12 14
                * DoorFacing 2 3 2 3 6 7 6 7
                */
                var offset = itemID - 0x436E;
                var facing = (DoorFacing)((offset / 2 + 2 * ((1 + offset / 4) % 2)) % 8);
                door = new GenericHouseDoor(facing, itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x46DD && itemID < 0x46E5)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x46DD), itemID, 0xEB, 0xF2, false);
            }
            else if (itemID >= 0x4D22 && itemID < 0x4D2A)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x4D22), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x50C8 && itemID < 0x50D0)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x50C8), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x50D0 && itemID < 0x50D8)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x50D0), itemID, 0xEA, 0xF1, false);
            }
            else if (itemID >= 0x5142 && itemID < 0x514A)
            {
                door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x5142), itemID, 0xF0, 0xEF, false);
            }
            // TODO: Fix this because the heuristic is broken, or remove the type calculation
            else if (itemID >= 0x9AD7 && itemID <= 0x9AE6)
            {
                var type = (itemID - 0x9AD7) / 16;
                var facing = (DoorFacing)((itemID - 0x9AD7) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x9AD7 + type * 16, 0xED, 0xF4);
            }
            // TODO: Fix this because the heuristic is broken, or remove the type calculation
            else if (itemID >= 0x9B3C && itemID <= 0x9B4B)
            {
                var type = (itemID - 0x9B3C) / 16;
                var facing = (DoorFacing)((itemID - 0x9B3C) / 2 % 8);
                door = new GenericHouseDoor(facing, 0x9B3C + type * 16, 0xED, 0xF4);
            }

            if (door != null)
            {
                if (from != null)
                {
                    door.KeyValue = CreateKeys(from);
                }

                AddDoor(door, xOffset, yOffset, zOffset);
            }
            else
            {
                Console.WriteLine("BaseHouse: Door ItemID {0} not supported.", itemID);
            }

            return door;
        }

        /* Offset 0 2 4 6
         * DoorFacing 2 3 6 7
         */
        private static DoorFacing GetSADoorFacing(int offset) => (DoorFacing)((offset / 2 + 2 * (1 + offset / 4)) % 8);

        public uint CreateKeys(Mobile m)
        {
            var value = Key.RandomValue();

            if (!IsAosRules)
            {
                var packKey = new Key(KeyType.Gold);
                var bankKey = new Key(KeyType.Gold);

                packKey.KeyValue = value;
                bankKey.KeyValue = value;

                packKey.LootType = LootType.Newbied;
                bankKey.LootType = LootType.Newbied;

                var box = m.BankBox;

                if (!box.TryDropItem(m, bankKey, false))
                {
                    bankKey.Delete();
                }

                m.AddToBackpack(packKey);
            }

            return value;
        }

        public BaseDoor[] AddSouthDoors(int x, int y, int z) => AddSouthDoors(true, x, y, z, false);

        public BaseDoor[] AddSouthDoors(bool wood, int x, int y, int z, bool inv)
        {
            var westDoor = MakeDoor(wood, inv ? DoorFacing.WestCCW : DoorFacing.WestCW);
            var eastDoor = MakeDoor(wood, inv ? DoorFacing.EastCW : DoorFacing.EastCCW);

            westDoor.Link = eastDoor;
            eastDoor.Link = westDoor;

            AddDoor(westDoor, x, y, z);
            AddDoor(eastDoor, x + 1, y, z);

            return new[] { westDoor, eastDoor };
        }

        public BaseDoor MakeDoor(bool wood, DoorFacing facing)
        {
            if (wood)
            {
                return new DarkWoodHouseDoor(facing);
            }

            return new MetalHouseDoor(facing);
        }

        public void AddDoor(BaseDoor door, int xoff, int yoff, int zoff)
        {
            door.MoveToWorld(new Point3D(xoff + X, yoff + Y, zoff + Z), Map);
            Doors.Add(door);
        }

        public void AddTrashBarrel(Mobile from)
        {
            if (!IsActive)
            {
                return;
            }

            for (var i = 0; i < Doors?.Count; ++i)
            {
                var door = Doors[i];
                var p = door.Location;

                if (door.Open)
                {
                    p = new Point3D(p.X - door.Offset.X, p.Y - door.Offset.Y, p.Z - door.Offset.Z);
                }

                if (from.Z + 16 >= p.Z && p.Z + 16 >= from.Z)
                {
                    if (from.InRange(p, 1))
                    {
                        from.SendLocalizedMessage(502120); // You cannot place a trash barrel near a door or near steps.
                        return;
                    }
                }
            }

            if (m_Trash?.Deleted != false)
            {
                m_Trash = new TrashBarrel { Movable = false };
                m_Trash.MoveToWorld(from.Location, from.Map);

                /* You have a new trash barrel.
                 * Three minutes after you put something in the barrel, the trash will be emptied.
                 * Be forewarned, this is permanent!
                 */
                from.SendLocalizedMessage(502121);
            }
            else
            {
                from.SendLocalizedMessage(502117); // You already have a trash barrel!
            }
        }

        public void SetSign(int xoff, int yoff, int zoff)
        {
            Sign = new HouseSign(this);
            Sign.MoveToWorld(new Point3D(X + xoff, Y + yoff, Z + zoff), Map);
        }

        private void SetLockdown(Item i, bool locked, bool checkContains = false)
        {
            if (LockDowns == null)
            {
                return;
            }

            if (i is BaseAddonContainer)
            {
                i.Movable = false;
            }
            else
            {
                i.Movable = !locked;
            }

            i.IsLockedDown = locked;

            if (locked)
            {
                if (i is VendorRentalContract contract)
                {
                    if (!VendorRentalContracts.Contains(contract))
                    {
                        VendorRentalContracts.Add(contract);
                    }
                }
                else
                {
                    if (!checkContains || !LockDowns.Contains(i))
                    {
                        LockDowns.Add(i);
                    }
                }
            }
            else
            {
                if (i is VendorRentalContract contract)
                {
                    VendorRentalContracts.Remove(contract);
                }

                LockDowns.Remove(i);
            }

            if (!locked)
            {
                i.SetLastMoved();
            }

            if (i is Container && (!locked || !(i is BaseBoard or Aquarium or FishBowl)))
            {
                foreach (var c in i.Items)
                {
                    SetLockdown(c, locked, checkContains);
                }
            }
        }

        public bool LockDown(Mobile m, Item item) => LockDown(m, item, true);

        public bool LockDown(Mobile m, Item item, bool checkIsInside)
        {
            if (!IsCoOwner(m) || !IsActive)
            {
                return false;
            }

            if (item is BaseAddonContainer || item.Movable && !HasSecureItem(item))
            {
                var amt = 1 + item.TotalItems;

                var rootItem = item.RootParent as Item;
                var parentItem = item.Parent as Item;

                if (checkIsInside && item.RootParent is Mobile)
                {
                    m.SendLocalizedMessage(1005525); // That is not in your house
                }
                else if (checkIsInside && !IsInside(item.GetWorldLocation(), item.ItemData.Height))
                {
                    m.SendLocalizedMessage(1005525); // That is not in your house
                }
                else if (Ethic.IsImbued(item))
                {
                    m.SendLocalizedMessage(1005377); // You cannot lock that down
                }
                else if (HasSecureItem(rootItem))
                {
                    m.SendLocalizedMessage(501737); // You need not lock down items in a secure container.
                }
                else if (parentItem != null && !HasLockedDownItem(parentItem))
                {
                    m.SendLocalizedMessage(501736); // You must lockdown the container first!
                }
                else if (item is not VendorRentalContract && (IsAosRules
                    ? !CheckAosLockdowns(amt) || !CheckAosStorage(amt)
                    : LockDownCount + amt > MaxLockDowns))
                {
                    m.SendLocalizedMessage(1005379); // That would exceed the maximum lock down limit for this house
                }
                else
                {
                    SetLockdown(item, true);
                    return true;
                }
            }
            else if (LockDowns.IndexOf(item) != -1)
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1005526); // That is already locked down
                return true;
            }
            else if (item is HouseSign or Static)
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1005526); // This is already locked down.
            }
            else
            {
                m.SendLocalizedMessage(1005377); // You cannot lock that down
            }

            return false;
        }

        public bool CheckTransferPosition(Mobile from, Mobile to)
        {
            var isValid = true;
            Item sign = Sign;
            var p = sign?.GetWorldLocation() ?? Point3D.Zero;

            if (from.Map != Map || to.Map != Map)
            {
                isValid = false;
            }
            else if (sign == null)
            {
                isValid = false;
            }
            else if (from.Map != sign.Map || to.Map != sign.Map)
            {
                isValid = false;
            }
            else if (IsInside(from))
            {
                isValid = false;
            }
            else if (IsInside(to))
            {
                isValid = false;
            }
            else if (!from.InRange(p, 2))
            {
                isValid = false;
            }
            else if (!to.InRange(p, 2))
            {
                isValid = false;
            }

            if (!isValid)
            {
                // In order to transfer the house, you and the recipient must both be outside the building and within two paces of the house sign.
                from.SendLocalizedMessage(1062067);
            }

            return isValid;
        }

        public void BeginConfirmTransfer(Mobile from, Mobile to)
        {
            if (Deleted || !from.CheckAlive() || !IsOwner(from))
            {
                return;
            }

            if (NewVendorSystem && HasPersonalVendors)
            {
                // You cannot trade this house while you still have personal vendors inside.
                from.SendLocalizedMessage(1062467);
            }
            else if (DecayLevel == DecayLevel.DemolitionPending)
            {
                // This house has been marked for demolition, and it cannot be transferred.
                from.SendLocalizedMessage(1005321);
            }
            else if (from == to)
            {
                from.SendLocalizedMessage(1005330); // You cannot transfer a house to yourself, silly.
            }
            else if (HasAccountHouse(to))
            {
                from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
            }
            else if (CheckTransferPosition(from, to))
            {
                from.SendLocalizedMessage(1005326); // Please wait while the other player verifies the transfer.

                if (HasRentedVendors)
                {
                    /* You are about to be traded a home that has active vendor contracts.
                     * While there are active vendor contracts in this house, you
                     * <strong>cannot</strong> demolish <strong>OR</strong> customize the home.
                     * When you accept this house, you also accept landlordship for every
                     * contract vendor in the house.
                     */
                    to.SendGump(
                        new WarningGump(
                            1060635,
                            30720,
                            1062487,
                            32512,
                            420,
                            280,
                            okay => ConfirmTransfer_Callback(to, okay, from)
                        )
                    );
                }
                else
                {
                    to.CloseGump<HouseTransferGump>();
                    to.SendGump(new HouseTransferGump(from, to, this));
                }
            }
        }

        private void ConfirmTransfer_Callback(Mobile to, bool ok, Mobile from)
        {
            if (!ok || Deleted || !from.CheckAlive() || !IsOwner(from))
            {
                return;
            }

            if (CheckTransferPosition(from, to))
            {
                to.CloseGump<HouseTransferGump>();
                to.SendGump(new HouseTransferGump(from, to, this));
            }
        }

        public void EndConfirmTransfer(Mobile from, Mobile to)
        {
            if (Deleted || !from.CheckAlive() || !IsOwner(from))
            {
                return;
            }

            if (NewVendorSystem && HasPersonalVendors)
            {
                // You cannot trade this house while you still have personal vendors inside.
                from.SendLocalizedMessage(1062467);
            }
            else if (DecayLevel == DecayLevel.DemolitionPending)
            {
                // This house has been marked for demolition, and it cannot be transferred.
                from.SendLocalizedMessage(1005321);
            }
            else if (from == to)
            {
                from.SendLocalizedMessage(1005330); // You cannot transfer a house to yourself, silly.
            }
            else if (HasAccountHouse(to))
            {
                from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
            }
            else if (CheckTransferPosition(from, to))
            {
                var fromState = from.NetState;
                var toState = to.NetState;

                if (fromState != null && toState != null)
                {
                    if (from.HasTrade)
                    {
                        // You cannot trade a house while you have other trades pending.
                        from.SendLocalizedMessage(1062071);
                    }
                    else if (to.HasTrade)
                    {
                        // You cannot trade a house while you have other trades pending.
                        to.SendLocalizedMessage(1062071);
                    }
                    else if (!to.Alive)
                    {
                        // TODO: Check if the message is correct.
                        from.SendLocalizedMessage(1062069); // You cannot transfer this house to that person.
                    }
                    else
                    {
                        Container c = fromState.AddTrade(toState);

                        c.DropItem(new TransferItem(this));
                    }
                }
            }
        }

        public void Release(Mobile m, Item item)
        {
            if (!IsCoOwner(m) || !IsActive)
            {
                return;
            }

            if (HasLockedDownItem(item))
            {
                item.PublicOverheadMessage(MessageType.Label, 0x3B2, 501657); // [no longer locked down]
                SetLockdown(item, false);
                // TidyItemList( m_LockDowns );

                (item as RewardBrazier)?.TurnOff();
            }
            else if (HasSecureItem(item))
            {
                ReleaseSecure(m, item);
            }
            else
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1010416); // This is not locked down or secured.
            }
        }

        public void AddSecure(Mobile m, Item item)
        {
            if (Secures == null || !IsOwner(m) || !IsActive)
            {
                return;
            }

            if (!IsInside(item))
            {
                m.SendLocalizedMessage(1005525); // That is not in your house
            }
            else if (HasLockedDownItem(item))
            {
                m.SendLocalizedMessage(1010550); // This is already locked down and cannot be secured.
            }
            else if (item is not Container)
            {
                LockDown(m, item);
            }
            else
            {
                SecureInfo info = null;

                for (var i = 0; info == null && i < Secures.Count; ++i)
                {
                    if (Secures[i].Item == item)
                    {
                        info = Secures[i];
                    }
                }

                if (info != null)
                {
                    m.CloseGump<SetSecureLevelGump>();
                    m.SendGump(new SetSecureLevelGump(m_Owner, info, this));
                }
                else if (item.Parent != null)
                {
                    m.SendLocalizedMessage(1010423); // You cannot secure this, place it on the ground first.
                }
                // Mondain's Legacy mod
                else if (item is not BaseAddonContainer && !item.Movable)
                {
                    m.SendLocalizedMessage(1010424); // You cannot secure this.
                }
                else if (!IsAosRules && SecureCount >= MaxSecures)
                {
                    // The maximum number of secure items has been reached :
                    m.SendLocalizedMessage(1008142, true, MaxSecures.ToString());
                }
                else if (IsAosRules ? !CheckAosLockdowns(1) : LockDownCount + 125 >= MaxLockDowns)
                {
                    m.SendLocalizedMessage(1005379); // That would exceed the maximum lock down limit for this house
                }
                else if (IsAosRules && !CheckAosStorage(item.TotalItems))
                {
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.
                }
                else
                {
                    info = new SecureInfo((Container)item, SecureLevel.Owner);

                    item.IsLockedDown = false;
                    item.IsSecure = true;

                    Secures.Add(info);
                    LockDowns.Remove(item);
                    item.Movable = false;

                    m.CloseGump<SetSecureLevelGump>();
                    m.SendGump(new SetSecureLevelGump(m_Owner, info, this));
                }
            }
        }

        public virtual bool IsCombatRestricted(Mobile m)
        {
            if (m?.Player != true || m.AccessLevel >= AccessLevel.GameMaster || !IsAosRules ||
                m_Owner?.AccessLevel >= AccessLevel.GameMaster)
            {
                return false;
            }

            for (var i = 0; i < m.Aggressed.Count; ++i)
            {
                var info = m.Aggressed[i];

                if (info.Defender.Player && info.Defender.Alive &&
                    Core.Now - info.LastCombatTime < HouseRegion.CombatHeatDelay &&
                    (m.Guild is not Guild attackerGuild || info.Defender.Guild is not Guild defenderGuild ||
                     defenderGuild != attackerGuild && !defenderGuild.IsEnemy(attackerGuild)))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSecureAccess(Mobile m, SecureLevel level)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (IsCombatRestricted(m))
            {
                return false;
            }

            return level switch
            {
                SecureLevel.Owner    => IsOwner(m),
                SecureLevel.CoOwners => IsCoOwner(m),
                SecureLevel.Friends  => IsFriend(m),
                SecureLevel.Anyone   => true,
                SecureLevel.Guild    => IsGuildMember(m),
                _                    => false
            };
        }

        public void ReleaseSecure(Mobile m, Item item)
        {
            if (Secures == null || !IsOwner(m) || item is StrongBox || !IsActive)
            {
                return;
            }

            for (var i = 0; i < Secures.Count; ++i)
            {
                var info = Secures[i];

                if (info.Item == item && HasSecureAccess(m, info.Level))
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;

                    if (item is BaseAddonContainer)
                    {
                        item.Movable = false;
                    }
                    else
                    {
                        item.Movable = true;
                    }

                    item.SetLastMoved();
                    item.PublicOverheadMessage(MessageType.Label, 0x3B2, 501656); // [no longer secure]
                    Secures.RemoveAt(i);
                    return;
                }
            }

            m.SendLocalizedMessage(501717); // This isn't secure...
        }

        public void AddStrongBox(Mobile from)
        {
            if (!IsCoOwner(from) || !IsActive)
            {
                return;
            }

            if (from == Owner)
            {
                from.SendLocalizedMessage(502109); // Owners don't get a strong box
                return;
            }

            if (IsAosRules ? !CheckAosLockdowns(1) : LockDownCount + 1 > MaxLockDowns)
            {
                from.SendLocalizedMessage(1005379); // That would exceed the maximum lock down limit for this house
                return;
            }

            foreach (var info in Secures)
            {
                var c = info.Item;

                if (!c.Deleted && c is StrongBox box && box.Owner == from)
                {
                    from.SendLocalizedMessage(502112); // You already have a strong box
                    return;
                }
            }

            for (var i = 0; i < Doors?.Count; ++i)
            {
                var door = Doors[i];
                var p = door.Location;

                if (door.Open)
                {
                    p = new Point3D(p.X - door.Offset.X, p.Y - door.Offset.Y, p.Z - door.Offset.Z);
                }

                if (from.Z + 16 >= p.Z && p.Z + 16 >= from.Z)
                {
                    if (from.InRange(p, 1))
                    {
                        from.SendLocalizedMessage(502113); // You cannot place a strongbox near a door or near steps.
                        return;
                    }
                }
            }

            var sb = new StrongBox(from, this) { Movable = false, IsLockedDown = false, IsSecure = true };
            Secures.Add(new SecureInfo(sb, SecureLevel.CoOwners));
            sb.MoveToWorld(from.Location, from.Map);
        }

        public void Kick(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || Friends == null)
            {
                return;
            }

            if (targ.AccessLevel > AccessLevel.Player && from.AccessLevel <= targ.AccessLevel)
            {
                from.SendLocalizedMessage(501346); // Uh oh...a bigger boot may be required!
            }
            else if (IsFriend(targ) && !Core.ML)
            {
                from.SendLocalizedMessage(501348); // You cannot eject a friend of the house!
            }
            else if (targ is PlayerVendor)
            {
                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
            }
            else if (!IsInside(targ))
            {
                from.SendLocalizedMessage(501352); // You may not eject someone who is not in your house!
            }
            else if (targ is BaseCreature creature && creature.NoHouseRestrictions)
            {
                from.SendLocalizedMessage(501347); // You cannot eject that from the house!
            }
            else
            {
                targ.MoveToWorld(BanLocation, Map);

                from.SendLocalizedMessage(1042840, targ.Name); // ~1_PLAYER NAME~ has been ejected from this house.
                /* You have been ejected from this house.
                 * If you persist in entering, you may be banned from the house.
                 */
                targ.SendLocalizedMessage(501341);
            }
        }

        public void RemoveAccess(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || Access == null)
            {
                return;
            }

            if (Access.Contains(targ))
            {
                Access.Remove(targ);

                if (!HasAccess(targ) && IsInside(targ))
                {
                    targ.Location = BanLocation;
                    targ.SendLocalizedMessage(1060734); // Your access to this house has been revoked.
                }

                from.SendLocalizedMessage(1050051); // The invitation has been revoked.
            }
        }

        public void RemoveBan(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || Bans == null)
            {
                return;
            }

            if (Bans.Contains(targ))
            {
                Bans.Remove(targ);

                from.SendLocalizedMessage(501297); // The ban is lifted.
            }
        }

        public void Ban(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || Bans == null)
            {
                return;
            }

            if (targ.AccessLevel > AccessLevel.Player && from.AccessLevel <= targ.AccessLevel)
            {
                from.SendLocalizedMessage(501354); // Uh oh...a bigger boot may be required.
            }
            else if (IsFriend(targ))
            {
                from.SendLocalizedMessage(501348); // You cannot eject a friend of the house!
            }
            else if (targ is PlayerVendor)
            {
                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
            }
            else if (Bans.Count >= MaxBans)
            {
                from.SendLocalizedMessage(501355); // The ban limit for this house has been reached!
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501356); // This person is already banned!
            }
            else if (!IsInside(targ))
            {
                from.SendLocalizedMessage(501352); // You may not eject someone who is not in your house!
            }
            else if (!Public && IsAosRules)
            {
                // You cannot ban someone from a private house.  Revoke their access instead.
                from.SendLocalizedMessage(1062521);
            }
            else if (targ is BaseCreature bc && bc.NoHouseRestrictions)
            {
                from.SendLocalizedMessage(1062040); // You cannot ban that.
            }
            else
            {
                Bans.Add(targ);

                from.SendLocalizedMessage(1042839, targ.Name); // ~1_PLAYER_NAME~ has been banned from this house.
                targ.SendLocalizedMessage(501340);             // You have been banned from this house.

                targ.MoveToWorld(BanLocation, Map);
            }
        }

        public void GrantAccess(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || Access == null)
            {
                return;
            }

            if (HasAccess(targ))
            {
                from.SendLocalizedMessage(1060729); // That person already has access to this house.
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(1060712); // That is not a player.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501367); // This person is banned!  Unban them first.
            }
            else
            {
                Access.Add(targ);

                targ.SendLocalizedMessage(1060735); // You have been granted access to this house.
            }
        }

        public void AddCoOwner(Mobile from, Mobile targ)
        {
            if (!IsOwner(from) || CoOwners == null || Friends == null)
            {
                return;
            }

            if (IsOwner(targ))
            {
                from.SendLocalizedMessage(501360); // This person is already the house owner!
            }
            else if (Friends.Contains(targ))
            {
                from.SendLocalizedMessage(501361); // This person is a friend of the house. Remove them first.
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(501362); // That can't be a co-owner of the house.
            }
            else if (!Core.AOS && HasAccountHouse(targ))
            {
                from.SendLocalizedMessage(501364); // That person is already a house owner.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501367); // This person is banned!  Unban them first.
            }
            else if (CoOwners.Count >= MaxCoOwners)
            {
                from.SendLocalizedMessage(501368); // Your co-owner list is full!
            }
            else if (CoOwners.Contains(targ))
            {
                from.SendLocalizedMessage(501369); // This person is already on your co-owner list!
            }
            else
            {
                CoOwners.Add(targ);

                targ.Delta(MobileDelta.Noto);
                targ.SendLocalizedMessage(501343); // You have been made a co-owner of this house.
            }
        }

        public void RemoveCoOwner(Mobile from, Mobile targ)
        {
            if (!IsOwner(from) || CoOwners == null)
            {
                return;
            }

            if (CoOwners.Contains(targ))
            {
                CoOwners.Remove(targ);

                targ.Delta(MobileDelta.Noto);

                from.SendLocalizedMessage(501299); // Co-owner removed from list.
                targ.SendLocalizedMessage(501300); // You have been removed as a house co-owner.

                foreach (var info in Secures)
                {
                    var c = info.Item;

                    if (c is StrongBox box && box.Owner == targ)
                    {
                        box.IsLockedDown = false;
                        box.IsSecure = false;
                        Secures.Remove(info);
                        box.Destroy();
                        break;
                    }
                }
            }
        }

        public void AddFriend(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || Friends == null || CoOwners == null)
            {
                return;
            }

            if (IsOwner(targ))
            {
                from.SendLocalizedMessage(501370); // This person is already an owner of the house!
            }
            else if (CoOwners.Contains(targ))
            {
                from.SendLocalizedMessage(501369); // This person is already on your co-owner list!
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(501371); // That can't be a friend of the house.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501374); // This person is banned!  Unban them first.
            }
            else if (Friends.Count >= MaxFriends)
            {
                from.SendLocalizedMessage(501375); // Your friends list is full!
            }
            else if (Friends.Contains(targ))
            {
                from.SendLocalizedMessage(501376); // This person is already on your friends list!
            }
            else
            {
                Friends.Add(targ);

                targ.Delta(MobileDelta.Noto);
                targ.SendLocalizedMessage(501337); // You have been made a friend of this house.
            }
        }

        public void RemoveFriend(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || Friends == null)
            {
                return;
            }

            if (Friends.Contains(targ))
            {
                Friends.Remove(targ);

                targ.Delta(MobileDelta.Noto);

                from.SendLocalizedMessage(501298);  // Friend removed from list.
                targ.SendLocalizedMessage(1060751); // You are no longer a friend of this house.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(15); // version

            if (!DynamicDecay.Enabled)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write((int)m_CurrentStage);
                writer.Write(NextDecayStage);
            }

            writer.Write(m_RelativeBanLocation);

            VendorRentalContracts.Tidy();
            writer.Write(VendorRentalContracts);
            InternalizedVendors.Tidy();
            writer.Write(InternalizedVendors);

            writer.WriteEncodedInt(RelocatedEntities.Count);
            foreach (var relEntity in RelocatedEntities)
            {
                writer.Write(relEntity.RelativeLocation);

                if (relEntity.Entity.Deleted)
                {
                    writer.Write(Serial.MinusOne);
                }
                else
                {
                    writer.Write(relEntity.Entity.Serial);
                }
            }

            writer.WriteEncodedInt(VendorInventories.Count);
            for (var i = 0; i < VendorInventories.Count; i++)
            {
                var inventory = VendorInventories[i];
                inventory.Serialize(writer);
            }

            writer.Write(LastRefreshed);
            writer.Write(RestrictDecay);

            writer.Write(Visits);

            writer.Write(Price);

            writer.Write(Access);

            writer.Write(BuiltOn);
            writer.Write(LastTraded);

            Addons.Tidy();
            writer.Write(Addons);

            writer.Write(Secures.Count);

            for (var i = 0; i < Secures.Count; ++i)
            {
                Secures[i].Serialize(writer);
            }

            writer.Write(m_Public);

            // writer.Write( BanLocation );

            writer.Write(m_Owner);

            // Version 5 no longer serializes region coords
            CoOwners.Tidy();
            writer.Write(CoOwners);
            Friends.Tidy();
            writer.Write(Friends);
            Bans.Tidy();
            writer.Write(Bans);

            writer.Write(Sign);
            writer.Write(m_Trash);

            Doors.Tidy();
            writer.Write(Doors);
            LockDowns.Tidy();
            writer.Write(LockDowns);
            // writer.WriteItemList( m_Secures, true );

            writer.Write(MaxLockDowns);
            writer.Write(MaxSecures);

            // Items in locked down containers that aren't locked down themselves must decay!
            for (var i = 0; i < LockDowns.Count; ++i)
            {
                var item = LockDowns[i];

                if (item is Container cont && !(cont is BaseBoard or Aquarium or FishBowl))
                {
                    var children = cont.Items;

                    for (var j = 0; j < children.Count; ++j)
                    {
                        var child = children[j];

                        if (child.Decays && !child.IsLockedDown && !child.IsSecure &&
                            child.LastMoved + child.DecayTime <= Core.Now)
                        {
                            Timer.StartTimer(child.Delete);
                        }
                    }
                }
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            int count;
            var loadedDynamicDecay = false;

            switch (version)
            {
                case 15:
                    {
                        var stage = reader.ReadInt();

                        if (stage != -1)
                        {
                            m_CurrentStage = (DecayLevel)stage;
                            NextDecayStage = reader.ReadDateTime();
                            loadedDynamicDecay = true;
                        }

                        goto case 14;
                    }
                case 14:
                    {
                        m_RelativeBanLocation = reader.ReadPoint3D();
                        goto case 13;
                    }
                case 13: // removed ban location serialization
                case 12:
                    {
                        VendorRentalContracts = reader.ReadEntityList<VendorRentalContract>();
                        InternalizedVendors = reader.ReadEntityList<Mobile>();

                        var relocatedCount = reader.ReadEncodedInt();
                        for (var i = 0; i < relocatedCount; i++)
                        {
                            var relLocation = reader.ReadPoint3D();
                            var entity = reader.ReadEntity<IEntity>();

                            if (entity != null)
                            {
                                RelocatedEntities.Add(new RelocatedEntity(entity, relLocation));
                            }
                        }

                        var inventoryCount = reader.ReadEncodedInt();
                        for (var i = 0; i < inventoryCount; i++)
                        {
                            var inventory = new VendorInventory(this, reader);
                            VendorInventories.Add(inventory);
                        }

                        goto case 11;
                    }
                case 11:
                    {
                        LastRefreshed = reader.ReadDateTime();
                        RestrictDecay = reader.ReadBool();
                        goto case 10;
                    }
                case 10: // just a signal for updates
                case 9:
                    {
                        Visits = reader.ReadInt();
                        goto case 8;
                    }
                case 8:
                    {
                        Price = reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        Access = reader.ReadEntityList<Mobile>();
                        goto case 6;
                    }
                case 6:
                    {
                        BuiltOn = reader.ReadDateTime();
                        LastTraded = reader.ReadDateTime();
                        goto case 5;
                    }
                case 5: // just removed fields
                case 4:
                    {
                        Addons = reader.ReadEntityList<Item>();
                        goto case 3;
                    }
                case 3:
                    {
                        count = reader.ReadInt();
                        Secures = new List<SecureInfo>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            var info = new SecureInfo(reader);

                            if (info.Item != null)
                            {
                                info.Item.IsSecure = true;
                                Secures.Add(info);
                            }
                        }

                        goto case 2;
                    }
                case 2:
                    {
                        m_Public = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 13)
                        {
                            reader.ReadPoint3D(); // house ban location
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 14)
                        {
                            m_RelativeBanLocation = BaseBanLocation;
                        }

                        if (version < 12)
                        {
                            VendorRentalContracts = new List<VendorRentalContract>();
                            InternalizedVendors = new List<Mobile>();
                        }

                        if (version < 4)
                        {
                            Addons = new List<Item>();
                        }

                        if (version < 7)
                        {
                            Access = new List<Mobile>();
                        }

                        if (version < 8)
                        {
                            Price = DefaultPrice;
                        }

                        m_Owner = reader.ReadEntity<Mobile>();

                        if (version < 5)
                        {
                            count = reader.ReadInt();

                            for (var i = 0; i < count; i++)
                            {
                                reader.ReadRect2D();
                            }
                        }

                        UpdateRegion();

                        CoOwners = reader.ReadEntityList<Mobile>();
                        Friends = reader.ReadEntityList<Mobile>();
                        Bans = reader.ReadEntityList<Mobile>();

                        Sign = reader.ReadEntity<HouseSign>();
                        m_Trash = reader.ReadEntity<TrashBarrel>();

                        Doors = reader.ReadEntityList<BaseDoor>();
                        LockDowns = reader.ReadEntityList<Item>();

                        for (var i = 0; i < LockDowns.Count; ++i)
                        {
                            LockDowns[i].IsLockedDown = true;
                        }

                        for (var i = 0; i < VendorRentalContracts.Count; ++i)
                        {
                            VendorRentalContracts[i].IsLockedDown = true;
                        }

                        if (version < 3)
                        {
                            var items = reader.ReadEntityList<Item>();
                            Secures = new List<SecureInfo>(items.Count);

                            for (var i = 0; i < items.Count; ++i)
                            {
                                if (items[i] is Container c)
                                {
                                    c.IsSecure = true;
                                    Secures.Add(new SecureInfo(c, SecureLevel.CoOwners));
                                }
                            }
                        }

                        MaxLockDowns = reader.ReadInt();
                        MaxSecures = reader.ReadInt();

                        if ((Map == null || Map == Map.Internal) && Location == Point3D.Zero)
                        {
                            Delete();
                        }

                        if (m_Owner != null)
                        {
                            if (!m_Table.TryGetValue(m_Owner, out var list))
                            {
                                m_Table[m_Owner] = list = new List<BaseHouse>();
                            }

                            list.Add(this);
                        }

                        break;
                    }
            }

            if (version <= 1)
            {
                ChangeSignType(0xBD2); // private house, plain brass sign
            }

            if (version < 10)
            {
                Timer.StartTimer(FixLockdowns_Sandbox);
            }

            if (version < 11)
            {
                LastRefreshed = Core.Now + TimeSpan.FromHours(24 * Utility.RandomDouble());
            }

            if (DynamicDecay.Enabled && !loadedDynamicDecay)
            {
                var old = GetOldDecayLevel();

                if (old == DecayLevel.DemolitionPending)
                {
                    old = DecayLevel.Collapsed;
                }

                SetDynamicDecay(old);
            }

            if (!CheckDecay())
            {
                if (RelocatedEntities.Count > 0)
                {
                    Timer.StartTimer(RestoreRelocatedEntities);
                }

                if (m_Owner == null && Friends.Count == 0 && CoOwners.Count == 0)
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(10.0), Delete);
                }
            }
        }

        private void FixLockdowns_Sandbox()
        {
            if (LockDowns?.Count > 0)
            {
                using var queue = PooledRefQueue<Item>.Create();
                foreach (var item in LockDowns)
                {
                    if (item is Container)
                    {
                        queue.Enqueue(item);
                    }
                }

                while (queue.Count > 0)
                {
                    SetLockdown(queue.Dequeue(), true, true);
                }
            }
        }

        public static void HandleDeletion(Mobile mob)
        {
            var houses = GetHouses(mob);

            if (houses.Count == 0)
            {
                return;
            }

            var acct = mob.Account as Account;
            Mobile trans = null;

            if (acct != null)
            {
                for (var i = 0; i < acct.Length; ++i)
                {
                    if (acct[i] != null && acct[i] != mob)
                    {
                        trans = acct[i];
                    }
                }
            }

            for (var i = 0; i < houses.Count; ++i)
            {
                var house = houses[i];

                if (trans == null && house.CoOwners.Count == 0)
                {
                    Timer.StartTimer(house.Delete);
                }
                else
                {
                    house.Owner = trans;
                }
            }
        }

        public int GetLockdowns()
        {
            var count = 0;

            if (LockDowns != null)
            {
                for (var i = 0; i < LockDowns.Count; ++i)
                {
                    if (LockDowns[i] != null)
                    {
                        var item = LockDowns[i];

                        if (item is not Container)
                        {
                            count += item.TotalItems;
                        }
                    }

                    count++;
                }
            }

            return count;
        }

        public override void OnDelete()
        {
            RestoreRelocatedEntities();

            new FixColumnTimer(this).Start();

            base.OnDelete();
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Owner != null && m_Table.TryGetValue(m_Owner, out var list) && list.Remove(this) && list.Count == 0)
            {
                m_Table.Remove(m_Owner);
            }

            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }

            Sign?.Delete();

            m_Trash?.Delete();

            if (Doors != null)
            {
                for (var i = 0; i < Doors.Count; ++i)
                {
                    Item item = Doors[i];

                    item?.Delete();
                }

                Doors.Clear();
            }

            if (LockDowns != null)
            {
                for (var i = 0; i < LockDowns.Count; ++i)
                {
                    var item = LockDowns[i];

                    if (item != null)
                    {
                        item.IsLockedDown = false;
                        item.IsSecure = false;
                        item.Movable = true;
                        item.SetLastMoved();
                    }
                }

                LockDowns.Clear();
            }

            if (VendorRentalContracts != null)
            {
                for (var i = 0; i < VendorRentalContracts.Count; ++i)
                {
                    Item item = VendorRentalContracts[i];

                    if (item != null)
                    {
                        item.IsLockedDown = false;
                        item.IsSecure = false;
                        item.Movable = true;
                        item.SetLastMoved();
                    }
                }

                VendorRentalContracts.Clear();
            }

            if (Secures != null)
            {
                for (var i = 0; i < Secures.Count; ++i)
                {
                    var info = Secures[i];

                    if (info.Item is StrongBox)
                    {
                        info.Item.Destroy();
                    }
                    else
                    {
                        info.Item.IsLockedDown = false;
                        info.Item.IsSecure = false;
                        info.Item.Movable = true;
                        info.Item.SetLastMoved();
                    }
                }

                Secures.Clear();
            }

            if (Addons != null)
            {
                for (var i = 0; i < Addons.Count; ++i)
                {
                    var item = Addons[i];

                    if (item != null)
                    {
                        if (!item.Deleted && item is IAddon addon)
                        {
                            var deed = addon.Deed;
                            var retainDeedHue = false; // if the items aren't hued but the deed itself is
                            var hue = 0;

                            // There are things that are IAddon which aren't BaseAddon
                            if (addon is BaseAddon ba && ba.RetainDeedHue)
                            {
                                retainDeedHue = true;

                                for (var j = 0; hue == 0 && j < ba.Components.Count; ++j)
                                {
                                    var c = ba.Components[j];

                                    if (c.Hue != 0)
                                    {
                                        hue = c.Hue;
                                    }
                                }
                            }

                            if (deed != null)
                            {
                                if (retainDeedHue)
                                {
                                    deed.Hue = hue;
                                }

                                deed.MoveToWorld(item.Location, item.Map);
                            }
                        }

                        item.Delete();
                    }
                }

                Addons.Clear();
            }

            if (VendorInventories.Count > 0)
            {
                using var inventories = PooledRefList<VendorInventory>.Create(VendorInventories.Count);
                inventories.AddRange(VendorInventories);
                foreach (var inventory in inventories)
                {
                    inventory.Delete();
                }
            }

            MovingCrate?.Delete();

            KillVendors();

            AllHouses.Remove(this);
        }

        public static bool HasHouse(Mobile m)
        {
            if (m == null || !m_Table.TryGetValue(m, out var list))
            {
                return false;
            }

            foreach (var h in list)
            {
                if (!h.Deleted)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAccountHouse(Mobile m)
        {
            if (m.Account is not Account a)
            {
                return false;
            }

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != null && HasHouse(a[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsOwner(Mobile m) =>
            m != null && (m == m_Owner || m.AccessLevel >= AccessLevel.GameMaster ||
                          IsAosRules && AccountHandler.CheckAccount(m, m_Owner));

        public bool IsCoOwner(Mobile m) =>
            m != null && CoOwners != null &&
            (IsOwner(m) || CoOwners.Contains(m) || !IsAosRules && AccountHandler.CheckAccount(m, m_Owner));

        public bool IsGuildMember(Mobile m) => m != null && Owner?.Guild != null && m.Guild == Owner.Guild;

        public void RemoveKeys(Mobile m)
        {
            if (Doors != null)
            {
                uint keyValue = 0;

                for (var i = 0; keyValue == 0 && i < Doors.Count; ++i)
                {
                    keyValue = Doors[i].KeyValue;
                }

                Key.RemoveKeys(m, keyValue);
            }
        }

        public void ChangeLocks(Mobile m)
        {
            var keyValue = CreateKeys(m);

            if (Doors != null)
            {
                for (var i = 0; i < Doors.Count; ++i)
                {
                    Doors[i].KeyValue = keyValue;
                }
            }
        }

        public void RemoveLocks()
        {
            if (Doors != null)
            {
                for (var i = 0; i < Doors.Count; ++i)
                {
                    var door = Doors[i];
                    door.KeyValue = 0;
                    door.Locked = false;
                }
            }
        }

        public virtual HouseDeed GetDeed() => null;

        public bool IsFriend(Mobile m) => m != null && Friends != null && (IsCoOwner(m) || Friends.Contains(m));

        public bool IsBanned(Mobile m)
        {
            if (m == null || m == Owner || m.AccessLevel > AccessLevel.Player || Bans == null)
            {
                return false;
            }

            var theirAccount = m.Account as Account;

            for (var i = 0; i < Bans.Count; ++i)
            {
                var c = Bans[i];

                if (c == m)
                {
                    return true;
                }

                if (c.Account is Account bannedAccount && bannedAccount == theirAccount)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAccess(Mobile m)
        {
            if (m == null)
            {
                return false;
            }

            if (m.AccessLevel > AccessLevel.Player || IsFriend(m) || Access?.Contains(m) == true)
            {
                return true;
            }

            if (m is not BaseCreature bc)
            {
                return false;
            }

            if (bc.NoHouseRestrictions)
            {
                return true;
            }

            if (!(bc.Controlled || bc.Summoned))
            {
                return false;
            }

            m = bc.ControlMaster ?? bc.SummonMaster;

            return m != null && (m.AccessLevel > AccessLevel.Player || IsFriend(m) || Access?.Contains(m) == true);
        }

        public bool HasLockedDownItem(Item check) =>
            LockDowns?.Contains(check) == true ||
            check is VendorRentalContract contract && VendorRentalContracts.Contains(contract);

        public bool HasSecureItem(Item item)
        {
            if (item == null)
            {
                return false;
            }

            for (var i = 0; i < Secures?.Count; ++i)
            {
                if (Secures[i].Item == item)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual Guildstone FindGuildstone()
        {
            var map = Map;

            if (map == null)
            {
                return null;
            }

            var mcl = Components;
            var bounds = new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height);

            foreach (var gs in map.GetItemsInBounds<Guildstone>(bounds))
            {
                return gs;
            }

            return null;
        }

        public void ResetDynamicDecay()
        {
            m_CurrentStage = DecayLevel.Ageless;
            NextDecayStage = DateTime.MinValue;
        }

        public void SetDynamicDecay(DecayLevel level)
        {
            m_CurrentStage = level;

            if (DynamicDecay.Decays(level))
            {
                NextDecayStage = Core.Now + DynamicDecay.GetRandomDuration(level);
            }
            else
            {
                NextDecayStage = DateTime.MinValue;
            }
        }

        private class TransferItem : Item
        {
            private readonly BaseHouse m_House;

            public TransferItem(BaseHouse house) : base(0x14F0)
            {
                m_House = house;

                Hue = 0x480;
                Movable = false;
            }

            public TransferItem(Serial serial) : base(serial)
            {
            }

            public override string DefaultName => "a house transfer contract";

            public override void GetProperties(IPropertyList list)
            {
                base.GetProperties(list);

                var houseName = m_House == null ? "an unnamed house" : m_House.Sign.GetName();
                var owner = m_House?.Owner?.Name ?? "nobody";

                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                var valid = m_House != null && Sextant.Format(
                    m_House.Location,
                    m_House.Map,
                    ref xLong,
                    ref yLat,
                    ref xMins,
                    ref yMins,
                    ref xEast,
                    ref ySouth
                );

                list.Add(1061112, Utility.FixHtml(houseName)); // House Name: ~1_val~
                list.Add(1061113, owner);                      // Owner: ~1_val~
                if (valid)
                {
                    // Location: ~1_val~
                    list.Add(1061114, $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}, {xLong}° {xMins}'{(xEast ? "E" : "W")}");
                }
                else
                {
                    list.Add(1061114, "unknown"); // Location: ~1_val~
                }
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

                Timer.DelayCall(Delete);
            }

            public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (!base.AllowSecureTrade(from, to, newOwner, accepted))
                {
                    return false;
                }

                if (!accepted)
                {
                    return true;
                }

                if (Deleted || m_House?.Deleted != false || !m_House.IsOwner(from) || !from.CheckAlive() ||
                    !to.CheckAlive())
                {
                    return false;
                }

                if (HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                    return false;
                }

                return m_House.CheckTransferPosition(from, to);
            }

            public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (Deleted)
                {
                    return;
                }

                Delete();

                if (m_House?.Deleted != false || !m_House.IsOwner(from) || !from.CheckAlive() || !to.CheckAlive())
                {
                    return;
                }

                if (!accepted)
                {
                    return;
                }

                from.SendLocalizedMessage(501338); // You have transferred ownership of the house.

                /* You are now the owner of this house.
                 * The house's co-owner, friend, ban, and access lists have been cleared.
                 * You should double-check the security settings on any doors and teleporters in the house.
                 */
                to.SendLocalizedMessage(501339);

                m_House.RemoveKeys(from);
                m_House.Owner = to;
                m_House.Bans.Clear();
                m_House.Friends.Clear();
                m_House.CoOwners.Clear();
                m_House.ChangeLocks(to);
                m_House.LastTraded = Core.Now;
            }
        }

        private class FixColumnTimer : Timer
        {
            private readonly int m_EndX;
            private readonly int m_EndY;
            private readonly Map m_Map;
            private readonly int m_StartX;
            private readonly int m_StartY;

            public FixColumnTimer(BaseMulti multi) : base(TimeSpan.Zero)
            {
                m_Map = multi.Map;

                var mcl = multi.Components;

                m_StartX = multi.X + mcl.Min.X;
                m_StartY = multi.Y + mcl.Min.Y;
                m_EndX = multi.X + mcl.Max.X;
                m_EndY = multi.Y + mcl.Max.Y;
            }

            protected override void OnTick()
            {
                if (m_Map == null)
                {
                    return;
                }

                for (var x = m_StartX; x <= m_EndX; ++x)
                {
                    for (var y = m_StartY; y <= m_EndY; ++y)
                    {
                        m_Map.FixColumn(x, y);
                    }
                }
            }
        }
    }

    public enum DecayType
    {
        Ageless,
        AutoRefresh,
        ManualRefresh,
        Condemned
    }

    public enum DecayLevel
    {
        Ageless,
        LikeNew,
        Slightly,
        Somewhat,
        Fairly,
        Greatly,
        IDOC,
        Collapsed,
        DemolitionPending
    }

    public enum SecureAccessResult
    {
        Insecure,
        Accessible,
        Inaccessible
    }

    public enum SecureLevel
    {
        Owner,
        CoOwners,
        Friends,
        Anyone,
        Guild
    }

    public class SecureInfo : ISecurable
    {
        public SecureInfo(Container item, SecureLevel level)
        {
            Item = item;
            Level = level;
        }

        public SecureInfo(IGenericReader reader)
        {
            Item = reader.ReadEntity<Container>();
            Level = (SecureLevel)reader.ReadByte();
        }

        public Container Item { get; }

        public SecureLevel Level { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(Item);
            writer.Write((byte)Level);
        }
    }

    public class RelocatedEntity
    {
        public RelocatedEntity(IEntity entity, Point3D relativeLocation)
        {
            Entity = entity;
            RelativeLocation = relativeLocation;
        }

        public IEntity Entity { get; }

        public Point3D RelativeLocation { get; }
    }

    public class LockdownTarget : Target
    {
        private readonly BaseHouse m_House;
        private readonly bool m_Release;

        public LockdownTarget(bool release, BaseHouse house) : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Release = release;
            m_House = house;
        }

        protected override void OnTargetNotAccessible(Mobile from, object targeted)
        {
            OnTarget(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsCoOwner(from))
            {
                return;
            }

            if (targeted is Item item)
            {
                if (m_Release)
                {
                    if (item is AddonContainerComponent component)
                    {
                        if (component.Addon != null)
                        {
                            m_House.Release(from, component.Addon);
                        }
                    }
                    else
                    {
                        m_House.Release(from, item);
                    }
                }
                else
                {
                    if (item is VendorRentalContract)
                    {
                        // You must double click the contract in your pack to lock it down.
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1062392);
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501732); // I cannot lock this down!
                    }
                    else if (item is AddonComponent)
                    {
                        from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 501727); // You cannot lock that down!
                        from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 501732); // I cannot lock this down!
                    }
                    else
                    {
                        if (item is AddonContainerComponent component)
                        {
                            if (component.Addon != null)
                            {
                                m_House.LockDown(from, component.Addon);
                            }
                        }
                        else
                        {
                            m_House.LockDown(from, item);
                        }
                    }
                }
            }
            else if (targeted is StaticTarget)
            {
            }
            else
            {
                from.SendLocalizedMessage(1005377); // You cannot lock that down
            }
        }
    }

    public class SecureTarget : Target
    {
        private readonly BaseHouse m_House;
        private readonly bool m_Release;

        public SecureTarget(bool release, BaseHouse house) : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Release = release;
            m_House = house;
        }

        protected override void OnTargetNotAccessible(Mobile from, object targeted)
        {
            OnTarget(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsCoOwner(from))
            {
                return;
            }

            if (targeted is Item item)
            {
                if (m_Release)
                {
                    if (item is AddonContainerComponent component)
                    {
                        if (component.Addon != null)
                        {
                            m_House.ReleaseSecure(from, component.Addon);
                        }
                    }
                    else
                    {
                        m_House.ReleaseSecure(from, item);
                    }
                }
                else
                {
                    if (item is VendorRentalContract)
                    {
                        // You must double click the contract in your pack to lock it down.
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1062392);
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501732); // I cannot lock this down!
                    }
                    else
                    {
                        if (item is AddonContainerComponent component)
                        {
                            if (component.Addon != null)
                            {
                                m_House.AddSecure(from, component.Addon);
                            }
                        }
                        else
                        {
                            m_House.AddSecure(from, item);
                        }
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1010424); // You cannot secure this
            }
        }
    }

    public class HouseKickTarget : Target
    {
        private readonly BaseHouse m_House;

        public HouseKickTarget(BaseHouse house) : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsFriend(from))
            {
                return;
            }

            if (targeted is Mobile mobile)
            {
                m_House.Kick(from, mobile);
            }
            else
            {
                from.SendLocalizedMessage(501347); // You cannot eject that from the house!
            }
        }
    }

    public class HouseBanTarget : Target
    {
        private readonly bool m_Banning;
        private readonly BaseHouse m_House;

        public HouseBanTarget(bool ban, BaseHouse house) : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Banning = ban;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsFriend(from))
            {
                return;
            }

            if (targeted is Mobile mobile)
            {
                if (m_Banning)
                {
                    m_House.Ban(from, mobile);
                }
                else
                {
                    m_House.RemoveBan(from, mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501347); // You cannot eject that from the house!
            }
        }
    }

    public class HouseAccessTarget : Target
    {
        private readonly BaseHouse m_House;

        public HouseAccessTarget(BaseHouse house) : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsFriend(from))
            {
                return;
            }

            if (targeted is Mobile mobile)
            {
                m_House.GrantAccess(from, mobile);
            }
            else
            {
                from.SendLocalizedMessage(1060712); // That is not a player.
            }
        }
    }

    public class CoOwnerTarget : Target
    {
        private readonly bool m_Add;
        private readonly BaseHouse m_House;

        public CoOwnerTarget(bool add, BaseHouse house) : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Add = add;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsOwner(from))
            {
                return;
            }

            if (targeted is Mobile mobile)
            {
                if (m_Add)
                {
                    m_House.AddCoOwner(from, mobile);
                }
                else
                {
                    m_House.RemoveCoOwner(from, mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501362); // That can't be a coowner
            }
        }
    }

    public class HouseFriendTarget : Target
    {
        private readonly bool m_Add;
        private readonly BaseHouse m_House;

        public HouseFriendTarget(bool add, BaseHouse house) : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Add = add;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || m_House.Deleted || !m_House.IsCoOwner(from))
            {
                return;
            }

            if (targeted is Mobile mobile)
            {
                if (m_Add)
                {
                    m_House.AddFriend(from, mobile);
                }
                else
                {
                    m_House.RemoveFriend(from, mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501371); // That can't be a friend
            }
        }
    }

    public class HouseOwnerTarget : Target
    {
        private readonly BaseHouse m_House;

        public HouseOwnerTarget(BaseHouse house) : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile { Player: true } mobile)
            {
                m_House.BeginConfirmTransfer(from, mobile);
            }
            else
            {
                from.SendLocalizedMessage(501384); // Only a player can own a house!
            }
        }
    }

    public class SetSecureLevelEntry : ContextMenuEntry
    {
        private readonly Item m_Item;
        private ISecurable m_Securable;

        public SetSecureLevelEntry(Item item, ISecurable securable) : base(6203, 6)
        {
            m_Item = item;
            m_Securable = securable;
        }

        public static ISecurable GetSecurable(Mobile from, Item item)
        {
            var house = BaseHouse.FindHouseAt(item);

            if (house?.IsOwner(from) != true || !house.IsAosRules)
            {
                return null;
            }

            ISecurable sec = null;

            if (item is ISecurable securable)
            {
                var isOwned = item is BaseDoor door && house.Doors.Contains(door);

                if (!isOwned)
                {
                    isOwned = house is HouseFoundation foundation && foundation.IsFixture(item);
                }

                if (!isOwned)
                {
                    isOwned = house.HasLockedDownItem(item);
                }

                if (isOwned)
                {
                    sec = securable;
                }
            }
            else
            {
                var list = house.Secures;

                for (var i = 0; sec == null && i < list?.Count; ++i)
                {
                    var si = list[i];

                    if (si.Item == item)
                    {
                        sec = si;
                    }
                }
            }

            return sec;
        }

        public static void AddTo(Mobile from, Item item, List<ContextMenuEntry> list)
        {
            var sec = GetSecurable(from, item);

            if (sec != null)
            {
                list.Add(new SetSecureLevelEntry(item, sec));
            }
        }

        public override void OnClick()
        {
            var sec = GetSecurable(Owner.From, m_Item);

            if (sec != null)
            {
                Owner.From.CloseGump<SetSecureLevelGump>();
                Owner.From.SendGump(new SetSecureLevelGump(Owner.From, sec, BaseHouse.FindHouseAt(m_Item)));
            }
        }
    }

    public class TempNoHousingRegion : BaseRegion
    {
        private readonly Mobile m_RegionOwner;

        public TempNoHousingRegion(BaseHouse house, Mobile regionowner)
            : base(null, house.Map, DefaultPriority, house.Region.Area)
        {
            Register();

            m_RegionOwner = regionowner;

            Timer.StartTimer(house.RestrictedPlacingTime, Unregister);
        }

        public override bool AllowHousing(Mobile from, Point3D p) =>
            from == m_RegionOwner || AccountHandler.CheckAccount(from, m_RegionOwner);
    }
}
