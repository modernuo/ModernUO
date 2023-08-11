using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(4, false)]
public partial class Runebook : Item, ISecurable, ICraftable
{
    public static readonly TimeSpan UseDelay = TimeSpan.FromSeconds(7.0);

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BookQuality _quality;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafter;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(3, setter: "private")]
    private List<RunebookEntry> _entries;

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _description;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _curCharges;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxCharges;

    [SerializableField(7, getter: "private", setter: "private")]
    private int _defaultIndex;

    [Constructible]
    public Runebook() : this(Core.SE ? 12 : 6)
    {
    }

    [Constructible]
    public Runebook(int maxCharges) : base(Core.AOS ? 0x22C5 : 0xEFA)
    {
        Weight = Core.SE ? 1.0 : 3.0;
        LootType = LootType.Blessed;
        Hue = 0x461;

        Layer = Core.AOS ? Layer.Invalid : Layer.OneHanded;

        _entries = new List<RunebookEntry>();
        _maxCharges = maxCharges;
        _defaultIndex = -1;
        _level = SecureLevel.CoOwners;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextUse { get; set; }

    public List<Mobile> Openers { get; set; } = new();

    public override int LabelNumber => 1041267; // runebook

    public RunebookEntry Default
    {
        get
        {
            if (_defaultIndex >= 0 && _defaultIndex < Entries.Count)
            {
                return Entries[_defaultIndex];
            }

            return null;
        }
        set => DefaultIndex = value == null ? -1 : Entries.IndexOf(value);
    }

    public override bool DisplayLootType => Core.AOS;

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        var charges = Math.Min(5 + quality + (int)(from.Skills.Inscribe.Value / 30), 10);

        MaxCharges = Core.SE ? charges * 2 : charges;

        if (makersMark)
        {
            Crafter = from.RawName;
        }

        Quality = (BookQuality)(quality - 1);

        return quality;
    }

    public override bool AllowEquippedCast(Mobile from) => true;

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);
        SetSecureLevelEntry.AddTo(from, this, list);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _quality = (BookQuality)reader.ReadByte();
        Timer.DelayCall(crafter => _crafter = crafter?.RawName, reader.ReadEntity<Mobile>());
        _level = (SecureLevel)reader.ReadInt();

        var count = reader.ReadInt();

        Entries = new List<RunebookEntry>(count);

        for (var i = 0; i < count; ++i)
        {
            var entry = new RunebookEntry(this);
            entry.Deserialize(reader);
            Entries.Add(entry);
        }

        _description = reader.ReadString();
        _curCharges = reader.ReadInt();
        _maxCharges = reader.ReadInt();
        _defaultIndex = reader.ReadInt();
    }

    public void DropRune(Mobile from, RunebookEntry e, int index)
    {
        if (_defaultIndex > index)
        {
            DefaultIndex -= 1;
        }
        else if (_defaultIndex == index)
        {
            DefaultIndex = -1;
        }

        this.RemoveAt(_entries, index);

        var rune = new RecallRune
        {
            Target = e.Location,
            TargetMap = e.Map,
            Description = e.Description,
            House = e.House,
            Marked = true
        };

        from.AddToBackpack(rune);

        from.SendLocalizedMessage(502421); // You have removed the rune.
    }

    public bool IsOpen(Mobile toCheck)
    {
        var ns = toCheck.NetState;

        if (ns == null)
        {
            return false;
        }

        foreach (var gump in ns.Gumps)
        {
            if ((gump as RunebookGump)?.Book == this)
            {
                return true;
            }
        }

        return false;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_quality == BookQuality.Exceptional)
        {
            list.Add(1063341); // exceptional
        }

        if (_crafter != null)
        {
            list.Add(1050043, _crafter); // crafted by ~1_NAME~
        }

        if (!string.IsNullOrEmpty(_description))
        {
            list.Add(_description);
        }
    }

    public override bool OnDragLift(Mobile from)
    {
        if (from.HasGump<RunebookGump>())
        {
            from.SendLocalizedMessage(500169); // You cannot pick that up.
            return false;
        }

        foreach (var m in Openers)
        {
            if (IsOpen(m))
            {
                m.CloseGump<RunebookGump>();
            }
        }

        Openers.Clear();

        return true;
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_description?.Length > 0)
        {
            LabelTo(from, _description);
        }

        base.OnSingleClick(from);

        if (_crafter != null)
        {
            LabelTo(from, 1050043, _crafter);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), Core.ML ? 3 : 1) && CheckAccess(from))
        {
            if (RootParent is BaseCreature)
            {
                from.SendLocalizedMessage(502402); // That is inaccessible.
                return;
            }

            if (Core.Now < NextUse)
            {
                from.SendLocalizedMessage(502406); // This book needs time to recharge.
                return;
            }

            from.CloseGump<RunebookGump>();
            from.SendGump(new RunebookGump(from, this));

            Openers.Add(from);
        }
    }

    public virtual void OnTravel()
    {
        if (!Core.SA)
        {
            NextUse = Core.Now + UseDelay;
        }
    }

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is not Runebook book)
        {
            return;
        }

        book.Entries = new List<RunebookEntry>();

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            book.Entries.Add(new RunebookEntry(this, entry.Location, entry.Map, entry.Description, entry.House));
        }
    }

    public bool CheckAccess(Mobile m)
    {
        if (!IsLockedDown || m.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        var house = BaseHouse.FindHouseAt(this);

        return (house?.IsAosRules != true || house.Public && !house.IsBanned(m) || house.HasAccess(m)) &&
               house?.HasSecureAccess(m, Level) == true;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is RecallRune rune)
        {
            if (IsLockedDown && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendLocalizedMessage(502413, null, 0x35); // That cannot be done while the book is locked down.
                return false;
            }

            if (IsOpen(from))
            {
                from.SendLocalizedMessage(1005571); // You cannot place objects in the book while viewing the contents.
                return false;
            }

            if (Entries.Count >= 16)
            {
                from.SendLocalizedMessage(502401); // This runebook is full.
                return false;
            }

            if (rune.Marked && rune.TargetMap != null)
            {
                Entries.Add(new RunebookEntry(this, rune.Target, rune.TargetMap, rune.Description, rune.House));

                rune.Delete();

                from.SendSound(0x42, GetWorldLocation());
                from.SendMessage((rune.Description?.Trim()).DefaultIfNullOrEmpty("(indescript)"));

                return true;
            }

            from.SendLocalizedMessage(502409); // This rune does not have a marked location.
            return false;
        }

        if (dropped is RecallScroll)
        {
            if (CurCharges >= MaxCharges)
            {
                from.SendLocalizedMessage(502410); // This book already has the maximum amount of charges.
                return false;
            }

            from.SendSound(0x249, GetWorldLocation());

            var amount = dropped.Amount;

            if (amount > MaxCharges - CurCharges)
            {
                dropped.Consume(MaxCharges - CurCharges);
                CurCharges = MaxCharges;
            }
            else
            {
                CurCharges += amount;
                dropped.Delete();

                return true;
            }
        }

        return false;
    }
}

[SerializationGenerator(2)]
public partial class RunebookEntry
{
    [CanBeNull]
    [DirtyTrackingEntity]
    private Runebook _runebook;

    [SerializableField(0)]
    private BaseHouse _house;

    [SerializableFieldSaveFlag(0)]
    public bool ShouldSerializeHouse() => _house?.Deleted == false;

    [SerializableField(1)]
    private Point3D _location;

    [SerializableFieldSaveFlag(1)]
    public bool ShouldSerializeLocation() => _house?.Deleted != false;

    [SerializableField(2)]
    private Map _map;

    [SerializableFieldSaveFlag(2)]
    public bool ShouldSerializeMap() => _house?.Deleted != false;

    [SerializableField(3)]
    private string _description;

    [SerializableFieldSaveFlag(3)]
    public bool ShouldSerializeDesc() => _house?.Deleted != false;

    public RunebookEntry(
        Runebook runebook,
        Point3D loc = new(),
        Map map = null,
        string description = null,
        BaseHouse house = null
    )
    {
        _runebook = runebook;
        _house = house;
        _location = loc;
        _map = map;
        _description = description;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        switch (version)
        {
            case 1:
                {
                    _house = reader.ReadEntity<BaseHouse>();
                    goto case 0;
                }
            case 0:
                {
                    _location = reader.ReadPoint3D();
                    _map = reader.ReadMap();
                    _description = reader.ReadString();

                    break;
                }
        }
    }
}
