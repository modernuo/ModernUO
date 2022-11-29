using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Server.Accounting;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Items
{
    public class RaffleEntry
    {
        public RaffleEntry(Mobile from)
        {
            From = from;

            Address = From.NetState?.Address ?? IPAddress.None;

            Date = Core.Now;
        }

        public RaffleEntry(IGenericReader reader, int version)
        {
            switch (version)
            {
                case 3: // HouseRaffleStone version changes
                case 2:
                case 1:
                case 0:
                    {
                        From = reader.ReadEntity<Mobile>();
                        Address = Utility.Intern(reader.ReadIPAddress());
                        Date = reader.ReadDateTime();

                        break;
                    }
            }
        }

        public Mobile From { get; }

        public IPAddress Address { get; }

        public DateTime Date { get; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(From);
            writer.Write(Address);
            writer.Write(Date);
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

    [Flippable(0xEDD, 0xEDE)]
    public class HouseRaffleStone : Item
    {
        private const int EntryLimitPerIP = 4;
        private const int DefaultTicketPrice = 5000;
        private const int MessageHue = 1153;

        public static readonly TimeSpan DefaultDuration = TimeSpan.FromDays(7.0);
        public static readonly TimeSpan ExpirationTime = TimeSpan.FromDays(30.0);

        private static readonly List<HouseRaffleStone> m_AllStones = new();
        private Rectangle2D m_Bounds;
        private TimeSpan m_Duration;
        private Map m_Facet;

        private HouseRaffleRegion m_Region;
        private DateTime m_Started;

        private HouseRaffleState m_State;
        private int m_TicketPrice;

        private Mobile m_Winner;

        [Constructible]
        public HouseRaffleStone()
            : base(0xEDD)
        {
            m_Region = null;
            m_Bounds = new Rectangle2D();
            m_Facet = null;

            m_Winner = null;
            Deed = null;

            m_State = HouseRaffleState.Inactive;
            m_Started = DateTime.MinValue;
            m_Duration = DefaultDuration;
            ExpireAction = HouseRaffleExpireAction.None;
            m_TicketPrice = DefaultTicketPrice;

            Entries = new List<RaffleEntry>();

            Movable = false;

            m_AllStones.Add(this);
        }

        public HouseRaffleStone(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public HouseRaffleState CurrentState
        {
            get => m_State;
            set
            {
                if (m_State != value)
                {
                    if (value == HouseRaffleState.Active)
                    {
                        Entries.Clear();
                        m_Winner = null;
                        Deed = null;
                        m_Started = Core.Now;
                    }

                    m_State = value;
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public Rectangle2D PlotBounds
        {
            get => m_Bounds;
            set
            {
                m_Bounds = value;

                InvalidateRegion();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public Map PlotFacet
        {
            get => m_Facet;
            set
            {
                m_Facet = value;

                InvalidateRegion();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public Mobile Winner
        {
            get => m_Winner;
            set
            {
                m_Winner = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public HouseRaffleDeed Deed { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public DateTime Started
        {
            get => m_Started;
            set
            {
                m_Started = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public TimeSpan Duration
        {
            get => m_Duration;
            set
            {
                m_Duration = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsExpired
        {
            get
            {
                if (m_State != HouseRaffleState.Completed)
                {
                    return false;
                }

                return m_Started + m_Duration + ExpirationTime <= Core.Now;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public HouseRaffleExpireAction ExpireAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public int TicketPrice
        {
            get => m_TicketPrice;
            set
            {
                m_TicketPrice = Math.Max(0, value);
                InvalidateProperties();
            }
        }

        public List<RaffleEntry> Entries { get; private set; }

        public override string DefaultName => "a house raffle stone";

        public override bool DisplayWeight => false;

        public static void CheckEnd_OnTick()
        {
            for (var i = 0; i < m_AllStones.Count; i++)
            {
                m_AllStones[i].CheckEnd();
            }
        }

        public static void Initialize()
        {
            for (var i = m_AllStones.Count - 1; i >= 0; i--)
            {
                var stone = m_AllStones[i];

                if (stone.IsExpired)
                {
                    switch (stone.ExpireAction)
                    {
                        case HouseRaffleExpireAction.HideStone:
                            {
                                if (stone.Visible)
                                {
                                    stone.Visible = false;
                                    stone.ItemID = 0x1B7B; // Non-blocking ItemID
                                }

                                break;
                            }
                        case HouseRaffleExpireAction.DeleteStone:
                            {
                                stone.Delete();
                                break;
                            }
                    }
                }
            }

            Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), CheckEnd_OnTick);
        }

        public bool ValidLocation() =>
            m_Bounds.Start != Point2D.Zero && m_Bounds.End != Point2D.Zero && m_Facet != null &&
            m_Facet != Map.Internal;

        private void InvalidateRegion()
        {
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }

            if (ValidLocation())
            {
                m_Region = new HouseRaffleRegion(this);
                m_Region.Register();
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
            var result = new StringBuilder();

            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                result.AppendFormat(
                    "{0}°{1}'{2},{3}°{4}'{5}",
                    yLat,
                    yMins,
                    ySouth ? "S" : "N",
                    xLong,
                    xMins,
                    xEast ? "E" : "W"
                );
            }
            else
            {
                result.AppendFormat("{0},{1}", loc.X, loc.Y);
            }

            if (displayMap)
            {
                result.AppendFormat(" ({0})", map);
            }

            return result.ToString();
        }

        public Point3D GetPlotCenter()
        {
            var x = m_Bounds.X + m_Bounds.Width / 2;
            var y = m_Bounds.Y + m_Bounds.Height / 2;
            var z = m_Facet?.GetAverageZ(x, y) ?? 0;

            return new Point3D(x, y, z);
        }

        public string FormatLocation()
        {
            if (!ValidLocation())
            {
                return "no location set";
            }

            return FormatLocation(GetPlotCenter(), m_Facet, true);
        }

        public string FormatPrice() => m_TicketPrice == 0 ? "FREE" : $"{m_TicketPrice} gold";

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (ValidLocation())
            {
                list.Add(FormatLocation());
            }

            switch (m_State)
            {
                case HouseRaffleState.Active:
                    {
                        list.Add(1060658, $"{"ticket price"}\t{FormatPrice()}");  // ~1_val~: ~2_val~
                        list.Add(1060659, $"{"ends"}\t{m_Started + m_Duration}"); // ~1_val~: ~2_val~
                        break;
                    }
                case HouseRaffleState.Completed:
                    {
                        list.Add(1060658, $"winner\t{m_Winner?.Name ?? "Unknown"}"); // ~1_val~: ~2_val~
                        break;
                    }
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            switch (m_State)
            {
                case HouseRaffleState.Active:
                    {
                        LabelTo(from, 1060658, $"Ends\t{m_Started + m_Duration}"); // ~1_val~: ~2_val~
                        break;
                    }
                case HouseRaffleState.Completed:
                    {
                        LabelTo(
                            from,
                            1060658, // ~1_val~: ~2_val~
                            $"Winner\t{m_Winner?.Name ?? "Unknown"}"
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

                if (m_State == HouseRaffleState.Inactive)
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
            if (m_State != HouseRaffleState.Active || !from.CheckAlive())
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
                        1150470,
                        0x7F00,
                        $"You are about to purchase a raffle ticket for the house plot located at {FormatLocation()}.  The ticket price is {FormatPrice()}.  Tickets are non-refundable and you can only purchase one ticket per account.  Do you wish to continue?",
                        0xFFFFFF,
                        420,
                        280,
                        okay => Purchase_Callback(from, okay)
                    )
                ); // CONFIRM TICKET PURCHASE
            }
        }

        public void Purchase_Callback(Mobile from, bool okay)
        {
            if (Deleted || m_State != HouseRaffleState.Active || !from.CheckAlive() || HasEntered(from) || IsAtIPLimit(from))
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

                if (m_TicketPrice == 0 || from.Backpack?.ConsumeTotal(typeof(Gold), m_TicketPrice) == true ||
                    Banker.Withdraw(from, m_TicketPrice))
                {
                    Entries.Add(new RaffleEntry(from));

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
            if (m_State != HouseRaffleState.Active || m_Started + m_Duration > Core.Now)
            {
                return;
            }

            m_State = HouseRaffleState.Completed;

            if (m_Region != null && Entries.Count != 0)
            {
                m_Winner = Entries.RandomElement().From;

                if (m_Winner != null)
                {
                    Deed = new HouseRaffleDeed(this, m_Winner);

                    m_Winner.SendMessage(
                        MessageHue,
                        $"Congratulations, {m_Winner.Name}!  You have won the raffle for the plot located at {FormatLocation()}."
                    );

                    if (m_Winner.AddToBackpack(Deed))
                    {
                        m_Winner.SendMessage(MessageHue, "The writ of lease has been placed in your backpack.");
                    }
                    else
                    {
                        m_Winner.BankBox.DropItem(Deed);
                        m_Winner.SendMessage(
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
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }

            m_AllStones.Remove(this);

            base.OnDelete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3); // version

            writer.WriteEncodedInt((int)m_State);
            writer.WriteEncodedInt((int)ExpireAction);

            writer.Write(Deed);

            writer.Write(m_Bounds);
            writer.Write(m_Facet);

            writer.Write(m_Winner);

            writer.Write(m_TicketPrice);
            writer.Write(m_Started);
            writer.Write(m_Duration);

            writer.Write(Entries.Count);

            foreach (var entry in Entries)
            {
                entry.Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_State = (HouseRaffleState)reader.ReadEncodedInt();

                        goto case 2;
                    }
                case 2:
                    {
                        ExpireAction = (HouseRaffleExpireAction)reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        Deed = reader.ReadEntity<HouseRaffleDeed>();

                        goto case 0;
                    }
                case 0:
                    {
                        var oldActive = version < 3 && reader.ReadBool();

                        m_Bounds = reader.ReadRect2D();
                        m_Facet = reader.ReadMap();

                        m_Winner = reader.ReadEntity<Mobile>();

                        m_TicketPrice = reader.ReadInt();
                        m_Started = reader.ReadDateTime();
                        m_Duration = reader.ReadTimeSpan();

                        var entryCount = reader.ReadInt();
                        Entries = new List<RaffleEntry>(entryCount);

                        for (var i = 0; i < entryCount; i++)
                        {
                            var entry = new RaffleEntry(reader, version);

                            if (entry.From == null)
                            {
                                continue; // Character was deleted
                            }

                            Entries.Add(entry);
                        }

                        InvalidateRegion();

                        m_AllStones.Add(this);

                        if (version < 3)
                        {
                            if (oldActive)
                            {
                                m_State = HouseRaffleState.Active;
                            }
                            else if (m_Winner != null)
                            {
                                m_State = HouseRaffleState.Completed;
                            }
                            else
                            {
                                m_State = HouseRaffleState.Inactive;
                            }
                        }

                        break;
                    }
            }
        }

        private class RaffleContextMenuEntry : ContextMenuEntry
        {
            protected readonly Mobile m_From;
            protected readonly HouseRaffleStone m_Stone;

            public RaffleContextMenuEntry(Mobile from, HouseRaffleStone stone, int label)
                : base(label)
            {
                m_From = from;
                m_Stone = stone;
            }
        }

        private class EditEntry : RaffleContextMenuEntry
        {
            public EditEntry(Mobile from, HouseRaffleStone stone)
                : base(from, stone, 5101) // Edit
            {
            }

            public override void OnClick()
            {
                if (m_Stone.Deleted || m_From.AccessLevel < AccessLevel.Seer)
                {
                    return;
                }

                m_From.SendGump(new PropertiesGump(m_From, m_Stone));
            }
        }

        private class ActivateEntry : RaffleContextMenuEntry
        {
            public ActivateEntry(Mobile from, HouseRaffleStone stone)
                : base(from, stone, 5113) // Start
            {
                if (!stone.ValidLocation())
                {
                    Flags |= CMEFlags.Disabled;
                }
            }

            public override void OnClick()
            {
                if (m_Stone.Deleted || m_From.AccessLevel < AccessLevel.Seer || !m_Stone.ValidLocation())
                {
                    return;
                }

                m_Stone.CurrentState = HouseRaffleState.Active;
            }
        }

        private class ManagementEntry : RaffleContextMenuEntry
        {
            public ManagementEntry(Mobile from, HouseRaffleStone stone)
                : base(from, stone, 5032) // Game Monitor
            {
            }

            public override void OnClick()
            {
                if (m_Stone.Deleted || m_From.AccessLevel < AccessLevel.Seer)
                {
                    return;
                }

                m_From.SendGump(new HouseRaffleManagementGump(m_Stone));
            }
        }
    }
}
