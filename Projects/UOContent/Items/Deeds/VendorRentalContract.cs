using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class VendorRentalContract : Item
{
    [SerializableField(0, getter: "private", setter: "private")]
    private int _rentalDurationId; // TODO: Replace this with something more robust

    private VendorRentalDuration _duration;

    private Mobile _offeree;
    private Timer _offerExpireTimer;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _price;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _landlordRenew;

    [Constructible]
    public VendorRentalContract() : base(0x14F0)
    {
        Weight = 1.0;
        Hue = 0x672;

        _duration = VendorRentalDuration.Instances[0];
        Price = 1500;
    }

    public override int LabelNumber => 1062332; // a vendor rental contract

    public VendorRentalDuration Duration
    {
        get => _duration;
        set
        {
            if (value != null)
            {
                _duration = value;
                _rentalDurationId = _duration.ID;
            }
        }
    }

    public Mobile Offeree
    {
        get => _offeree;
        set
        {
            if (_offerExpireTimer != null)
            {
                _offerExpireTimer.Stop();
                _offerExpireTimer = null;
            }

            _offeree = value;

            if (value != null)
            {
                _offerExpireTimer = new OfferExpireTimer(this);
                _offerExpireTimer.Start();
            }

            InvalidateProperties();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Offeree != null)
        {
            list.Add(1062368, Offeree.Name); // Being Offered To ~1_NAME~
        }
    }

    public bool IsLandlord(Mobile m)
    {
        if (IsLockedDown)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house != null && house.DecayType != DecayType.Condemned)
            {
                return house.IsOwner(m);
            }
        }

        return false;
    }

    public bool IsUsableBy(Mobile from, bool byLandlord, bool byBackpack, bool noOfferee, bool sendMessage)
    {
        if (Deleted || !from.CheckAlive(sendMessage))
        {
            return false;
        }

        if (noOfferee && Offeree != null)
        {
            if (sendMessage)
            {
                from.SendLocalizedMessage(1062343); // That item is currently in use.
            }

            return false;
        }

        if (byBackpack && IsChildOf(from.Backpack))
        {
            return true;
        }

        if (byLandlord && IsLandlord(from))
        {
            if (from.Map != Map || !from.InRange(this, 5))
            {
                if (sendMessage)
                {
                    from.SendLocalizedMessage(501853); // Target is too far away.
                }

                return false;
            }

            return true;
        }

        return false;
    }

    public override void OnDelete()
    {
        if (IsLockedDown)
        {
            var house = BaseHouse.FindHouseAt(this);

            house?.VendorRentalContracts.Remove(this);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Offeree != null)
        {
            from.SendLocalizedMessage(1062343); // That item is currently in use.
        }
        else if (!IsLockedDown)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                return;
            }

            var house = BaseHouse.FindHouseAt(from);

            if (house?.IsOwner(from) != true)
            {
                from.SendLocalizedMessage(
                    1062333
                ); // You must be standing inside of a house that you own to make use of this contract.
            }
            else if (!house.IsAosRules)
            {
                from.SendMessage("Rental contracts can only be placed in AOS-enabled houses.");
            }
            else if (!house.Public)
            {
                from.SendLocalizedMessage(1062335); // Rental contracts can only be placed in public houses.
            }
            else if (!house.CanPlaceNewVendor())
            {
                from.SendLocalizedMessage(1062352); // You do not have enough storage available to place this contract.
            }
            else
            {
                from.SendLocalizedMessage(1062337); // Target the exact location you wish to rent out.
                from.Target = new RentTarget(this);
            }
        }
        else if (IsLandlord(from))
        {
            if (from.InRange(this, 5))
            {
                from.CloseGump<VendorRentalContractGump>();
                from.SendGump(new VendorRentalContractGump(this, from));
            }
            else
            {
                from.SendLocalizedMessage(501853); // Target is too far away.
            }
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (IsUsableBy(from, true, true, true, false))
        {
            list.Add(new ContractOptionEntry(this));
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var index = Math.Clamp(_rentalDurationId, 0, VendorRentalDuration.Instances.Length);
        _duration = VendorRentalDuration.Instances[index];
    }

    private class ContractOptionEntry : ContextMenuEntry
    {
        private readonly VendorRentalContract m_Contract;

        public ContractOptionEntry(VendorRentalContract contract) : base(6209) => m_Contract = contract;

        public override void OnClick()
        {
            var from = Owner.From;

            if (m_Contract.IsUsableBy(from, true, true, true, true))
            {
                from.CloseGump<VendorRentalContractGump>();
                from.SendGump(new VendorRentalContractGump(m_Contract, from));
            }
        }
    }

    private class RentTarget : Target
    {
        private readonly VendorRentalContract m_Contract;

        public RentTarget(VendorRentalContract contract) : base(-1, false, TargetFlags.None) => m_Contract = contract;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!m_Contract.IsUsableBy(from, false, true, true, true))
            {
                return;
            }

            if (targeted is not IPoint3D location)
            {
                return;
            }

            var pLocation = new Point3D(location);
            var map = from.Map;

            var house = BaseHouse.FindHouseAt(pLocation, map, 0);

            if (house?.IsOwner(from) != true)
            {
                from.SendLocalizedMessage(1062338); // The location being rented out must be inside of your house.
            }
            else if (BaseHouse.FindHouseAt(from) != house)
            {
                // You must be located inside of the house in which you are trying to place the contract.
                from.SendLocalizedMessage(1062339);
            }
            else if (!house.IsAosRules)
            {
                from.SendMessage("Rental contracts can only be placed in AOS-enabled houses.");
            }
            else if (!house.Public)
            {
                from.SendLocalizedMessage(1062335); // Rental contracts can only be placed in public houses.
            }
            else if (house.DecayType == DecayType.Condemned)
            {
                from.SendLocalizedMessage(1062468); // You cannot place a contract in a condemned house.
            }
            else if (!house.CanPlaceNewVendor())
            {
                from.SendLocalizedMessage(1062352); // You do not have enought storage available to place this contract.
            }
            else if (!map.CanFit(pLocation, 16, false, false))
            {
                from.SendLocalizedMessage(1062486); // A vendor cannot exist at that location.  Please try again.
            }
            else
            {
                BaseHouse.IsThereVendor(pLocation, map, out var vendor, out var contract);

                if (vendor)
                {
                    // You may not place a rental contract at this location while other beings occupy it.
                    from.SendLocalizedMessage(1062342);
                }
                else if (contract)
                {
                    // That location is cluttered.  Please clear out any objects there and try again.
                    from.SendLocalizedMessage(1062341);
                }
                else
                {
                    m_Contract.MoveToWorld(pLocation, map);

                    if (!house.LockDown(from, m_Contract))
                    {
                        from.AddToBackpack(m_Contract);
                    }
                }
            }
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            from.SendLocalizedMessage(1062336); // You decide not to place the contract at this time.
        }
    }

    private class OfferExpireTimer : Timer
    {
        private readonly VendorRentalContract m_Contract;

        public OfferExpireTimer(VendorRentalContract contract) : base(TimeSpan.FromSeconds(30.0))
        {
            m_Contract = contract;
        }

        protected override void OnTick()
        {
            var offeree = m_Contract.Offeree;

            if (offeree != null)
            {
                offeree.CloseGump<VendorRentalOfferGump>();

                m_Contract.Offeree = null;
            }
        }
    }
}
