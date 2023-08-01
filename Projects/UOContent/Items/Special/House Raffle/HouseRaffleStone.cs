using System;
using System.Collections.Generic;
using System.Net;
using ModernUO.Serialization;
using Server.Accounting;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Text;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RaffleEntry
{
    [SerializableField(0, setter: "private")]
    private Mobile _from;

    [SerializableField(1, setter: "private")]
    private IPAddress _address;

    [SerializableField(2, setter: "private")]
    private DateTime _date;

    public RaffleEntry(Mobile from)
    {
        _from = from;
        _address = from?.NetState?.Address ?? IPAddress.None;
        _date = Core.Now;
    }

    public RaffleEntry()
    {
        _from = null;
        _address = null;
        _date = Core.Now;
    }
}

public enum HouseRaffleState
{
    Inactive,
    Active,
    Completed
}

public enum HouseRaffleExpireAction
{
    None,
    HideStone,
    DeleteStone
}

[SerializationGenerator(4, false)]
[Flippable(0xEDD, 0xEDE)]
public partial class HouseRaffleStone : Item
{
    private const int EntryLimitPerIP = 4;
    private const int DefaultTicketPrice = 5000;
    private const int MessageHue = 1153;

    public static readonly TimeSpan DefaultDuration = TimeSpan.FromDays(7.0); // Duration of the raffle itself
    public static readonly TimeSpan ExpirationTime = TimeSpan.FromDays(30.0); // Time a person has to use the stone to place the house

    private static Timer _allStonesTimer;
    private static HashSet<HouseRaffleStone> _allStones;

    private HouseRaffleRegion _region;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private HouseRaffleExpireAction _expireAction;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private HouseRaffleDeed _deed;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private Mobile _winner;

    [InvalidateProperties]
    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private DateTime _started;

    [InvalidateProperties]
    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private TimeSpan _duration;

    [SerializableField(9, setter: "private")]
    private List<RaffleEntry> _entries;

    [Constructible]
    public HouseRaffleStone() : base(0xEDD)
    {
        _region = null;
        _plotBounds = new Rectangle2D();
        _plotFacet = null;

        _winner = null;
        _deed = null;

        _currentState = HouseRaffleState.Inactive;
        _started = DateTime.MinValue;
        _duration = DefaultDuration;
        _expireAction = HouseRaffleExpireAction.None;
        _ticketPrice = DefaultTicketPrice;

        Entries = new List<RaffleEntry>();

        Movable = false;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    public HouseRaffleState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState != value)
            {
                if (value == HouseRaffleState.Active)
                {
                    this.Clear(_entries);
                    Winner = null;
                    Deed = null;
                    Started = Core.Now;
                    AddRaffleStone(this);
                }
                else
                {
                    RemoveRaffleStone(this);
                }

                _currentState = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    public Rectangle2D PlotBounds
    {
        get => _plotBounds;
        set
        {
            _plotBounds = value;

            InvalidateRegion();
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableProperty(4)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    public Map PlotFacet
    {
        get => _plotFacet;
        set
        {
            _plotFacet = value;

            InvalidateRegion();
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsExpired
    {
        get
        {
            if (_currentState != HouseRaffleState.Completed)
            {
                return false;
            }

            return Core.Now > _started + _duration + ExpirationTime;
        }
    }

    [SerializableProperty(6)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    public int TicketPrice
    {
        get => _ticketPrice;
        set
        {
            _ticketPrice = Math.Max(0, value);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override string DefaultName => "a house raffle stone";

    public override bool DisplayWeight => false;

    public static void CheckEnd_OnTick()
    {
        foreach (var stone in _allStones)
        {
            stone.CheckEnd();
        }
    }

    private static void AddRaffleStone(HouseRaffleStone stone)
    {
        _allStones ??= new HashSet<HouseRaffleStone>();
        _allStones.Add(stone);

        if (_allStones.Count == 1)
        {
            Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), CheckEnd_OnTick);
        }
    }

    private static void RemoveRaffleStone(HouseRaffleStone stone)
    {
        if (_allStones?.Remove(stone) == true && _allStones.Count == 0)
        {
            _allStonesTimer.Stop();
            _allStonesTimer = null;
        }
    }

    public bool ValidLocation() =>
        _plotBounds.Start != Point2D.Zero &&
        _plotBounds.End != Point2D.Zero &&
        _plotFacet != null && _plotFacet != Map.Internal;

    private void InvalidateRegion()
    {
        if (_region != null)
        {
            _region.Unregister();
            _region = null;
        }

        if (ValidLocation())
        {
            _region = new HouseRaffleRegion(this);
            _region.Register();
        }
    }

    private bool HasEntered(Mobile from)
    {
        if (from.Account is not Account acc)
        {
            return false;
        }

        foreach (var entry in Entries)
        {
            if (entry.From != null)
            {
                var entryAcc = entry.From.Account as Account;

                if (entryAcc == acc)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsAtIPLimit(Mobile from)
    {
        if (from.NetState == null)
        {
            return false;
        }

        var address = from.NetState.Address;
        var tickets = 0;

        foreach (var entry in Entries)
        {
            if (Utility.IPMatchClassC(entry.Address, address))
            {
                if (++tickets >= EntryLimitPerIP)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string FormatLocation(Point3D loc, Map map, bool displayMap)
    {
        using var result = ValueStringBuilder.Create();

        var xLong = 0;
        var yLat = 0;
        int xMins = 0;
        var yMins = 0;
        bool xEast = false, ySouth = false;

        if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
        {
            result.Append($"{yLat}°{yMins}'{(ySouth ? "S" : "N")},{xLong}°{xMins}'{(xEast ? "E" : "W")}");
        }
        else
        {
            result.Append($"{loc.X},{loc.Y}");
        }

        if (displayMap)
        {
            result.Append($" ({map})");
        }

        return result.ToString();
    }

    public Point3D GetPlotCenter()
    {
        var x = _plotBounds.X + _plotBounds.Width / 2;
        var y = _plotBounds.Y + _plotBounds.Height / 2;
        var z = _plotFacet?.GetAverageZ(x, y) ?? 0;

        return new Point3D(x, y, z);
    }

    public string FormatLocation() =>
        !ValidLocation() ? "no location set" : FormatLocation(GetPlotCenter(), _plotFacet, true);

    public string FormatPrice() => _ticketPrice == 0 ? "FREE" : $"{_ticketPrice} gold";

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ValidLocation())
        {
            list.Add(FormatLocation());
        }

        switch (_currentState)
        {
            case HouseRaffleState.Active:
                {
                    list.Add(1060658, $"{"ticket price"}\t{FormatPrice()}");  // ~1_val~: ~2_val~
                    list.Add(1060659, $"{"ends"}\t{_started + _duration}"); // ~1_val~: ~2_val~
                    break;
                }
            case HouseRaffleState.Completed:
                {
                    list.Add(1060658, $"winner\t{_winner?.Name ?? "Unknown"}"); // ~1_val~: ~2_val~
                    break;
                }
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        switch (_currentState)
        {
            case HouseRaffleState.Active:
                {
                    LabelTo(from, 1060658, $"Ends\t{_started + _duration}"); // ~1_val~: ~2_val~
                    break;
                }
            case HouseRaffleState.Completed:
                {
                    LabelTo(
                        from,
                        1060658, // ~1_val~: ~2_val~
                        $"Winner\t{_winner?.Name ?? "Unknown"}"
                    );
                    break;
                }
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.AccessLevel >= AccessLevel.Seer)
        {
            list.Add(new EditEntry(from, this));

            if (_currentState == HouseRaffleState.Inactive)
            {
                list.Add(new ActivateEntry(from, this));
            }
            else
            {
                list.Add(new ManagementEntry(from, this));
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_currentState != HouseRaffleState.Active || !from.CheckAlive())
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (HasEntered(from))
        {
            from.SendMessage(MessageHue, "You have already entered this plot's raffle.");
        }
        else if (IsAtIPLimit(from))
        {
            from.SendMessage(MessageHue, "You may not enter this plot's raffle.");
        }
        else
        {
            from.SendGump(
                new WarningGump(
                    1150470, // CONFIRM TICKET PURCHASE
                    0x7F00,
                    $"You are about to purchase a raffle ticket for the house plot located at {FormatLocation()}.  The ticket price is {FormatPrice()}.  Tickets are non-refundable and you can only purchase one ticket per account.  Do you wish to continue?",
                    0xFFFFFF,
                    420,
                    280,
                    okay => Purchase_Callback(from, okay)
                )
            );
        }
    }

    public void Purchase_Callback(Mobile from, bool okay)
    {
        if (Deleted || _currentState != HouseRaffleState.Active || !from.CheckAlive() || HasEntered(from) || IsAtIPLimit(from))
        {
            return;
        }

        if (from.Account is not Account)
        {
            return;
        }

        if (okay)
        {
            Container bank = from.FindBankNoCreate();

            if (_ticketPrice == 0 || from.Backpack?.ConsumeTotal(typeof(Gold), _ticketPrice) == true ||
                Banker.Withdraw(from, _ticketPrice))
            {
                this.Add(Entries, new RaffleEntry(from));

                from.SendMessage(MessageHue, "You have successfully entered the plot's raffle.");
            }
            else
            {
                from.SendMessage(MessageHue, $"You do not have the {FormatPrice()} required to enter the raffle.");
            }
        }
        else
        {
            from.SendMessage(MessageHue, "You have chosen not to enter the raffle.");
        }
    }

    public void CheckEnd()
    {
        if (_currentState != HouseRaffleState.Active || Core.Now < _started + _duration)
        {
            return;
        }

        CurrentState = HouseRaffleState.Completed;

        if (_region != null && _entries.Count != 0)
        {
            Winner = _entries.RandomElement().From;

            if (_winner != null)
            {
                Deed = new HouseRaffleDeed(this, _winner);

                _winner.SendMessage(
                    MessageHue,
                    $"Congratulations, {_winner.Name}! You have won the raffle for the plot located at {FormatLocation()}."
                );

                if (_winner.AddToBackpack(Deed))
                {
                    _winner.SendMessage(MessageHue, "The writ of lease has been placed in your backpack.");
                }
                else
                {
                    _winner.BankBox.DropItem(Deed);
                    _winner.SendMessage(
                        MessageHue,
                        "As your backpack is full, the writ of lease has been placed in your bank box."
                    );
                }
            }
        }

        InvalidateProperties();
    }

    public override void OnDelete()
    {
        if (_region != null)
        {
            _region.Unregister();
            _region = null;
        }

        RemoveRaffleStone(this);

        base.OnDelete();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _currentState = (HouseRaffleState)reader.ReadEncodedInt();
        _expireAction = (HouseRaffleExpireAction)reader.ReadEncodedInt();
        _deed = reader.ReadEntity<HouseRaffleDeed>();
        _plotBounds = reader.ReadRect2D();
        _plotFacet = reader.ReadMap();
        _winner = reader.ReadEntity<Mobile>();
        _ticketPrice = reader.ReadInt();
        _started = reader.ReadDateTime();
        _duration = reader.ReadTimeSpan();

        var entryCount = reader.ReadInt();
        _entries = new List<RaffleEntry>(entryCount);

        for (var i = 0; i < entryCount; i++)
        {
            var entry = new RaffleEntry();
            entry.Deserialize(reader);

            if (entry.From == null)
            {
                continue; // Character was deleted
            }

            _entries.Add(entry);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        InvalidateRegion();

        // Only check for expirations on world load since the wait time is so long (30 days)
        if (IsExpired)
        {
            switch (ExpireAction)
            {
                case HouseRaffleExpireAction.HideStone:
                    {
                        if (Visible)
                        {
                            Visible = false;
                            ItemID = 0x1B7B; // Non-blocking ItemID
                        }

                        break;
                    }
                case HouseRaffleExpireAction.DeleteStone:
                    {
                        Timer.DelayCall(Delete);
                        break;
                    }
            }
        }
        else if (_currentState == HouseRaffleState.Active)
        {
            AddRaffleStone(this);
        }
    }

    private class RaffleContextMenuEntry : ContextMenuEntry
    {
        protected readonly Mobile _from;
        protected readonly HouseRaffleStone _stone;

        public RaffleContextMenuEntry(Mobile from, HouseRaffleStone stone, int label) : base(label)
        {
            _from = from;
            _stone = stone;
        }
    }

    private class EditEntry : RaffleContextMenuEntry
    {
        public EditEntry(Mobile from, HouseRaffleStone stone) : base(from, stone, 5101) // Edit
        {
        }

        public override void OnClick()
        {
            if (_stone.Deleted || _from.AccessLevel < AccessLevel.Seer)
            {
                return;
            }

            _from.SendGump(new PropertiesGump(_from, _stone));
        }
    }

    private class ActivateEntry : RaffleContextMenuEntry
    {
        public ActivateEntry(Mobile from, HouseRaffleStone stone) : base(from, stone, 5113) // Start
        {
            if (!stone.ValidLocation())
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick()
        {
            if (_stone.Deleted || _from.AccessLevel < AccessLevel.Seer || !_stone.ValidLocation())
            {
                return;
            }

            _stone.CurrentState = HouseRaffleState.Active;
        }
    }

    private class ManagementEntry : RaffleContextMenuEntry
    {
        public ManagementEntry(Mobile from, HouseRaffleStone stone) : base(from, stone, 5032) // Game Monitor
        {
        }

        public override void OnClick()
        {
            if (_stone.Deleted || _from.AccessLevel < AccessLevel.Seer)
            {
                return;
            }

            _from.SendGump(new HouseRaffleManagementGump(_stone));
        }
    }
}
