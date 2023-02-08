using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Gumps;
using Server.Misc;
using Server.Multis;
using Server.Prompts;

namespace Server.Mobiles;

public class VendorRentalDuration
{
    public static readonly VendorRentalDuration[] Instances =
    {
        new(TimeSpan.FromDays(7.0), 1062361),  // 1 Week
        new(TimeSpan.FromDays(14.0), 1062362), // 2 Weeks
        new(TimeSpan.FromDays(21.0), 1062363), // 3 Weeks
        new(TimeSpan.FromDays(28.0), 1062364)  // 1 Month
    };

    private VendorRentalDuration(TimeSpan duration, int name)
    {
        Duration = duration;
        Name = name;
    }

    public TimeSpan Duration { get; }

    public int Name { get; }

    public int ID
    {
        get
        {
            for (var i = 0; i < Instances.Length; i++)
            {
                if (Instances[i] == this)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}

[SerializationGenerator(0)]
public partial class RentedVendor : PlayerVendor
{
    private Timer _rentalExpireTimer;

    public RentedVendor(
        Mobile owner, BaseHouse house, VendorRentalDuration duration, int rentalPrice,
        bool landlordRenew, int rentalGold
    ) : base(owner, house)
    {
        RentalDuration = duration;
        RentalPrice = RenewalPrice = rentalPrice;
        LandlordRenew = landlordRenew;
        RenterRenew = false;

        RentalGold = rentalGold;

        RentalExpireTime = Core.Now + duration.Duration;
        _rentalExpireTimer = new RentalExpireTimer(this, duration.Duration);
        _rentalExpireTimer.Start();
    }

    public VendorRentalDuration RentalDuration { get; private set; }

    [SerializableField(0, getter: "private", setter: "private")]
    private int _rentalDurationId; // TODO: Replace this with something more robust

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _rentalPrice;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _landlordRenew;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _renterRenew;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _renewalPrice;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _rentalGold;

    [DeltaDateTime]
    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _rentalExpireTime;

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Landlord => House?.Owner;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Renew => LandlordRenew && RenterRenew && House != null && House.DecayType != DecayType.Condemned;

    public override bool IsOwner(Mobile m) => m == Owner || m.AccessLevel >= AccessLevel.GameMaster ||
                                              Core.ML && AccountHandler.CheckAccount(m, Owner);

    public bool IsLandlord(Mobile m) => House?.IsOwner(m) == true;

    public void ComputeRentalExpireDelay(out int days, out int hours)
    {
        var delay = RentalExpireTime - Core.Now;

        if (delay <= TimeSpan.Zero)
        {
            days = 0;
            hours = 0;
        }
        else
        {
            days = delay.Days;
            hours = delay.Hours;
        }
    }

    public void SendRentalExpireMessage(Mobile to)
    {
        ComputeRentalExpireDelay(out var days, out var hours);

        to.SendLocalizedMessage(
            1062464,
            $"{days}\t{hours}"
        ); // The rental contract on this vendor will expire in ~1_DAY~ day(s) and ~2_HOUR~ hour(s).
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _rentalExpireTimer.Stop();
    }

    public override void Destroy(bool toBackpack)
    {
        if (RentalGold > 0 && House?.IsAosRules == true)
        {
            House.MovingCrate ??= new MovingCrate(House);

            Banker.Deposit(House.MovingCrate, RentalGold);
            RentalGold = 0;
        }

        base.Destroy(toBackpack);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        if (from.Alive)
        {
            if (IsOwner(from))
            {
                list.Add(new ContractOptionsEntry(this));
            }
            else if (IsLandlord(from))
            {
                if (RentalGold > 0)
                {
                    list.Add(new CollectRentEntry(this));
                }

                list.Add(new TerminateContractEntry(this));
                list.Add(new ContractOptionsEntry(this));
            }
        }

        base.GetContextMenuEntries(from, list);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var index = Math.Clamp(_rentalDurationId, 0, VendorRentalDuration.Instances.Length - 1);
        RentalDuration = VendorRentalDuration.Instances[index];

        var delay = _rentalExpireTime - Core.Now;
        _rentalExpireTimer = new RentalExpireTimer(this, delay > TimeSpan.Zero ? delay : TimeSpan.Zero).Start();
    }

    private class ContractOptionsEntry : ContextMenuEntry
    {
        private readonly RentedVendor m_Vendor;

        public ContractOptionsEntry(RentedVendor vendor) : base(6209) => m_Vendor = vendor;

        public override void OnClick()
        {
            var from = Owner.From;

            if (m_Vendor.Deleted || !from.CheckAlive())
            {
                return;
            }

            if (m_Vendor.IsOwner(from))
            {
                from.CloseGump<RenterVendorRentalGump>();
                from.SendGump(new RenterVendorRentalGump(m_Vendor));

                m_Vendor.SendRentalExpireMessage(from);
            }
            else if (m_Vendor.IsLandlord(from))
            {
                from.CloseGump<LandlordVendorRentalGump>();
                from.SendGump(new LandlordVendorRentalGump(m_Vendor));

                m_Vendor.SendRentalExpireMessage(from);
            }
        }
    }

    private class CollectRentEntry : ContextMenuEntry
    {
        private readonly RentedVendor m_Vendor;

        public CollectRentEntry(RentedVendor vendor) : base(6212) => m_Vendor = vendor;

        public override void OnClick()
        {
            var from = Owner.From;

            if (m_Vendor.Deleted || !from.CheckAlive() || !m_Vendor.IsLandlord(from))
            {
                return;
            }

            if (m_Vendor.RentalGold > 0)
            {
                var depositedGold = Banker.DepositUpTo(from, m_Vendor.RentalGold);
                m_Vendor.RentalGold -= depositedGold;

                if (depositedGold > 0)
                {
                    from.SendLocalizedMessage(
                        1060397,
                        depositedGold.ToString()
                    ); // ~1_AMOUNT~ gold has been deposited into your bank box.
                }

                if (m_Vendor.RentalGold > 0)
                {
                    from.SendLocalizedMessage(500390); // Your bank box is full.
                }
            }
        }
    }

    private class TerminateContractEntry : ContextMenuEntry
    {
        private readonly RentedVendor m_Vendor;

        public TerminateContractEntry(RentedVendor vendor) : base(6218) => m_Vendor = vendor;

        public override void OnClick()
        {
            var from = Owner.From;

            if (m_Vendor.Deleted || !from.CheckAlive() || !m_Vendor.IsLandlord(from))
            {
                return;
            }

            from.SendLocalizedMessage(
                1062503
            ); // Enter the amount of gold you wish to offer the renter in exchange for immediate termination of this contract?
            from.Prompt = new RefundOfferPrompt(m_Vendor);
        }
    }

    private class RefundOfferPrompt : Prompt
    {
        private readonly RentedVendor m_Vendor;

        public RefundOfferPrompt(RentedVendor vendor) => m_Vendor = vendor;

        public override void OnResponse(Mobile from, string text)
        {
            if (!m_Vendor.CanInteractWith(from, false) || !m_Vendor.IsLandlord(from))
            {
                return;
            }

            text = text.Trim();

            if (!int.TryParse(text, out var amount))
            {
                amount = -1;
            }

            var owner = m_Vendor.Owner;
            if (owner == null)
            {
                return;
            }

            if (amount < 0)
            {
                from.SendLocalizedMessage(1062506); // You did not enter a valid amount.  Offer canceled.
            }
            else if (Banker.GetBalance(from) < amount)
            {
                from.SendLocalizedMessage(1062507); // You do not have that much money in your bank account.
            }
            else if (owner.Map != m_Vendor.Map || !owner.InRange(m_Vendor, 5))
            {
                from.SendLocalizedMessage(
                    1062505
                ); // The renter must be closer to the vendor in order for you to make this offer.
            }
            else
            {
                from.SendLocalizedMessage(1062504); // Please wait while the renter considers your offer.

                owner.CloseGump<VendorRentalRefundGump>();
                owner.SendGump(new VendorRentalRefundGump(m_Vendor, from, amount));
            }
        }
    }

    private class RentalExpireTimer : Timer
    {
        private readonly RentedVendor m_Vendor;

        public RentalExpireTimer(RentedVendor vendor, TimeSpan delay) : base(delay, vendor.RentalDuration.Duration)
        {
            m_Vendor = vendor;
        }

        protected override void OnTick()
        {
            var renewalPrice = m_Vendor.RenewalPrice;

            if (m_Vendor.Renew && m_Vendor.HoldGold >= renewalPrice)
            {
                m_Vendor.HoldGold -= renewalPrice;
                m_Vendor.RentalGold += renewalPrice;

                m_Vendor.RentalPrice = renewalPrice;

                m_Vendor.RentalExpireTime = Core.Now + m_Vendor.RentalDuration.Duration;
            }
            else
            {
                m_Vendor.Destroy(false);
            }
        }
    }
}
