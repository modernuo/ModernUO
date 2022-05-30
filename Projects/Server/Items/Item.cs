using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Server.ContextMenus;
using Server.Items;
using Server.Logging;
using Server.Network;
using Server.Targeting;

namespace Server
{
    /// <summary>
    ///     Internal flags used to signal how the item should be updated and resent to nearby clients.
    /// </summary>
    [Flags]
    public enum ItemDelta
    {
        /// <summary>
        ///     Nothing.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        ///     Resend the item.
        /// </summary>
        Update = 0x00000001,

        /// <summary>
        ///     Resend the item only if it is equipped.
        /// </summary>
        EquipOnly = 0x00000002,

        /// <summary>
        ///     Resend the item's properties.
        /// </summary>
        Properties = 0x00000004
    }

    /// <summary>
    ///     Enumeration containing possible ways to handle item ownership on death.
    /// </summary>
    public enum DeathMoveResult
    {
        /// <summary>
        ///     The item should be placed onto the corpse.
        /// </summary>
        MoveToCorpse,

        /// <summary>
        ///     The item should remain equipped.
        /// </summary>
        RemainEquipped,

        /// <summary>
        ///     The item should be placed into the owners backpack.
        /// </summary>
        MoveToBackpack
    }

    /// <summary>
    ///     Enumeration of an item's loot and steal state.
    /// </summary>
    public enum LootType : byte
    {
        /// <summary>
        ///     Stealable. Lootable.
        /// </summary>
        Regular = 0,

        /// <summary>
        ///     Unstealable. Unlootable, unless owned by a murderer.
        /// </summary>
        Newbied = 1,

        /// <summary>
        ///     Unstealable. Unlootable, always.
        /// </summary>
        Blessed = 2,

        /// <summary>
        ///     Stealable. Lootable, always.
        /// </summary>
        Cursed = 3
    }

    public class BounceInfo
    {
        public BounceInfo(Item item)
        {
            Map = item.Map;
            Location = item.Location;
            WorldLoc = item.GetWorldLocation();
            Parent = item.Parent;
        }

        private BounceInfo(Map map, Point3D loc, Point3D worldLoc, IEntity parent)
        {
            Map = map;
            Location = loc;
            WorldLoc = worldLoc;
            Parent = parent;
        }

        public Point3D Location { get; set; }
        public Point3D WorldLoc { get; set; }
        public Map Map { get; set; }
        public IEntity Parent { get; set; }

        public static BounceInfo Deserialize(IGenericReader reader)
        {
            if (reader.ReadBool())
            {
                var map = reader.ReadMap();
                var loc = reader.ReadPoint3D();
                var worldLoc = reader.ReadPoint3D();

                IEntity parent = reader.ReadEntity<IEntity>();

                return new BounceInfo(map, loc, worldLoc, parent);
            }

            return null;
        }

        public static void Serialize(BounceInfo info, IGenericWriter writer)
        {
            if (info == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);

                writer.Write(info.Map);
                writer.Write(info.Location);
                writer.Write(info.WorldLoc);

                if (info.Parent is Mobile mobile)
                {
                    writer.Write(mobile);
                }
                else if (info.Parent is Item item)
                {
                    writer.Write(item);
                }
                else
                {
                    writer.Write((Serial)0);
                }
            }
        }
    }

    public enum TotalType
    {
        Gold,
        Items,
        Weight
    }

    [Flags]
    public enum ExpandFlag
    {
        None = 0x000,

        Name = 0x001,
        Items = 0x002,
        Bounce = 0x004,
        Holder = 0x008,
        Blessed = 0x010,
        TempFlag = 0x020,
        SaveFlag = 0x040,
        Weight = 0x080,
        Spawner = 0x100
    }

    public class Item : IHued, IComparable<Item>, ISpawnable, IPropertyListObject
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Item));

        public const int QuestItemHue = 0x4EA; // Hmmmm... "for EA"?
        public static readonly List<Item> EmptyItems = new();
        private static readonly Queue<Item> m_DeltaQueue = new();

        private static int m_OpenSlots;
        private int m_Amount;

        private CompactInfo m_CompactInfo;

        private ItemDelta m_DeltaFlags;
        private Direction m_Direction;
        private ImplFlag m_Flags;
        private int m_Hue;
        private int m_ItemID;
        private Layer m_Layer;

        private Point3D m_Location;
        private LootType m_LootType;
        private Map m_Map;
        private IEntity m_Parent; // Mobile, Item, or null=World

        private ObjectPropertyList m_PropertyList;

        [Constructible]
        public Item(int itemID = 0)
        {
            m_ItemID = itemID;
            Serial = World.NewItem;

            // m_Items = new ArrayList( 1 );
            Visible = true;
            Movable = true;
            Amount = 1;
            m_Map = Map.Internal;

            SetLastMoved();

            World.AddEntity(this);
            SetTypeRef(GetType());
        }

        public Item(Serial serial)
        {
            Serial = serial;
            SetTypeRef(GetType());
        }

        public void SetTypeRef(Type type)
        {
            TypeRef = World.ItemTypes.IndexOf(type);

            if (TypeRef == -1)
            {
                World.ItemTypes.Add(type);
                TypeRef = World.ItemTypes.Count - 1;
            }
        }

        public int TempFlags
        {
            get => LookupCompactInfo()?.m_TempFlags ?? 0;
            set
            {
                var info = AcquireCompactInfo();

                info.m_TempFlags = value;

                if (info.m_TempFlags == 0)
                {
                    VerifyCompactInfo();
                }
            }
        }

        public int SavedFlags
        {
            get => LookupCompactInfo()?.m_SavedFlags ?? 0;
            set
            {
                var info = AcquireCompactInfo();

                info.m_SavedFlags = value;

                if (info.m_SavedFlags == 0)
                {
                    VerifyCompactInfo();
                }
            }
        }

        /// <summary>
        ///     The <see cref="Mobile" /> who is currently <see cref="Mobile.Holding">holding</see> this item.
        /// </summary>
        public Mobile HeldBy
        {
            get => LookupCompactInfo()?.m_HeldBy;
            set
            {
                var info = AcquireCompactInfo();

                info.m_HeldBy = value;

                if (info.m_HeldBy == null)
                {
                    VerifyCompactInfo();
                }
            }
        }

        /// <summary>
        ///     Overridable. Determines whether the item will show <see cref="AddWeightProperty" />.
        /// </summary>
        public virtual bool DisplayWeight => Core.ML && (Movable || IsLockedDown || IsSecure || ItemData.Weight != 255);

        [CommandProperty(AccessLevel.GameMaster)]
        public LootType LootType
        {
            get => m_LootType;
            set
            {
                if (m_LootType != value)
                {
                    m_LootType = value;

                    if (DisplayLootType)
                    {
                        InvalidateProperties();
                    }
                }
            }
        }

        public static TimeSpan DefaultDecayTime { get; set; } = TimeSpan.FromHours(1.0);

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan DecayTime => DefaultDecayTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Decays => Movable && Visible;

        public DateTime LastMoved { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Stackable
        {
            get => GetFlag(ImplFlag.Stackable);
            set => SetFlag(ImplFlag.Stackable, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Visible
        {
            get => GetFlag(ImplFlag.Visible);
            set
            {
                if (GetFlag(ImplFlag.Visible) != value)
                {
                    SetFlag(ImplFlag.Visible, value);

                    if (m_Map != null)
                    {
                        var worldLoc = GetWorldLocation();

                        var eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                        Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

                        foreach (var state in eable)
                        {
                            var m = state.Mobile;

                            if (!m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                            {
                                OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                                state.Send(removeEntity);
                            }
                        }

                        eable.Free();
                    }

                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Movable
        {
            get => GetFlag(ImplFlag.Movable);
            set
            {
                if (GetFlag(ImplFlag.Movable) != value)
                {
                    SetFlag(ImplFlag.Movable, value);

                    Delta(ItemDelta.Update);
                }
            }
        }

        public virtual bool ForceShowProperties => false;

        public virtual bool HandlesOnMovement => false;

        public static int LockedDownFlag { get; set; }

        public static int SecureFlag { get; set; }

        public bool IsLockedDown
        {
            get => GetTempFlag(LockedDownFlag);
            set
            {
                SetTempFlag(LockedDownFlag, value);
                InvalidateProperties();
            }
        }

        public bool IsSecure
        {
            get => GetTempFlag(SecureFlag);
            set
            {
                SetTempFlag(SecureFlag, value);
                InvalidateProperties();
            }
        }

        public virtual bool IsVirtualItem => false;

        public virtual bool CanSeeStaffOnly(Mobile from) => from.AccessLevel > AccessLevel.Counselor;

        public virtual int LabelNumber
        {
            get
            {
                if (m_ItemID < 0x4000)
                {
                    return 1020000 + m_ItemID;
                }

                return 1078872 + m_ItemID;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalGold => GetTotal(TotalType.Gold);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalItems => GetTotal(TotalType.Items);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalWeight => GetTotal(TotalType.Weight);

        public virtual double DefaultWeight
        {
            get
            {
                if (m_ItemID < 0 || m_ItemID > TileData.MaxItemValue || this is BaseMulti)
                {
                    return 0;
                }

                var weight = TileData.ItemTable[m_ItemID].Weight;

                if (weight is 255 or 0)
                {
                    weight = 1;
                }

                return weight;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public double Weight
        {
            get
            {
                var info = LookupCompactInfo();

                return info != null && info.m_Weight != -1 ? info.m_Weight : DefaultWeight;
            }
            set
            {
                if (Weight != value)
                {
                    var info = AcquireCompactInfo();

                    var oldPileWeight = PileWeight;

                    info.m_Weight = value;

                    if (info.m_Weight == -1)
                    {
                        VerifyCompactInfo();
                    }

                    var newPileWeight = PileWeight;

                    UpdateTotal(this, TotalType.Weight, newPileWeight - oldPileWeight);

                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int PileWeight => (int)Math.Ceiling(Weight * Amount);

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Hue
        {
            get => m_Hue;
            set
            {
                if (m_Hue != value)
                {
                    m_Hue = value;

                    Delta(ItemDelta.Update);
                }
            }
        }

        public virtual bool Nontransferable => QuestItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Layer Layer
        {
            get => m_Layer;
            set
            {
                if (m_Layer != value)
                {
                    m_Layer = value;

                    Delta(ItemDelta.EquipOnly);
                }
            }
        }

        public List<Item> Items => LookupItems() ?? EmptyItems;

        [CommandProperty(AccessLevel.GameMaster)]
        public IEntity RootParent
        {
            get
            {
                var p = m_Parent;

                while (p is Item item)
                {
                    if (item.m_Parent == null)
                    {
                        break;
                    }

                    p = item.m_Parent;
                }

                return p;
            }
        }

        public bool NoMoveHS { get; set; }

        public virtual int PhysicalResistance => 0;
        public virtual int FireResistance => 0;
        public virtual int ColdResistance => 0;
        public virtual int PoisonResistance => 0;
        public virtual int EnergyResistance => 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ItemID
        {
            get => m_ItemID;
            set
            {
                if (m_ItemID != value)
                {
                    var oldPileWeight = PileWeight;

                    m_ItemID = value;

                    var newPileWeight = PileWeight;

                    UpdateTotal(this, TotalType.Weight, newPileWeight - oldPileWeight);

                    InvalidateProperties();
                    Delta(ItemDelta.Update);
                }
            }
        }

        public virtual string DefaultName => null;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get => LookupCompactInfo()?.m_Name ?? DefaultName;
            set
            {
                if (value == null || value != DefaultName)
                {
                    var info = AcquireCompactInfo();

                    info.m_Name = value;

                    if (info.m_Name == null)
                    {
                        VerifyCompactInfo();
                    }

                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Developer)]
        public IEntity Parent
        {
            get => m_Parent;
            set
            {
                if (m_Parent == value)
                {
                    return;
                }

                var oldParent = m_Parent;

                m_Parent = value;

                if (m_Map != null)
                {
                    if (oldParent != null && m_Parent == null)
                    {
                        m_Map.OnEnter(this);
                    }
                    else if (m_Parent != null)
                    {
                        m_Map.OnLeave(this);
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public LightType Light
        {
            get => (LightType)m_Direction;
            set
            {
                if ((LightType)m_Direction != value)
                {
                    m_Direction = (Direction)value;
                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Direction
        {
            get => m_Direction;
            set
            {
                if (m_Direction != value)
                {
                    m_Direction = value;
                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Amount
        {
            get => m_Amount;
            set
            {
                var oldValue = m_Amount;

                if (oldValue != value)
                {
                    var oldPileWeight = PileWeight;

                    m_Amount = value;

                    var newPileWeight = PileWeight;

                    UpdateTotal(this, TotalType.Weight, newPileWeight - oldPileWeight);

                    OnAmountChange(oldValue);

                    Delta(ItemDelta.Update);

                    if (oldValue > 1 || value > 1)
                    {
                        InvalidateProperties();
                    }

                    if (!Stackable && m_Amount > 1)
                    {
                        Console.WriteLine(
                            "Warning: 0x{0:X}: Amount changed for non-stackable item '{2}'. ({1})",
                            Serial.Value,
                            m_Amount,
                            GetType().Name
                        );
                    }
                }
            }
        }

        public virtual bool HandlesOnSpeech => false;

        public virtual bool BlocksFit => false;

        public bool InSecureTrade => GetSecureTradeCont() != null;

        public ItemData ItemData => TileData.ItemTable[m_ItemID & TileData.MaxItemValue];

        public virtual bool CanTarget => true;
        public virtual bool DisplayLootType => true;

        public static bool ScissorCopyLootType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool QuestItem
        {
            get => GetFlag(ImplFlag.QuestItem);
            set
            {
                SetFlag(ImplFlag.QuestItem, value);
                InvalidateProperties();
                Delta(ItemDelta.Update);
            }
        }

        public bool Insured
        {
            get => GetFlag(ImplFlag.Insured);
            set
            {
                SetFlag(ImplFlag.Insured, value);
                InvalidateProperties();
            }
        }

        public bool PaidInsurance
        {
            get => GetFlag(ImplFlag.PaidInsurance);
            set => SetFlag(ImplFlag.PaidInsurance, value);
        }

        public Mobile BlessedFor
        {
            get => LookupCompactInfo()?.m_BlessedFor;
            set
            {
                var info = AcquireCompactInfo();

                info.m_BlessedFor = value;

                if (info.m_BlessedFor == null)
                {
                    VerifyCompactInfo();
                }

                InvalidateProperties();
            }
        }

        public int CompareTo(Item other) => other == null ? -1 : Serial.CompareTo(other.Serial);

        public virtual int HuedItemID => m_ItemID;
        public ObjectPropertyList PropertyList => m_PropertyList ??= InitializePropertyList(new ObjectPropertyList(this));

        /// <summary>
        ///     Overridable. Fills an <see cref="ObjectPropertyList" /> with everything applicable. By default, this invokes
        ///     <see cref="AddNameProperties" />, then <see cref="Item.GetChildProperties">Item.GetChildProperties</see> or
        ///     <see cref="Mobile.GetChildProperties">Mobile.GetChildProperties</see>. This method should be overridden to add any
        ///     custom
        ///     properties.
        /// </summary>
        public virtual void GetProperties(ObjectPropertyList list)
        {
            AddNameProperties(list);
        }

        [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
        public DateTime Created { get; set; } = Core.Now;

        [CommandProperty(AccessLevel.GameMaster)]
        DateTime ISerializable.LastSerialized { get; set; } = Core.Now;

        long ISerializable.SavePosition { get; set; } = -1;

        BufferWriter ISerializable.SaveBuffer { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public Serial Serial { get; }

        public int TypeRef { get; private set; }

        public virtual void Serialize(IGenericWriter writer)
        {
            writer.Write(9); // version

            var flags = SaveFlag.None;

            int x = m_Location.m_X, y = m_Location.m_Y, z = m_Location.m_Z;

            if (x != 0 || y != 0 || z != 0)
            {
                if (x is >= short.MinValue and <= short.MaxValue && y is >= short.MinValue and <= short.MaxValue &&
                    z is >= sbyte.MinValue and <= sbyte.MaxValue)
                {
                    if (x != 0 || y != 0)
                    {
                        if (x is >= byte.MinValue and <= byte.MaxValue && y is >= byte.MinValue and <= byte.MaxValue)
                        {
                            flags |= SaveFlag.LocationByteXY;
                        }
                        else
                        {
                            flags |= SaveFlag.LocationShortXY;
                        }
                    }

                    if (z != 0)
                    {
                        flags |= SaveFlag.LocationSByteZ;
                    }
                }
                else
                {
                    flags |= SaveFlag.LocationFull;
                }
            }

            var info = LookupCompactInfo();
            var items = LookupItems();

            if (m_Direction != Direction.North)
            {
                flags |= SaveFlag.Direction;
            }

            if (info?.m_Bounce != null)
            {
                flags |= SaveFlag.Bounce;
            }

            if (m_LootType != LootType.Regular)
            {
                flags |= SaveFlag.LootType;
            }

            if (m_ItemID != 0)
            {
                flags |= SaveFlag.ItemID;
            }

            if (m_Hue != 0)
            {
                flags |= SaveFlag.Hue;
            }

            if (m_Amount != 1)
            {
                flags |= SaveFlag.Amount;
            }

            if (m_Layer != Layer.Invalid)
            {
                flags |= SaveFlag.Layer;
            }

            if (info?.m_Name != null)
            {
                flags |= SaveFlag.Name;
            }

            if (m_Parent != null)
            {
                flags |= SaveFlag.Parent;
            }

            if (items.Count > 0)
            {
                flags |= SaveFlag.Items;
            }

            if (m_Map != Map.Internal)
            {
                flags |= SaveFlag.Map;
            }
            // if (m_InsuredFor != null && !m_InsuredFor.Deleted)
            // flags |= SaveFlag.InsuredFor;

            if (info != null)
            {
                if (info.m_BlessedFor?.Deleted == false)
                {
                    flags |= SaveFlag.BlessedFor;
                }

                if (info.m_HeldBy?.Deleted == false)
                {
                    flags |= SaveFlag.HeldBy;
                }

                if (info.m_SavedFlags != 0)
                {
                    flags |= SaveFlag.SavedFlags;
                }
            }

            if (info == null || info.m_Weight == -1.0)
            {
                flags |= SaveFlag.NullWeight;
            }
            else
            {
                if (info.m_Weight == 0.0)
                {
                    flags |= SaveFlag.WeightIs0;
                }
                else if (info.m_Weight != 1.0)
                {
                    if (info.m_Weight == (int)info.m_Weight)
                    {
                        flags |= SaveFlag.IntWeight;
                    }
                    else
                    {
                        flags |= SaveFlag.WeightNot1or0;
                    }
                }
            }

            var implFlags = m_Flags & (ImplFlag.Visible | ImplFlag.Movable | ImplFlag.Stackable | ImplFlag.Insured |
                                       ImplFlag.PaidInsurance | ImplFlag.QuestItem);

            if (implFlags != (ImplFlag.Visible | ImplFlag.Movable))
            {
                flags |= SaveFlag.ImplFlags;
            }

            writer.Write((int)flags);

            /* begin last moved time optimization */
            var ticks = LastMoved.Ticks;
            var now = Core.Now.Ticks;

            var minutes = new TimeSpan(now - ticks).TotalMinutes;

            writer.WriteEncodedInt((int)Math.Clamp(minutes, int.MinValue, int.MaxValue));
            /* end */

            if (GetSaveFlag(flags, SaveFlag.Direction))
            {
                writer.Write((byte)m_Direction);
            }

            if (GetSaveFlag(flags, SaveFlag.Bounce))
            {
                BounceInfo.Serialize(info?.m_Bounce, writer);
            }

            if (GetSaveFlag(flags, SaveFlag.LootType))
            {
                writer.Write((byte)m_LootType);
            }

            if (GetSaveFlag(flags, SaveFlag.LocationFull))
            {
                writer.WriteEncodedInt(x);
                writer.WriteEncodedInt(y);
                writer.WriteEncodedInt(z);
            }
            else
            {
                if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                {
                    writer.Write((byte)x);
                    writer.Write((byte)y);
                }
                else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                {
                    writer.Write((short)x);
                    writer.Write((short)y);
                }

                if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                {
                    writer.Write((sbyte)z);
                }
            }

            if (GetSaveFlag(flags, SaveFlag.ItemID))
            {
                writer.WriteEncodedInt(m_ItemID);
            }

            if (GetSaveFlag(flags, SaveFlag.Hue))
            {
                writer.WriteEncodedInt(m_Hue);
            }

            if (GetSaveFlag(flags, SaveFlag.Amount))
            {
                writer.WriteEncodedInt(m_Amount);
            }

            if (GetSaveFlag(flags, SaveFlag.Layer))
            {
                writer.Write((byte)m_Layer);
            }

            if (GetSaveFlag(flags, SaveFlag.Name))
            {
                writer.Write(info.m_Name);
            }

            if (GetSaveFlag(flags, SaveFlag.Parent))
            {
                writer.Write(m_Parent);
            }

            if (GetSaveFlag(flags, SaveFlag.Items))
            {
                writer.Write(items);
            }

            if (GetSaveFlag(flags, SaveFlag.IntWeight))
            {
                writer.WriteEncodedInt((int)info.m_Weight);
            }
            else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
            {
                writer.Write(info.m_Weight);
            }

            if (GetSaveFlag(flags, SaveFlag.Map))
            {
                writer.Write(m_Map);
            }

            if (GetSaveFlag(flags, SaveFlag.ImplFlags))
            {
                writer.WriteEncodedInt((int)implFlags);
            }

            if (GetSaveFlag(flags, SaveFlag.InsuredFor))
            {
                writer.Write((Mobile)null);
            }

            if (GetSaveFlag(flags, SaveFlag.BlessedFor))
            {
                writer.Write(info.m_BlessedFor);
            }

            if (GetSaveFlag(flags, SaveFlag.HeldBy))
            {
                writer.Write(info.m_HeldBy);
            }

            if (GetSaveFlag(flags, SaveFlag.SavedFlags))
            {
                writer.WriteEncodedInt(info.m_SavedFlags);
            }
        }

        public void MoveToWorld(WorldLocation worldLocation)
        {
            MoveToWorld(worldLocation.Location, worldLocation.Map);
        }

        /// <summary>
        ///     Moves the Item to a given <paramref name="location" /> and <paramref name="map" />.
        /// </summary>
        public void MoveToWorld(Point3D location, Map map)
        {
            if (Deleted)
            {
                return;
            }

            var oldLocation = GetWorldLocation();
            var oldRealLocation = m_Location;

            SetLastMoved();

            if (Parent is Mobile mobile)
            {
                mobile.RemoveItem(this);
            }
            else if (Parent is Item item)
            {
                item.RemoveItem(this);
            }

            if (m_Map != map)
            {
                var old = m_Map;

                if (m_Map != null)
                {
                    m_Map.OnLeave(this);

                    if (oldLocation.m_X != 0)
                    {
                        SendRemovePacket(oldLocation);
                    }
                }

                m_Location = location;
                OnLocationChange(oldRealLocation);

                var items = LookupItems();

                for (var i = 0; i < items.Count; ++i)
                {
                    items[i].Map = map;
                }

                m_Map = map;
                m_Map?.OnEnter(this);

                OnMapChange();

                if (m_Map != null)
                {
                    Span<byte> oldWorldItem = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength].InitializePacket();
                    Span<byte> saWorldItem = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength].InitializePacket();
                    Span<byte> hsWorldItem = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength].InitializePacket();
                    Span<byte> opl = ObjectPropertyList.Enabled ? stackalloc byte[OutgoingEntityPackets.OPLPacketLength].InitializePacket() : null;

                    var eable = m_Map.GetClientsInRange(m_Location, GetMaxUpdateRange());

                    foreach (var state in eable)
                    {
                        var m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(m_Location, GetUpdateRange(m)))
                        {
                            if (state.HighSeas)
                            {
                                var length = OutgoingEntityPackets.CreateWorldEntity(hsWorldItem, this, true);
                                if (length != hsWorldItem.Length)
                                {
                                    hsWorldItem = hsWorldItem[..length];
                                }

                                SendInfoTo(state, hsWorldItem, opl);
                            }
                            else if (state.StygianAbyss)
                            {
                                var length = OutgoingEntityPackets.CreateWorldEntity(saWorldItem, this, false);
                                if (length != saWorldItem.Length)
                                {
                                    saWorldItem = saWorldItem[..length];
                                }

                                SendInfoTo(state, saWorldItem, opl);
                            }
                            else
                            {
                                var length = OutgoingItemPackets.CreateWorldItem(oldWorldItem, this);
                                if (length != oldWorldItem.Length)
                                {
                                    oldWorldItem = oldWorldItem[..length];
                                }

                                SendInfoTo(state, oldWorldItem, opl);
                            }
                        }
                    }

                    eable.Free();
                }

                RemDelta(ItemDelta.Update);

                if (old == null || old == Map.Internal)
                {
                    InvalidateProperties();
                }
            }
            else if (m_Map != null)
            {
                IPooledEnumerable<NetState> eable;

                if (oldLocation.m_X != 0)
                {
                    eable = m_Map.GetClientsInRange(oldLocation, GetMaxUpdateRange());

                    Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

                    foreach (var state in eable)
                    {
                        var m = state.Mobile;

                        if (!m.InRange(location, GetUpdateRange(m)))
                        {
                            OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                            state.Send(removeEntity);
                        }
                    }

                    eable.Free();
                }

                var oldInternalLocation = m_Location;

                m_Location = location;
                OnLocationChange(oldRealLocation);

                eable = m_Map.GetClientsInRange(m_Location, GetMaxUpdateRange());

                foreach (var state in eable)
                {
                    var m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(m_Location, GetUpdateRange(m)))
                    {
                        SendInfoTo(state);
                    }
                }

                eable.Free();

                m_Map.OnMove(oldInternalLocation, this);

                RemDelta(ItemDelta.Update);
            }
            else
            {
                Map = map;
                Location = location;
            }
        }

        /// <summary>
        ///     Has the item been deleted?
        /// </summary>
        public bool Deleted => GetFlag(ImplFlag.Deleted);

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Map Map
        {
            get => m_Map;
            set
            {
                if (m_Map != value)
                {
                    var old = m_Map;

                    if (m_Map != null && m_Parent == null)
                    {
                        m_Map.OnLeave(this);
                        SendRemovePacket();
                    }

                    var items = LookupItems();

                    for (var i = 0; i < items.Count; ++i)
                    {
                        items[i].Map = value;
                    }

                    m_Map = value;

                    if (m_Parent == null)
                    {
                        m_Map?.OnEnter(this);
                    }

                    Delta(ItemDelta.Update);

                    OnMapChange();

                    if (old == null || old == Map.Internal)
                    {
                        InvalidateProperties();
                    }
                }
            }
        }

        public virtual void ProcessDelta()
        {
            var flags = m_DeltaFlags;

            SetFlag(ImplFlag.InQueue, false);
            m_DeltaFlags = ItemDelta.None;

            var map = m_Map;

            if (map == null || Deleted)
            {
                return;
            }

            var worldLoc = GetWorldLocation();
            var update = (flags & ItemDelta.Update) != 0;

            if (update && m_Parent is Container { IsPublicContainer: false } contParent)
            {
                var rootParent = contParent.RootParent as Mobile;
                Mobile tradeRecip = null;

                if (rootParent != null)
                {
                    var ns = rootParent.NetState;

                    if (ns != null && rootParent.CanSee(this) && rootParent.InRange(worldLoc, GetUpdateRange(rootParent)))
                    {
                        ns.SendContainerContentUpdate(this);
                        SendOPLPacketTo(ns);
                    }
                }

                var st = GetSecureTradeCont()?.Trade;

                if (st != null)
                {
                    var test = st.From.Mobile;

                    if (test != null && test != rootParent)
                    {
                        tradeRecip = test;
                    }

                    test = st.To.Mobile;

                    if (test != null && test != rootParent)
                    {
                        tradeRecip = test;
                    }

                    var ns = tradeRecip?.NetState;

                    if (ns != null && tradeRecip.CanSee(this) && tradeRecip.InRange(worldLoc, GetUpdateRange(tradeRecip)))
                    {
                        ns.SendContainerContentUpdate(this);
                        SendOPLPacketTo(ns);
                    }
                }

                var openers = contParent.Openers;

                if (openers != null)
                {
                    for (var i = 0; i < openers.Count; ++i)
                    {
                        var mob = openers[i];

                        var range = GetUpdateRange(mob);

                        if (mob.Map != map || !mob.InRange(worldLoc, range))
                        {
                            openers.RemoveAt(i--);
                        }
                        else
                        {
                            if (mob == rootParent || mob == tradeRecip)
                            {
                                continue;
                            }

                            var ns = mob.NetState;

                            if (ns != null && mob.CanSee(this))
                            {
                                ns.SendContainerContentUpdate(this);
                                SendOPLPacketTo(ns);
                            }
                        }
                    }

                    if (openers.Count == 0)
                    {
                        contParent.Openers = null;
                    }
                }

                return;
            }

            var eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

            foreach (var state in eable)
            {
                var m = state.Mobile;

                if (!m.CanSee(this) || !m.InRange(worldLoc, GetUpdateRange(m)))
                {
                    continue;
                }

                if (update)
                {
                    if (m_Parent == null)
                    {
                        SendInfoTo(state);
                    }
                    else
                    {
                        if (m_Parent is Item)
                        {
                            // TODO: Optimize by writing once?
                            state.SendContainerContentUpdate(this);
                        }
                        else if (m_Parent is Mobile)
                        {
                            // TODO: Optimize by writing once?
                            state.SendEquipUpdate(this);
                        }

                        SendOPLPacketTo(state);
                    }
                }
                else if ((flags & ItemDelta.EquipOnly) != 0 && m_Parent is Mobile)
                {
                    state.SendEquipUpdate(this);
                    SendOPLPacketTo(state);
                }
                else if (ObjectPropertyList.Enabled && (flags & ItemDelta.Properties) != 0)
                {
                    SendOPLPacketTo(state);
                }
            }

            eable.Free();
        }

        public virtual void Delete()
        {
            if (Deleted)
            {
                return;
            }

            OnDelete();

            var items = LookupItems();

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i < items.Count)
                {
                    items[i].OnParentDeleted(this);
                }
            }

            SendRemovePacket();

            SetFlag(ImplFlag.Deleted, true);

            if (Parent is Mobile mobile)
            {
                mobile.RemoveItem(this);
            }
            else if (Parent is Item item)
            {
                item.RemoveItem(this);
            }

            ClearBounce();

            if (m_Map != null)
            {
                if (m_Parent == null)
                {
                    m_Map.OnLeave(this);
                }

                m_Map = null;
            }

            World.RemoveEntity(this);

            OnAfterDelete();

            m_PropertyList = null;
        }

        public ISpawner Spawner
        {
            get => LookupCompactInfo()?.m_Spawner;
            set
            {
                var info = AcquireCompactInfo();

                info.m_Spawner = value;

                if (info.m_Spawner == null)
                {
                    VerifyCompactInfo();
                }
            }
        }

        public virtual void OnBeforeSpawn(Point3D location, Map m)
        {
        }

        public virtual void OnAfterSpawn()
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual Point3D Location
        {
            get => m_Location;
            set
            {
                var oldLocation = m_Location;

                if (oldLocation == value)
                {
                    return;
                }

                if (m_Map != null)
                {
                    if (m_Parent == null)
                    {
                        IPooledEnumerable<NetState> eable;

                        if (m_Location.m_X != 0)
                        {
                            eable = m_Map.GetClientsInRange(oldLocation, GetMaxUpdateRange());

                            Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

                            foreach (var state in eable)
                            {
                                var m = state.Mobile;

                                if (!m.InRange(value, GetUpdateRange(m)))
                                {
                                    OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                                    state.Send(removeEntity);
                                }
                            }

                            eable.Free();
                        }

                        var oldLoc = m_Location;
                        m_Location = value;

                        SetLastMoved();

                        eable = m_Map.GetClientsInRange(m_Location, GetMaxUpdateRange());

                        foreach (var state in eable)
                        {
                            var m = state.Mobile;

                            if (m.CanSee(this) && m.InRange(m_Location, GetUpdateRange(m)) &&
                                (!state.HighSeas || !NoMoveHS || (m_DeltaFlags & ItemDelta.Update) != 0 ||
                                 !m.InRange(oldLoc, GetUpdateRange(m))))
                            {
                                SendInfoTo(state);
                            }
                        }

                        eable.Free();

                        RemDelta(ItemDelta.Update);
                    }
                    else
                    {
                        m_Location = value;
                        if (m_Parent is Item)
                        {
                            Delta(ItemDelta.Update);
                        }
                    }

                    if (m_Parent == null)
                    {
                        m_Map.OnMove(oldLocation, this);
                    }
                }
                else
                {
                    m_Location = value;
                }

                OnLocationChange(oldLocation);
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int X
        {
            get => Location.m_X;
            set => Location = new Point3D(value, m_Location.m_Y, m_Location.m_Z);
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Y
        {
            get => Location.m_Y;
            set => Location = new Point3D(m_Location.m_X, value, m_Location.m_Z);
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Z
        {
            get => Location.m_Z;
            set => Location = new Point3D(m_Location.m_X, m_Location.m_Y, value);
        }

        public virtual bool InRange(Point2D p, int range) => Utility.InRange(p.X, p.Y, X, Y, range);

        public virtual bool InRange(Point3D p, int range) => Utility.InRange(p.X, p.Y, X, Y, range);

        public ExpandFlag GetExpandFlags()
        {
            var info = LookupCompactInfo();

            ExpandFlag flags = 0;

            if (info != null)
            {
                if (info.m_BlessedFor != null)
                {
                    flags |= ExpandFlag.Blessed;
                }

                if (info.m_Bounce != null)
                {
                    flags |= ExpandFlag.Bounce;
                }

                if (info.m_HeldBy != null)
                {
                    flags |= ExpandFlag.Holder;
                }

                if (info.m_Items != null)
                {
                    flags |= ExpandFlag.Items;
                }

                if (info.m_Name != null)
                {
                    flags |= ExpandFlag.Name;
                }

                if (info.m_Spawner != null)
                {
                    flags |= ExpandFlag.Spawner;
                }

                if (info.m_SavedFlags != 0)
                {
                    flags |= ExpandFlag.SaveFlag;
                }

                if (info.m_TempFlags != 0)
                {
                    flags |= ExpandFlag.TempFlag;
                }

                if (info.m_Weight != -1)
                {
                    flags |= ExpandFlag.Weight;
                }
            }

            return flags;
        }

        private CompactInfo LookupCompactInfo() => m_CompactInfo;

        private CompactInfo AcquireCompactInfo() => m_CompactInfo ??= new CompactInfo();

        private void ReleaseCompactInfo()
        {
            m_CompactInfo = null;
        }

        private void VerifyCompactInfo()
        {
            var info = m_CompactInfo;

            if (info == null)
            {
                return;
            }

            var isValid = info.m_Name != null
                          || info.m_Items != null
                          || info.m_Bounce != null
                          || info.m_HeldBy != null
                          || info.m_BlessedFor != null
                          || info.m_Spawner != null
                          || info.m_TempFlags != 0
                          || info.m_SavedFlags != 0
                          || info.m_Weight != -1;

            if (!isValid)
            {
                ReleaseCompactInfo();
            }
        }

        public List<Item> LookupItems() => (this is Container container ? container.m_Items : LookupCompactInfo()?.m_Items) ?? EmptyItems;

        public List<Item> AcquireItems()
        {
            if (this is Container cont)
            {
                return cont.m_Items ?? (cont.m_Items = new List<Item>());
            }

            var info = AcquireCompactInfo();
            return info.m_Items ?? (info.m_Items = new List<Item>());
        }

        private void SetFlag(ImplFlag flag, bool value)
        {
            if (value)
            {
                m_Flags |= flag;
            }
            else
            {
                m_Flags &= ~flag;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetFlag(ImplFlag flag) => (m_Flags & flag) != 0;

        public BounceInfo GetBounce() => LookupCompactInfo()?.m_Bounce;

        public void RecordBounce()
        {
            AcquireCompactInfo().m_Bounce = new BounceInfo(this);
        }

        public void ClearBounce()
        {
            var info = LookupCompactInfo();

            var bounce = info?.m_Bounce;

            if (bounce == null)
            {
                return;
            }

            info.m_Bounce = null;

            if (bounce.Parent is Item parentItem)
            {
                if (!parentItem.Deleted)
                {
                    parentItem.OnItemBounceCleared(this);
                }
            }
            else if (bounce.Parent is Mobile { Deleted: false } parentMobile)
            {
                parentMobile.OnItemBounceCleared(this);
            }

            VerifyCompactInfo();
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when a client, <paramref name="from" />, invokes a 'help request' for the Item.
        ///     Seemingly no longer functional in newer clients.
        /// </summary>
        public virtual void OnHelpRequest(Mobile from)
        {
        }

        /// <summary>
        ///     Overridable. Method checked to see if the item can be traded.
        /// </summary>
        /// <returns>True if the trade is allowed, false if not.</returns>
        public virtual bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when a trade has completed, either successfully or not.
        /// </summary>
        public virtual void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
        }

        /// <summary>
        ///     Overridable. Method checked to see if the elemental resistances of this Item conflict with another Item on the
        ///     <see cref="Mobile" />.
        /// </summary>
        /// <returns>
        ///     <list type="table">
        ///         <item>
        ///             <term>True</term>
        ///             <description>
        ///                 There is a conflict. The elemental resistance bonuses of this Item should not be applied to the
        ///                 <see cref="Mobile" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>False</term>
        ///             <description>There is no conflict. The bonuses should be applied.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public virtual bool CheckPropertyConflict(Mobile m) => false;

        /// <summary>
        ///     Overridable. Sends the <see cref="PropertyList">object property list</see> to <paramref name="from" />.
        /// </summary>
        public virtual void SendPropertiesTo(Mobile from)
        {
            from.NetState?.Send(PropertyList.Buffer);
        }

        /// <summary>
        ///     Overridable. Adds the name of this item to the given <see cref="ObjectPropertyList" />. This method should be overridden
        ///     if the item requires a complex naming format.
        /// </summary>
        public virtual void AddNameProperty(ObjectPropertyList list)
        {
            var name = Name;

            if (name == null)
            {
                if (m_Amount <= 1)
                {
                    list.Add(LabelNumber);
                }
                else
                {
                    list.Add(1050039, "{0}\t#{1}", m_Amount, LabelNumber); // ~1_NUMBER~ ~2_ITEMNAME~
                }
            }
            else
            {
                if (m_Amount <= 1)
                {
                    list.Add(name);
                }
                else
                {
                    list.Add(1050039, "{0}\t{1}", m_Amount, Name); // ~1_NUMBER~ ~2_ITEMNAME~
                }
            }
        }

        /// <summary>
        ///     Overridable. Adds the loot type of this item to the given <see cref="ObjectPropertyList" />. By default, this will be
        ///     either 'blessed', 'cursed', or 'insured'.
        /// </summary>
        public virtual void AddLootTypeProperty(ObjectPropertyList list)
        {
            if (m_LootType == LootType.Blessed)
            {
                list.Add(1038021); // blessed
            }
            else if (m_LootType == LootType.Cursed)
            {
                list.Add(1049643); // cursed
            }
            else if (Insured)
            {
                list.Add(1061682); // <b>insured</b>
            }
        }

        /// <summary>
        ///     Overridable. Adds any elemental resistances of this item to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddResistanceProperties(ObjectPropertyList list)
        {
            var v = PhysicalResistance;

            if (v != 0)
            {
                list.Add(1060448, v.ToString()); // physical resist ~1_val~%
            }

            v = FireResistance;

            if (v != 0)
            {
                list.Add(1060447, v.ToString()); // fire resist ~1_val~%
            }

            v = ColdResistance;

            if (v != 0)
            {
                list.Add(1060445, v.ToString()); // cold resist ~1_val~%
            }

            v = PoisonResistance;

            if (v != 0)
            {
                list.Add(1060449, v.ToString()); // poison resist ~1_val~%
            }

            v = EnergyResistance;

            if (v != 0)
            {
                list.Add(1060446, v.ToString()); // energy resist ~1_val~%
            }
        }

        /// <summary>
        ///     Overridable. Displays cliloc 1072788-1072789.
        /// </summary>
        public virtual void AddWeightProperty(ObjectPropertyList list)
        {
            var weight = PileWeight + TotalWeight;

            if (weight == 1)
            {
                list.Add(1072788, weight.ToString()); // Weight: ~1_WEIGHT~ stone
            }
            else
            {
                list.Add(1072789, weight.ToString()); // Weight: ~1_WEIGHT~ stones
            }
        }

        /// <summary>
        ///     Overridable. Adds header properties. By default, this invokes <see cref="AddNameProperty" />,
        ///     <see cref="AddBlessedForProperty" /> (if applicable), and <see cref="AddLootTypeProperty" /> (if
        ///     <see cref="DisplayLootType" />).
        /// </summary>
        public virtual void AddNameProperties(ObjectPropertyList list)
        {
            AddNameProperty(list);

            if (IsSecure)
            {
                AddSecureProperty(list);
            }
            else if (IsLockedDown)
            {
                AddLockedDownProperty(list);
            }

            var blessedFor = BlessedFor;

            if (blessedFor?.Deleted == false)
            {
                AddBlessedForProperty(list, blessedFor);
            }

            if (DisplayLootType)
            {
                AddLootTypeProperty(list);
            }

            if (DisplayWeight)
            {
                AddWeightProperty(list);
            }

            if (QuestItem)
            {
                AddQuestItemProperty(list);
            }

            AppendChildNameProperties(list);
        }

        /// <summary>
        ///     Overridable. Adds the "Quest Item" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddQuestItemProperty(ObjectPropertyList list)
        {
            list.Add(1072351); // Quest Item
        }

        /// <summary>
        ///     Overridable. Adds the "Locked Down & Secure" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddSecureProperty(ObjectPropertyList list)
        {
            list.Add(501644); // locked down & secure
        }

        /// <summary>
        ///     Overridable. Adds the "Locked Down" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddLockedDownProperty(ObjectPropertyList list)
        {
            list.Add(501643); // locked down
        }

        /// <summary>
        ///     Overridable. Adds the "Blessed for ~1_NAME~" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddBlessedForProperty(ObjectPropertyList list, Mobile m)
        {
            list.Add(1062203, "{0}", m.Name); // Blessed for ~1_NAME~
        }

        /// <summary>
        ///     Overridable. Event invoked when a child (<paramref name="item" />) is building it's <see cref="ObjectPropertyList" />.
        ///     Recursively calls <see cref="Item.GetChildProperties">Item.GetChildProperties</see> or
        ///     <see cref="Mobile.GetChildProperties">Mobile.GetChildProperties</see>.
        /// </summary>
        public virtual void GetChildProperties(ObjectPropertyList list, Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.GetChildProperties(list, item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.GetChildProperties(list, item);
            }
        }

        /// <summary>
        ///     Overridable. Event invoked when a child (<paramref name="item" />) is building it's Name
        ///     <see cref="ObjectPropertyList" />
        ///     . Recursively calls <see cref="Item.GetChildProperties">Item.GetChildNameProperties</see> or
        ///     <see cref="Mobile.GetChildProperties">Mobile.GetChildNameProperties</see>.
        /// </summary>
        public virtual void GetChildNameProperties(ObjectPropertyList list, Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.GetChildNameProperties(list, item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.GetChildNameProperties(list, item);
            }
        }

        public virtual bool IsChildVisibleTo(Mobile m, Item child) => true;

        public void Bounce(Mobile from)
        {
            m_Parent?.RemoveItem(this);
            m_Parent = null;

            var bounce = GetBounce();

            if (bounce == null)
            {
                MoveToWorld(from.Location, from.Map);
                return;
            }

            var parent = bounce.Parent;

            if (parent?.Deleted != false)
            {
                MoveToWorld(from.Location, from.Map);
                return;
            }

            if (parent is Item ip)
            {
                var root = ip.RootParent;
                var rpm = root as Mobile;

                if (ip.IsAccessibleTo(from) &&
                    rpm?.CheckNonlocalDrop(from, this, ip) == true &&
                    (!ip.Movable || rpm == from || ip.Map == bounce.Map && root.Location == bounce.WorldLoc)
                )
                {
                    Location = bounce.Location;
                    ip.AddItem(this);
                }
                else
                {
                    MoveToWorld(from.Location, from.Map);
                }
            }
            else if (parent is Mobile mobile)
            {
                if (!mobile.EquipItem(this))
                {
                    MoveToWorld(bounce.WorldLoc, bounce.Map);
                }
            }
            else
            {
                MoveToWorld(from.Location, from.Map);
            }

            ClearBounce();
        }

        /// <summary>
        ///     Overridable. Method checked to see if this item may be equipped while casting a spell. By default, this returns false.
        ///     It
        ///     is overridden on spellbook and spell channeling weapons or shields.
        /// </summary>
        /// <returns>True if it may, false if not.</returns>
        /// <example>
        ///     <code>
        ///   public override bool AllowEquippedCast( Mobile from )
        ///   {
        ///     if (from.Int &gt;= 100)
        ///       return true;
        ///
        ///     return base.AllowEquippedCast( from );
        ///   }</code>
        ///     When placed in an Item script, the item may be cast when equipped if the <paramref name="from" /> has 100 or more
        ///     intelligence. Otherwise, it will drop to their backpack.
        /// </example>
        public virtual bool AllowEquippedCast(Mobile from) => false;

        public virtual bool CheckConflictingLayer(Mobile m, Item item, Layer layer) => m_Layer == layer;

        // Uses Race.RaceFlag
        public virtual int RequiredRaces => Race.AllowAllRaces;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckRace(Race race) => Race.IsAllowedRace(race, RequiredRaces);

        public virtual bool CheckRace(Mobile from, bool message = true)
        {
            var race = from.Race;
            var requiredRaces = RequiredRaces;

            if (Race.IsAllowedRace(race, requiredRaces))
            {
                return true;
            }

            if (!message)
            {
                return false;
            }

            if (requiredRaces == Race.AllowGargoylesOnly)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1111707); // Only gargoyles can wear this.
            }
            else if (race == Race.Gargoyle)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1111708); // Gargoyles can't wear this.
            }
            else if (requiredRaces == Race.AllowElvesOnly)
            {
                from.SendLocalizedMessage(1072203); // Only Elves may use this.
            }
            else
            {
                from.SendMessage($"{race.PluralName} may not use this.");
            }

            return false;
        }

        public virtual bool CanEquip(Mobile m) => m_Layer != Layer.Invalid && m.FindItemOnLayer(m_Layer) == null;

        public virtual void GetChildContextMenuEntries(Mobile from, List<ContextMenuEntry> list, Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.GetChildContextMenuEntries(from, list, item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.GetChildContextMenuEntries(from, list, item);
            }
        }

        public virtual void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (m_Parent is Item item)
            {
                item.GetChildContextMenuEntries(from, list, this);
            }
            else if (m_Parent is Mobile mobile)
            {
                mobile.GetChildContextMenuEntries(from, list, this);
            }
        }

        public virtual bool VerifyMove(Mobile from) => Movable;

        public virtual DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (!Movable)
            {
                return DeathMoveResult.RemainEquipped;
            }

            if (parent.KeepsItemsOnDeath)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (CheckBlessed(parent))
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (CheckNewbied() && parent.Kills < 5)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (parent.Player && Nontransferable)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            return DeathMoveResult.MoveToCorpse;
        }

        public virtual DeathMoveResult OnInventoryDeath(Mobile parent)
        {
            if (!Movable)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (parent.KeepsItemsOnDeath)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (CheckBlessed(parent))
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (CheckNewbied() && parent.Kills < 5)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            if (parent.Player && Nontransferable)
            {
                return DeathMoveResult.MoveToBackpack;
            }

            return DeathMoveResult.MoveToCorpse;
        }

        /// <summary>
        ///     Moves the Item to <paramref name="location" />. The Item does not change maps.
        /// </summary>
        public virtual void MoveToWorld(Point3D location)
        {
            MoveToWorld(location, m_Map);
        }

        public void LabelTo(Mobile to, int number, string args = "")
        {
            to.NetState.SendMessageLocalized(Serial, m_ItemID, MessageType.Label, 0x3B2, 3, number, "", args);
        }

        public void LabelTo(Mobile to, string text)
        {
            to.NetState.SendMessage(Serial, m_ItemID, MessageType.Label, 0x3B2, 3, false, "ENU", "", text);
        }

        public void LabelTo(Mobile to, string format, params object[] args)
        {
            LabelTo(to, string.Format(format, args));
        }

        public void LabelToAffix(Mobile to, int number, AffixType type, string affix, string args = "")
        {
            to.NetState.SendMessageLocalizedAffix(Serial, m_ItemID, MessageType.Label, 0x3B2, 3, number, "", type, affix, args);
        }

        public virtual void LabelLootTypeTo(Mobile to)
        {
            if (m_LootType == LootType.Blessed)
            {
                LabelTo(to, 1041362); // (blessed)
            }
            else if (m_LootType == LootType.Cursed)
            {
                LabelTo(to, "(cursed)");
            }
        }

        public bool AtWorldPoint(int x, int y) => m_Parent == null && m_Location.m_X == x && m_Location.m_Y == y;

        public bool AtPoint(int x, int y) => m_Location.m_X == x && m_Location.m_Y == y;

        public virtual bool CanDecay() =>
            Decays && Parent == null && Map != Map.Internal;


        public virtual bool OnDecay() =>
            CanDecay() && Region.Find(Location, Map).OnDecay(this);

        public void SetLastMoved()
        {
            LastMoved = Core.Now;
        }

        public virtual bool CanStackWith(Item dropped) =>
            dropped.Stackable && Stackable && dropped.GetType() == GetType() && dropped.ItemID == ItemID &&
            dropped.Hue == Hue && dropped.Name == Name && dropped.Amount + Amount <= 60000 && dropped != this;

        public bool StackWith(Mobile from, Item dropped) => StackWith(from, dropped, true);

        public virtual bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (CanStackWith(dropped))
            {
                if (m_LootType != dropped.m_LootType)
                {
                    m_LootType = LootType.Regular;
                }

                Amount += dropped.Amount;
                dropped.Delete();

                if (playSound && from != null)
                {
                    var soundID = GetDropSound();

                    if (soundID == -1)
                    {
                        soundID = 0x42;
                    }

                    from.SendSound(soundID, GetWorldLocation());
                }

                return true;
            }

            return false;
        }

        public virtual bool OnDragDrop(Mobile from, Item dropped)
        {
            var success = Parent is Container container && container.OnStackAttempt(from, this, dropped) ||
                          StackWith(from, dropped);

            if (success && Spawner != null)
            {
                Spawner.Remove(this);
                Spawner = null;
            }

            return success;
        }

        public Rectangle2D GetGraphicBounds()
        {
            var itemID = m_ItemID;
            var doubled = m_Amount > 1;

            if (itemID is >= 0xEEA and <= 0xEF2) // Are we coins?
            {
                var coinBase = (itemID - 0xEEA) / 3;
                coinBase *= 3;
                coinBase += 0xEEA;

                doubled = false;

                itemID = m_Amount switch
                {
                    <= 1 => coinBase,
                    <= 5 => coinBase + 1,
                    _    => coinBase + 2
                };
            }

            var bounds = ItemBounds.Table[itemID & 0x3FFF];

            if (doubled)
            {
                bounds.Set(bounds.X, bounds.Y, bounds.Width + 5, bounds.Height + 5);
            }

            return bounds;
        }

        public virtual void AppendChildProperties(ObjectPropertyList list)
        {
            if (m_Parent is Item item)
            {
                item.GetChildProperties(list, this);
            }
            else if (m_Parent is Mobile mobile)
            {
                mobile.GetChildProperties(list, this);
            }
        }

        public virtual void AppendChildNameProperties(ObjectPropertyList list)
        {
            if (m_Parent is Item item)
            {
                item.GetChildNameProperties(list, this);
            }
            else if (m_Parent is Mobile mobile)
            {
                mobile.GetChildNameProperties(list, this);
            }
        }

        private ObjectPropertyList InitializePropertyList(ObjectPropertyList list)
        {
            GetProperties(list);
            AppendChildProperties(list);
            list.Terminate();
            return list;
        }

        public void ClearProperties()
        {
            m_PropertyList = null;
        }

#nullable enable
        public virtual void InvalidateProperties()
        {
            if (!ObjectPropertyList.Enabled)
            {
                return;
            }

            if (m_Map != null && m_Map != Map.Internal && !World.Loading)
            {
                int? oldHash;
                int newHash;
                if (m_PropertyList != null)
                {
                    oldHash = m_PropertyList.Hash;
                    m_PropertyList.Reset();
                    InitializePropertyList(m_PropertyList);
                    newHash = m_PropertyList.Hash;
                }
                else
                {
                    oldHash = null;
                    newHash = PropertyList.Hash;
                }

                if (oldHash != newHash)
                {
                    Delta(ItemDelta.Properties);
                }
            }
            else
            {
                ClearProperties();
            }
        }
#nullable restore

        public virtual int GetPacketFlags()
        {
            var flags = 0;

            if (!Visible)
            {
                flags |= 0x80;
            }

            if (Movable || ForceShowProperties)
            {
                flags |= 0x20;
            }

            return flags;
        }

        public virtual bool OnMoveOff(Mobile m) => true;

        public virtual bool OnMoveOver(Mobile m) => true;

        public virtual void OnMovement(Mobile m, Point3D oldLocation)
        {
        }

        public void Internalize()
        {
            MoveToWorld(Point3D.Zero, Map.Internal);
        }

        public virtual void OnMapChange()
        {
        }

        public virtual void OnRemoved(IEntity parent)
        {
        }

        public virtual void OnAdded(IEntity parent)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
            {
                flags |= toSet;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

        public IPooledEnumerable<IEntity> GetObjectsInRange(int range)
        {
            var map = m_Map;

            return map == null
                ? Map.NullEnumerable<IEntity>.Instance
                : map.GetObjectsInRange(m_Parent == null ? m_Location : GetWorldLocation(), range);
        }

        public IPooledEnumerable<Item> GetItemsInRange(int range)
        {
            var map = m_Map;

            return map?.GetItemsInRange(m_Parent == null ? m_Location : GetWorldLocation(), range)
                   ?? Map.NullEnumerable<Item>.Instance;
        }

        public IPooledEnumerable<Mobile> GetMobilesInRange(int range)
        {
            var map = m_Map;

            return map?.GetMobilesInRange(m_Parent == null ? m_Location : GetWorldLocation(), range)
                   ?? Map.NullEnumerable<Mobile>.Instance;
        }

        public IPooledEnumerable<NetState> GetClientsInRange(int range)
        {
            var map = m_Map;

            return map.GetClientsInRange(m_Parent == null ? m_Location : GetWorldLocation(), range)
                   ?? Map.NullEnumerable<NetState>.Instance;
        }

        public bool GetTempFlag(int flag) => ((LookupCompactInfo()?.m_TempFlags ?? 0) & flag) != 0;

        public void SetTempFlag(int flag, bool value)
        {
            var info = AcquireCompactInfo();

            if (value)
            {
                info.m_TempFlags |= flag;
            }
            else
            {
                info.m_TempFlags &= ~flag;
            }

            if (info.m_TempFlags == 0)
            {
                VerifyCompactInfo();
            }
        }

        public bool GetSavedFlag(int flag) => ((LookupCompactInfo()?.m_SavedFlags ?? 0) & flag) != 0;

        public void SetSavedFlag(int flag, bool value)
        {
            var info = AcquireCompactInfo();

            if (value)
            {
                info.m_SavedFlags |= flag;
            }
            else
            {
                info.m_SavedFlags &= ~flag;
            }

            if (info.m_SavedFlags == 0)
            {
                VerifyCompactInfo();
            }
        }

        public virtual void BeforeSerialize()
        {
        }

        public virtual void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadInt();

            SetLastMoved();

            switch (version)
            {
                case 9:
                case 8:
                case 7:
                case 6:
                    {
                        var flags = (SaveFlag)reader.ReadInt();

                        if (version < 7)
                        {
                            LastMoved = reader.ReadDeltaTime();
                        }
                        else
                        {
                            var minutes = reader.ReadEncodedInt();

                            try
                            {
                                LastMoved = Core.Now - TimeSpan.FromMinutes(minutes);
                            }
                            catch
                            {
                                LastMoved = Core.Now;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                        {
                            m_Direction = (Direction)reader.ReadByte();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                        {
                            AcquireCompactInfo().m_Bounce = BounceInfo.Deserialize(reader);
                        }

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                        {
                            m_LootType = (LootType)reader.ReadByte();
                        }

                        int x = 0, y = 0, z = 0;

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                        {
                            x = reader.ReadEncodedInt();
                            y = reader.ReadEncodedInt();
                            z = reader.ReadEncodedInt();
                        }
                        else
                        {
                            if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                            {
                                x = reader.ReadByte();
                                y = reader.ReadByte();
                            }
                            else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                            {
                                x = reader.ReadShort();
                                y = reader.ReadShort();
                            }

                            if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                            {
                                z = reader.ReadSByte();
                            }
                        }

                        m_Location = new Point3D(x, y, z);

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                        {
                            m_ItemID = reader.ReadEncodedInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                        {
                            m_Hue = reader.ReadEncodedInt();
                        }

                        m_Amount = GetSaveFlag(flags, SaveFlag.Amount) ? reader.ReadEncodedInt() : 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                        {
                            m_Layer = (Layer)reader.ReadByte();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Name))
                        {
                            var name = reader.ReadString();

                            if (name != DefaultName)
                            {
                                AcquireCompactInfo().m_Name = name;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadSerial();

                            if (parent.IsMobile)
                            {
                                m_Parent = World.FindMobile(parent);
                            }
                            else if (parent.IsItem)
                            {
                                m_Parent = World.FindItem(parent);
                            }
                            else
                            {
                                m_Parent = null;
                            }

                            if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                            {
                                Delete();
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                        {
                            var items = reader.ReadEntityList<Item>();

                            if (this is Container)
                            {
                                (this as Container).m_Items = items;
                            }
                            else
                            {
                                AcquireCompactInfo().m_Items = items;
                            }
                        }

                        if (version < 8 || !GetSaveFlag(flags, SaveFlag.NullWeight))
                        {
                            double weight;

                            if (GetSaveFlag(flags, SaveFlag.IntWeight))
                            {
                                weight = reader.ReadEncodedInt();
                            }
                            else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                            {
                                weight = reader.ReadDouble();
                            }
                            else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                            {
                                weight = 0.0;
                            }
                            else
                            {
                                weight = 1.0;
                            }

                            if (weight != DefaultWeight)
                            {
                                AcquireCompactInfo().m_Weight = weight;
                            }
                        }

                        m_Map = GetSaveFlag(flags, SaveFlag.Map) ? reader.ReadMap() : Map.Internal;

                        SetFlag(ImplFlag.Visible, !GetSaveFlag(flags, SaveFlag.Visible) || reader.ReadBool());

                        SetFlag(ImplFlag.Movable, !GetSaveFlag(flags, SaveFlag.Movable) || reader.ReadBool());

                        if (GetSaveFlag(flags, SaveFlag.Stackable))
                        {
                            SetFlag(ImplFlag.Stackable, reader.ReadBool());
                        }

                        if (GetSaveFlag(flags, SaveFlag.ImplFlags))
                        {
                            m_Flags = (ImplFlag)reader.ReadEncodedInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.InsuredFor))
                            /*m_InsuredFor = */
                        {
                            reader.ReadEntity<Mobile>();
                        }

                        if (GetSaveFlag(flags, SaveFlag.BlessedFor))
                        {
                            AcquireCompactInfo().m_BlessedFor = reader.ReadEntity<Mobile>();
                        }

                        if (GetSaveFlag(flags, SaveFlag.HeldBy))
                        {
                            AcquireCompactInfo().m_HeldBy = reader.ReadEntity<Mobile>();
                        }

                        if (GetSaveFlag(flags, SaveFlag.SavedFlags))
                        {
                            AcquireCompactInfo().m_SavedFlags = reader.ReadEncodedInt();
                        }

                        if (m_Map != null && m_Parent == null)
                        {
                            m_Map.OnEnter(this);
                        }

                        break;
                    }
                case 5:
                    {
                        var flags = (SaveFlag)reader.ReadInt();

                        LastMoved = reader.ReadDeltaTime();

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                        {
                            m_Direction = (Direction)reader.ReadByte();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                        {
                            AcquireCompactInfo().m_Bounce = BounceInfo.Deserialize(reader);
                        }

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                        {
                            m_LootType = (LootType)reader.ReadByte();
                        }

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                        {
                            m_Location = reader.ReadPoint3D();
                        }

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                        {
                            m_ItemID = reader.ReadInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                        {
                            m_Hue = reader.ReadInt();
                        }

                        m_Amount = GetSaveFlag(flags, SaveFlag.Amount) ? reader.ReadInt() : 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                        {
                            m_Layer = (Layer)reader.ReadByte();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Name))
                        {
                            var name = reader.ReadString();

                            if (name != DefaultName)
                            {
                                AcquireCompactInfo().m_Name = name;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadSerial();

                            if (parent.IsMobile)
                            {
                                m_Parent = World.FindMobile(parent);
                            }
                            else if (parent.IsItem)
                            {
                                m_Parent = World.FindItem(parent);
                            }
                            else
                            {
                                m_Parent = null;
                            }

                            if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                            {
                                Delete();
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                        {
                            var items = reader.ReadEntityList<Item>();

                            if (this is Container cont)
                            {
                                cont.m_Items = items;
                            }
                            else
                            {
                                AcquireCompactInfo().m_Items = items;
                            }
                        }

                        double weight;

                        if (GetSaveFlag(flags, SaveFlag.IntWeight))
                        {
                            weight = reader.ReadEncodedInt();
                        }
                        else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                        {
                            weight = reader.ReadDouble();
                        }
                        else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                        {
                            weight = 0.0;
                        }
                        else
                        {
                            weight = 1.0;
                        }

                        if (weight != DefaultWeight)
                        {
                            AcquireCompactInfo().m_Weight = weight;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Map))
                        {
                            m_Map = reader.ReadMap();
                        }
                        else
                        {
                            m_Map = Map.Internal;
                        }

                        SetFlag(ImplFlag.Visible, !GetSaveFlag(flags, SaveFlag.Visible) || reader.ReadBool());
                        SetFlag(ImplFlag.Movable, !GetSaveFlag(flags, SaveFlag.Movable) || reader.ReadBool());

                        if (GetSaveFlag(flags, SaveFlag.Stackable))
                        {
                            SetFlag(ImplFlag.Stackable, reader.ReadBool());
                        }

                        if (m_Map != null && m_Parent == null)
                        {
                            m_Map.OnEnter(this);
                        }

                        break;
                    }
                case 4: // Just removed variables
                case 3:
                    {
                        m_Direction = (Direction)reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        AcquireCompactInfo().m_Bounce = BounceInfo.Deserialize(reader);
                        LastMoved = reader.ReadDeltaTime();

                        goto case 1;
                    }
                case 1:
                    {
                        m_LootType = (LootType)reader.ReadByte(); // m_Newbied = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Location = reader.ReadPoint3D();
                        m_ItemID = reader.ReadInt();
                        m_Hue = reader.ReadInt();
                        m_Amount = reader.ReadInt();
                        m_Layer = (Layer)reader.ReadByte();

                        var name = reader.ReadString();

                        if (name != DefaultName)
                        {
                            AcquireCompactInfo().m_Name = name;
                        }

                        Serial parent = reader.ReadSerial();

                        if (parent.IsMobile)
                        {
                            m_Parent = World.FindMobile(parent);
                        }
                        else if (parent.IsItem)
                        {
                            m_Parent = World.FindItem(parent);
                        }
                        else
                        {
                            m_Parent = null;
                        }

                        if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                        {
                            Delete();
                        }

                        var count = reader.ReadInt();

                        if (count > 0)
                        {
                            var items = new List<Item>(count);

                            for (var i = 0; i < count; ++i)
                            {
                                var item = reader.ReadEntity<Item>();

                                if (item != null)
                                {
                                    items.Add(item);
                                }
                            }

                            if (this is Container cont)
                            {
                                cont.m_Items = items;
                            }
                            else
                            {
                                AcquireCompactInfo().m_Items = items;
                            }
                        }

                        var weight = reader.ReadDouble();

                        if (weight != DefaultWeight)
                        {
                            AcquireCompactInfo().m_Weight = weight;
                        }

                        if (version <= 3)
                        {
                            reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadInt();
                        }

                        m_Map = reader.ReadMap();
                        SetFlag(ImplFlag.Visible, reader.ReadBool());
                        SetFlag(ImplFlag.Movable, reader.ReadBool());

                        if (version <= 3)
                            /*m_Deleted =*/
                        {
                            reader.ReadBool();
                        }

                        Stackable = reader.ReadBool();

                        if (m_Map != null && m_Parent == null)
                        {
                            m_Map.OnEnter(this);
                        }

                        break;
                    }
            }

            if (HeldBy != null)
            {
                Timer.StartTimer(FixHolding_Sandbox);
            }

            // if (version < 9)
            VerifyCompactInfo();
        }

        private void FixHolding_Sandbox()
        {
            var heldBy = HeldBy;

            if (heldBy != null)
            {
                if (GetBounce() != null)
                {
                    Bounce(heldBy);
                }
                else
                {
                    heldBy.Holding = null;
                    heldBy.AddToBackpack(this);
                    ClearBounce();
                }
            }
        }

        public virtual int GetMaxUpdateRange() => 18;

        public virtual int GetUpdateRange(Mobile m) => 18;

        public virtual void SendInfoTo(NetState ns, ReadOnlySpan<byte> world = default, Span<byte> opl = default)
        {
            SendWorldPacketTo(ns, world);
            SendOPLPacketTo(ns, opl);
        }

        public void SendOPLPacketTo(NetState ns, Span<byte> opl = default)
        {
            if (!ObjectPropertyList.Enabled)
            {
                return;
            }

            if (opl == null)
            {
                ns.SendOPLInfo(this);
                return;
            }

            OutgoingEntityPackets.CreateOPLInfo(opl, this);
            ns.Send(opl);
        }

        public virtual void SendWorldPacketTo(NetState ns, ReadOnlySpan<byte> world = default)
        {
            if (world != null)
            {
                ns?.Send(world);
                return;
            }

            ns.SendWorldItem(this);
        }

        public virtual int GetTotal(TotalType type) => 0;

        public virtual void UpdateTotal(Item sender, TotalType type, int delta)
        {
            if (!IsVirtualItem)
            {
                if (m_Parent is Item item)
                {
                    item.UpdateTotal(sender, type, delta);
                }
                else if (m_Parent is Mobile mobile)
                {
                    mobile.UpdateTotal(sender, type, delta);
                }
                else
                {
                    HeldBy?.UpdateTotal(sender, type, delta);
                }
            }
        }

        public virtual void UpdateTotals()
        {
        }

        public virtual void HandleInvalidTransfer(Mobile from)
        {
            // OSI sends 1074769, bug!
            if (QuestItem)
            {
                // You can only drop quest items into the top-most level of your backpack
                // while you still need them for your quest.
                from.SendLocalizedMessage(1049343);
            }
        }

        public bool ParentsContain<T>() where T : Item
        {
            var p = m_Parent;

            while (p is Item item)
            {
                if (item is T)
                {
                    return true;
                }

                if (item.m_Parent == null)
                {
                    break;
                }

                p = item.m_Parent;
            }

            return false;
        }

        public virtual void AddItem(Item item)
        {
            if (item?.Deleted != false || item.m_Parent == this)
            {
                return;
            }

            if (item == this)
            {
                Console.WriteLine(
                    "Warning: Adding item to itself: [0x{0:X} {1}].AddItem( [0x{2:X} {3}] )",
                    Serial.Value,
                    GetType().Name,
                    item.Serial.Value,
                    item.GetType().Name
                );
                Console.WriteLine(new StackTrace());
                return;
            }

            if (IsChildOf(item))
            {
                Console.WriteLine(
                    "Warning: Adding parent item to child: [0x{0:X} {1}].AddItem( [0x{2:X} {3}] )",
                    Serial.Value,
                    GetType().Name,
                    item.Serial.Value,
                    item.GetType().Name
                );
                Console.WriteLine(new StackTrace());
                return;
            }

            if (item.m_Parent is Mobile parentMobile)
            {
                parentMobile.RemoveItem(item);
            }
            else if (item.m_Parent is Item parentItem)
            {
                parentItem.RemoveItem(item);
            }
            else
            {
                item.SendRemovePacket();
            }

            item.Parent = this;
            item.Map = m_Map;

            var items = AcquireItems();

            items.Add(item);

            if (!item.IsVirtualItem)
            {
                UpdateTotal(item, TotalType.Gold, item.TotalGold);
                UpdateTotal(item, TotalType.Items, item.TotalItems + 1);
                UpdateTotal(item, TotalType.Weight, item.TotalWeight + item.PileWeight);
            }

            item.Delta(ItemDelta.Update);

            item.OnAdded(this);
            OnItemAdded(item);
        }

        public void Delta(ItemDelta flags)
        {
            if (m_Map == null || m_Map == Map.Internal)
            {
                return;
            }

            m_DeltaFlags |= flags;

            if (!GetFlag(ImplFlag.InQueue))
            {
                SetFlag(ImplFlag.InQueue, true);
                m_DeltaQueue.Enqueue(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemDelta(ItemDelta flags)
        {
            m_DeltaFlags &= ~flags;
        }

        public static void ProcessDeltaQueue()
        {
            var limit = m_DeltaQueue.Count;

            while (m_DeltaQueue.Count > 0 && --limit >= 0)
            {
                var item = m_DeltaQueue.Dequeue();

                if (item == null)
                {
                    continue;
                }

                item.SetFlag(ImplFlag.InQueue, false);

                try
                {
                    item.ProcessDelta();
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, "Process Delta Queue for {Item} failed", item);
                }
            }

            if (m_DeltaQueue.Count > 0)
            {
                Utility.PushColor(ConsoleColor.DarkYellow);
                Console.WriteLine("Warning: {0} items left in delta queue after processing.", m_DeltaQueue.Count);
                Utility.PopColor();
            }
        }

        public virtual void OnDelete()
        {
            if (Spawner != null)
            {
                Spawner.Remove(this);
                Spawner = null;
            }
        }

        public virtual void OnParentDeleted(IEntity parent)
        {
            Delete();
        }

        public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            if (m_Map == null)
            {
                return;
            }

            var worldLoc = GetWorldLocation();
            var eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            foreach (var state in eable)
            {
                var m = state.Mobile;

                if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                {
                    var length = OutgoingMessagePackets.CreateMessage(
                        buffer, Serial, m_ItemID, type, hue, 3, ascii, "ENU", Name, text
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, string args = "")
        {
            if (m_Map == null)
            {
                return;
            }

            var worldLoc = GetWorldLocation();
            var eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args)].InitializePacket();

            foreach (var state in eable)
            {
                var m = state.Mobile;

                if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                {
                    var length = OutgoingMessagePackets.CreateMessageLocalized(
                        buffer, Serial, m_ItemID, type, hue, 3, number, Name, args
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public virtual void OnAfterDelete()
        {
        }

        public virtual void RemoveItem(Item item)
        {
            var items = LookupItems();

            if (items.Remove(item))
            {
                item.SendRemovePacket();

                if (!item.IsVirtualItem)
                {
                    UpdateTotal(item, TotalType.Gold, -item.TotalGold);
                    UpdateTotal(item, TotalType.Items, -(item.TotalItems + 1));
                    UpdateTotal(item, TotalType.Weight, -(item.TotalWeight + item.PileWeight));
                }

                item.Parent = null;

                item.OnRemoved(this);
                OnItemRemoved(item);
            }
        }

        public virtual void OnAfterDuped(Item newItem)
        {
        }

        public virtual bool OnDragLift(Mobile from) => true;

        public virtual bool OnEquip(Mobile from) => true;

        protected virtual void OnAmountChange(int oldValue)
        {
        }

        public virtual void OnSpeech(SpeechEventArgs e)
        {
        }

        public virtual bool OnDroppedToMobile(Mobile from, Mobile target)
        {
            if (Nontransferable && from.Player)
            {
                HandleInvalidTransfer(from);
                return false;
            }

            return true;
        }

        public virtual bool DropToMobile(Mobile from, Mobile target, Point3D p) =>
            !(Deleted || from.Deleted || target.Deleted) && from.Map == target.Map && from.Map != null &&
            target.Map != null && (from.AccessLevel >= AccessLevel.GameMaster || from.InRange(target.Location, 2)) &&
            from.CanSee(target) && from.InLOS(target) && from.OnDroppedItemToMobile(this, target) &&
            OnDroppedToMobile(from, target) && target.OnDragDrop(from, this);

        public virtual bool OnDroppedInto(Mobile from, Container target, Point3D p)
        {
            if (!from.OnDroppedItemInto(this, target, p))
            {
                return false;
            }

            if (Nontransferable && from.Player && target != from.Backpack)
            {
                HandleInvalidTransfer(from);
                return false;
            }

            return target.OnDragDropInto(from, this, p);
        }

        public virtual bool OnDroppedOnto(Mobile from, Item target)
        {
            if (Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null ||
                target.Map == null)
            {
                return false;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.GetWorldLocation(), 2))
            {
                return false;
            }

            if (!from.CanSee(target) || !from.InLOS(target))
            {
                return false;
            }

            if (!target.IsAccessibleTo(from))
            {
                return false;
            }

            if (!from.OnDroppedItemOnto(this, target))
            {
                return false;
            }

            if (Nontransferable && from.Player && target != from.Backpack)
            {
                HandleInvalidTransfer(from);
                return false;
            }

            return target.OnDragDrop(from, this);
        }

        public virtual bool DropToItem(Mobile from, Item target, Point3D p)
        {
            if (Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null ||
                target.Map == null)
            {
                return false;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.GetWorldLocation(), 2))
            {
                return false;
            }

            if (!from.CanSee(target) || !from.InLOS(target))
            {
                return false;
            }

            if (!target.IsAccessibleTo(from))
            {
                return false;
            }

            if (target.RootParent is Mobile mobile && !mobile.CheckNonlocalDrop(from, this, target))
            {
                return false;
            }

            if (!from.OnDroppedItemToItem(this, target, p))
            {
                return false;
            }

            if (target is Container container && p.m_X != -1 && p.m_Y != -1)
            {
                return OnDroppedInto(from, container, p);
            }

            return OnDroppedOnto(from, target);
        }

        public virtual bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            if (Nontransferable && from.Player)
            {
                HandleInvalidTransfer(from);
                return false;
            }

            return true;
        }

        public virtual int GetLiftSound(Mobile from) => 0x57;

        public virtual bool DropToWorld(Mobile from, Point3D p)
        {
            if (Deleted || from.Deleted || from.Map == null)
            {
                return false;
            }

            if (!from.InRange(p, 2))
            {
                return false;
            }

            var map = from.Map;

            if (map == null)
            {
                return false;
            }

            int x = p.m_X, y = p.m_Y;
            var z = int.MinValue;

            var maxZ = from.Z + 16;

            var landTile = map.Tiles.GetLandTile(x, y);
            var landFlags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;

            map.GetAverageZ(x, y, out var landZ, out var landAvg, out _);

            if (!landTile.Ignored && (landFlags & TileFlag.Impassable) == 0)
            {
                if (landAvg <= maxZ)
                {
                    z = landAvg;
                }
            }

            var tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (var i = 0; i < tiles.Length; ++i)
            {
                var tile = tiles[i];
                var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                if (!id.Surface)
                {
                    continue;
                }

                var top = tile.Z + id.CalcHeight;

                if (top > maxZ || top < z)
                {
                    continue;
                }

                z = top;
            }

            var eable = map.GetItemsInRange(p, 0);

            var items = new List<Item>();
            foreach (var item in eable)
            {
                if (item is BaseMulti || item.ItemID > TileData.MaxItemValue)
                {
                    continue;
                }

                var id = item.ItemData;

                if (id.Surface)
                {
                    var top = item.Z + id.CalcHeight;
                    if (top <= maxZ && top >= z)
                    {
                        z = top;
                    }
                }

                items.Add(item);
            }

            eable.Free();

            if (z == int.MinValue)
            {
                return false;
            }

            if (z > maxZ)
            {
                return false;
            }

            m_OpenSlots = (1 << 20) - 1;

            var surfaceZ = z;

            for (var i = 0; i < tiles.Length; ++i)
            {
                var tile = tiles[i];
                var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                var checkZ = tile.Z;
                var checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                {
                    ++checkTop;
                }

                var zStart = Math.Max(checkZ - z, 0);
                var zEnd = Math.Min(checkTop - z, 19);

                if (zStart >= 20 || zEnd < 0)
                {
                    continue;
                }

                var bitCount = zEnd - zStart;

                m_OpenSlots &= ~(((1 << bitCount) - 1) << zStart);
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var id = item.ItemData;

                var checkZ = item.Z;
                var checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                {
                    ++checkTop;
                }

                var zStart = Math.Max(checkZ - z, 0);
                var zEnd = Math.Min(checkTop - z, 19);

                if (zStart >= 20 || zEnd < 0)
                {
                    continue;
                }

                var bitCount = zEnd - zStart;

                m_OpenSlots &= ~(((1 << bitCount) - 1) << zStart);
            }

            var height = ItemData.Height;

            if (height == 0)
            {
                ++height;
            }

            if (height > 30)
            {
                height = 30;
            }

            var match = (1 << height) - 1;
            var okay = false;

            for (var i = 0; i < 20; ++i)
            {
                if (i + height > 20)
                {
                    match >>= 1;
                }

                okay = ((m_OpenSlots >> i) & match) == match;

                if (okay)
                {
                    z += i;
                    break;
                }
            }

            if (!okay)
            {
                return false;
            }

            height = ItemData.Height;

            if (height == 0)
            {
                ++height;
            }

            if (landAvg > z && z + height > landZ)
            {
                return false;
            }

            if ((landFlags & TileFlag.Impassable) != 0 && landAvg > surfaceZ && z + height > landZ)
            {
                return false;
            }

            for (var i = 0; i < tiles.Length; ++i)
            {
                var tile = tiles[i];
                var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                var checkZ = tile.Z;
                var checkTop = checkZ + id.CalcHeight;

                if (checkTop > z && z + height > checkZ)
                {
                    return false;
                }

                if ((id.Surface || id.Impassable) && checkTop > surfaceZ && z + height > checkZ)
                {
                    return false;
                }
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var id = item.ItemData;

                // int checkZ = item.Z;
                // int checkTop = checkZ + id.CalcHeight;

                if (item.Z + id.CalcHeight > z && z + height > item.Z)
                {
                    return false;
                }
            }

            p = new Point3D(x, y, z);

            if (!from.InLOS(new Point3D(x, y, z + 1)))
            {
                return false;
            }

            if (!from.OnDroppedItemToWorld(this, p))
            {
                return false;
            }

            if (!OnDroppedToWorld(from, p))
            {
                return false;
            }

            var soundID = GetDropSound();

            MoveToWorld(p, from.Map);

            from.SendSound(soundID == -1 ? 0x42 : soundID, GetWorldLocation());

            return true;
        }

        public void SendRemovePacket() => SendRemovePacket(GetWorldLocation());

        public void SendRemovePacket(Point3D worldLoc)
        {
            if (Deleted || m_Map == null)
            {
                return;
            }

            var eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

            Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

            foreach (var state in eable)
            {
                var m = state.Mobile;

                if (m.InRange(worldLoc, GetUpdateRange(m)))
                {
                    OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                    state.Send(removeEntity);
                }
            }

            eable.Free();
        }

        public virtual int GetDropSound() => -1;

        public Point3D GetWorldLocation()
        {
            var root = RootParent;

            if (root == null)
            {
                return m_Location;
            }

            return root.Location;

            // return root == null ? m_Location : new Point3D( (IPoint3D) root );
        }

        public Point3D GetSurfaceTop()
        {
            var root = RootParent;

            if (root == null)
            {
                return new Point3D(
                    m_Location.m_X,
                    m_Location.m_Y,
                    m_Location.m_Z + (ItemData.Surface ? ItemData.CalcHeight : 0)
                );
            }

            return root.Location;
        }

        public Point3D GetWorldTop() => RootParent?.Location ??
                                        new Point3D(m_Location.m_X, m_Location.m_Y, m_Location.m_Z + ItemData.CalcHeight);

        public void SendLocalizedMessageTo(Mobile to, int number, string args = "")
        {
            if (Deleted || !to.CanSee(this))
            {
                return;
            }

            to.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", args);
        }

        public void SendLocalizedMessageTo(Mobile to, int number, AffixType affixType, string affix, string args)
        {
            if (Deleted || !to.CanSee(this))
            {
                return;
            }

            to.NetState.SendMessageLocalizedAffix(
                Serial,
                ItemID,
                MessageType.Regular,
                0x3B2,
                3,
                number,
                "",
                affixType,
                affix,
                args
            );
        }

        public virtual void OnSnoop(Mobile from)
        {
        }

        public SecureTradeContainer GetSecureTradeCont()
        {
            object p = this;

            while (p is Item item)
            {
                if (item is SecureTradeContainer container)
                {
                    return container;
                }

                p = item.m_Parent;
            }

            return null;
        }

        public virtual void OnItemAdded(Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSubItemAdded(item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnSubItemAdded(item);
            }
        }

        public virtual void OnItemRemoved(Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSubItemRemoved(item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnSubItemRemoved(item);
            }
        }

        public virtual void OnSubItemAdded(Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSubItemAdded(item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnSubItemAdded(item);
            }
        }

        public virtual void OnSubItemRemoved(Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSubItemRemoved(item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnSubItemRemoved(item);
            }
        }

        public virtual void OnItemBounceCleared(Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSubItemBounceCleared(item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnSubItemBounceCleared(item);
            }
        }

        public virtual void OnSubItemBounceCleared(Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSubItemBounceCleared(item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnSubItemBounceCleared(item);
            }
        }

        public virtual bool CheckTarget(Mobile from, Target targ, object targeted) =>
            m_Parent switch
            {
                Item item     => item.CheckTarget(from, targ, targeted),
                Mobile mobile => mobile.CheckTarget(from, targ, targeted),
                _             => true
            };

        public virtual bool IsAccessibleTo(Mobile check)
        {
            if (m_Parent is Item item)
            {
                return item.IsAccessibleTo(check);
            }

            var reg = Region.Find(GetWorldLocation(), m_Map);

            return reg.CheckAccessibility(this, check);

            /*SecureTradeContainer cont = GetSecureTradeCont();

            if (cont != null && !cont.IsChildOf( check ))
              return false;

            return true;*/
        }

        public bool IsChildOf(IEntity o) => IsChildOf(o, false);

        public bool IsChildOf(IEntity o, bool allowNull)
        {
            var p = m_Parent;

            if ((p == null || o == null) && !allowNull)
            {
                return false;
            }

            if (p == o)
            {
                return true;
            }

            while (p is Item item)
            {
                if (item.m_Parent == null)
                {
                    break;
                }

                p = item.m_Parent;

                if (p == o)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void OnItemUsed(Mobile from, Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnItemUsed(from, item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnItemUsed(from, item);
            }
        }

        public bool CheckItemUse(Mobile from) => CheckItemUse(from, this);

        public virtual bool CheckItemUse(Mobile from, Item item) =>
            m_Parent switch
            {
                Item parentItem     => parentItem.CheckItemUse(from, item),
                Mobile parentMobile => parentMobile.CheckItemUse(from, item),
                _                   => true
            };

        public virtual void OnItemLifted(Mobile from, Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnItemLifted(from, item);
            }
            else if (m_Parent is Mobile parentMobile)
            {
                parentMobile.OnItemLifted(from, item);
            }
        }

        public bool CheckLift(Mobile from)
        {
            var reject = LRReason.Inspecific;

            return CheckLift(from, this, ref reject);
        }

        public virtual bool CheckLift(Mobile from, Item item, ref LRReason reject) =>
            m_Parent switch
            {
                Item parentItem     => parentItem.CheckLift(from, item, ref reject),
                Mobile parentMobile => parentMobile.CheckLift(from, item, ref reject),
                _                   => true
            };

        public virtual void OnSingleClickContained(Mobile from, Item item)
        {
            if (m_Parent is Item parentItem)
            {
                parentItem.OnSingleClickContained(from, item);
            }
        }

        public virtual void OnAosSingleClick(Mobile from)
        {
            var opl = PropertyList;

            if (opl.Header > 0)
            {
                from.NetState.SendMessageLocalized(
                    Serial,
                    m_ItemID,
                    MessageType.Label,
                    0x3B2,
                    3,
                    opl.Header,
                    Name,
                    opl.HeaderArgs
                );
            }
        }

        public virtual void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
            {
                return;
            }

            if (DisplayLootType)
            {
                LabelLootTypeTo(from);
            }

            var ns = from.NetState;

            if (ns.CannotSendPackets())
            {
                return;
            }

            if (Name == null)
            {
                if (m_Amount <= 1)
                {
                    ns.SendMessageLocalized(Serial, m_ItemID, MessageType.Label, 0x3B2, 3, LabelNumber);
                }
                else
                {
                    ns.SendMessageLocalizedAffix(
                        Serial,
                        m_ItemID,
                        MessageType.Label,
                        0x3B2,
                        3,
                        LabelNumber,
                        "",
                        AffixType.Append,
                        $" : {m_Amount}"
                    );
                }
            }
            else
            {
                ns.SendMessage(
                    Serial,
                    m_ItemID,
                    MessageType.Label,
                    0x3B2,
                    3,
                    false,
                    "ENU",
                    "",
                    $"{Name}{(m_Amount > 1 ? $" : {m_Amount}" : "")}"
                );
            }
        }

        public virtual void ScissorHelper(Mobile from, Item newItem, int amountPerOldItem)
        {
            ScissorHelper(from, newItem, amountPerOldItem, true);
        }

        public virtual void ScissorHelper(Mobile from, Item newItem, int amountPerOldItem, bool carryHue)
        {
            // let's not go over 60000
            var amount = Math.Min(Amount, 60000 / amountPerOldItem);

            Amount -= amount;

            var ourHue = Hue;
            var thisMap = Map;
            var thisParent = m_Parent;
            var worldLoc = GetWorldLocation();
            var type = LootType;

            if (Amount == 0)
            {
                Delete();
            }

            newItem.Amount = amount * amountPerOldItem;

            if (carryHue)
            {
                newItem.Hue = ourHue;
            }

            if (ScissorCopyLootType)
            {
                newItem.LootType = type;
            }

            if ((thisParent as Container)?.TryDropItem(from, newItem, false) != true)
            {
                newItem.MoveToWorld(worldLoc, thisMap);
            }
        }

        public virtual void Consume()
        {
            Consume(1);
        }

        public virtual void Consume(int amount)
        {
            Amount -= amount;

            if (Amount <= 0)
            {
                Delete();
            }
        }

        public virtual void ReplaceWith(Item newItem)
        {
            if (m_Parent is Container container)
            {
                container.AddItem(newItem);
                newItem.Location = m_Location;
            }
            else
            {
                newItem.MoveToWorld(GetWorldLocation(), m_Map);
            }

            Delete();
        }

        public virtual bool CheckBlessed(Mobile m) =>
            m_LootType == LootType.Blessed || Mobile.InsuranceEnabled && Insured || m != null && m == BlessedFor;

        public virtual bool CheckNewbied() => m_LootType == LootType.Newbied;

        public virtual bool IsStandardLoot() =>
            (!Mobile.InsuranceEnabled || !Insured) && BlessedFor == null && m_LootType == LootType.Regular;

        public override string ToString() => $"0x{Serial.Value:X} \"{GetType().Name}\"";

        public virtual void OnSectorActivate()
        {
        }

        public virtual void OnSectorDeactivate()
        {
        }

        public virtual void OnLocationChange(Point3D oldLocation)
        {
        }

        public virtual void OnDoubleClick(Mobile from)
        {
        }

        public virtual void OnDoubleClickOutOfRange(Mobile from)
        {
        }

        public virtual void OnDoubleClickCantSee(Mobile from)
        {
        }

        public virtual void OnDoubleClickDead(Mobile from)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019048); // I am dead and cannot do that.
        }

        public virtual void OnDoubleClickNotAccessible(Mobile from)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        public virtual void OnDoubleClickSecureTrade(Mobile from)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        [Flags]
        private enum ImplFlag : byte
        {
            None = 0x00,
            Visible = 0x01,
            Movable = 0x02,
            Deleted = 0x04,
            Stackable = 0x08,
            InQueue = 0x10,
            Insured = 0x20,
            PaidInsurance = 0x40,
            QuestItem = 0x80
        }

        private class CompactInfo
        {
            public Mobile m_BlessedFor;
            public BounceInfo m_Bounce;

            public Mobile m_HeldBy;

            public List<Item> m_Items;
            public string m_Name;
            public int m_SavedFlags;

            public ISpawner m_Spawner;

            public int m_TempFlags;

            public double m_Weight = -1;
        }

        [Flags]
        private enum SaveFlag : uint
        {
            None = 0x00000000,
            Direction = 0x00000001,
            Bounce = 0x00000002,
            LootType = 0x00000004,
            LocationFull = 0x00000008,
            ItemID = 0x00000010,
            Hue = 0x00000020,
            Amount = 0x00000040,
            Layer = 0x00000080,
            Name = 0x00000100,
            Parent = 0x00000200,
            Items = 0x00000400,
            WeightNot1or0 = 0x00000800,
            Map = 0x00001000,
            Visible = 0x00002000,
            Movable = 0x00004000,
            Stackable = 0x00008000,
            WeightIs0 = 0x00010000,
            LocationSByteZ = 0x00020000,
            LocationShortXY = 0x00040000,
            LocationByteXY = 0x00080000,
            ImplFlags = 0x00100000,
            InsuredFor = 0x00200000,
            BlessedFor = 0x00400000,
            HeldBy = 0x00800000,
            IntWeight = 0x01000000,
            SavedFlags = 0x02000000,
            NullWeight = 0x04000000
        }
    }
}
