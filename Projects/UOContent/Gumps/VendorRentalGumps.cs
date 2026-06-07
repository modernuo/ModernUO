using System;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;
using Server.Targeting;

namespace Server.Gumps;

public abstract class BaseVendorRentalGump : DynamicGump
{
    private readonly GumpType _type;
    private readonly VendorRentalDuration _duration;
    private readonly int _price;
    private readonly int _renewalPrice;
    private readonly Mobile _landlord;
    private readonly Mobile _renter;
    private readonly bool _landlordRenew;
    private readonly bool _renterRenew;
    private readonly bool _renew;

    protected BaseVendorRentalGump(
        GumpType type, VendorRentalDuration duration, int price, int renewalPrice,
        Mobile landlord, Mobile renter, bool landlordRenew, bool renterRenew, bool renew
    ) : base(100, 100)
    {
        _type = type;
        _duration = duration;
        _price = price;
        _renewalPrice = renewalPrice;
        _landlord = landlord;
        _renter = renter;
        _landlordRenew = landlordRenew;
        _renterRenew = renterRenew;
        _renew = renew;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_type == GumpType.Offer)
        {
            builder.SetNoClose();
        }

        builder.AddPage();

        builder.AddImage(0, 0, 0x1F40);
        builder.AddImageTiled(20, 37, 300, 308, 0x1F42);
        builder.AddImage(20, 325, 0x1F43);

        builder.AddImage(35, 8, 0x39);
        builder.AddImageTiled(65, 8, 257, 10, 0x3A);
        builder.AddImage(290, 8, 0x3B);

        builder.AddImageTiled(70, 55, 230, 2, 0x23C5);

        builder.AddImage(32, 33, 0x2635);
        builder.AddHtmlLocalized(70, 35, 270, 20, 1062353, 0x1); // Vendor Rental Contract

        builder.AddPage(1);

        if (_type != GumpType.UnlockedContract)
        {
            builder.AddImage(65, 60, 0x827);
            builder.AddHtmlLocalized(79, 58, 270, 20, 1062370, 0x1); // Landlord:
            builder.AddLabel(150, 58, 0x64, _landlord?.Name ?? "");

            builder.AddImageTiled(70, 80, 230, 2, 0x23C5);
        }

        if (_type is GumpType.UnlockedContract or GumpType.LockedContract)
        {
            builder.AddButton(30, 96, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 2);
        }

        builder.AddHtmlLocalized(50, 95, 150, 20, 1062354, 0x1); // Contract Length
        builder.AddHtmlLocalized(230, 95, 270, 20, _duration.Name, 0x1);

        if (_type is GumpType.UnlockedContract or GumpType.LockedContract)
        {
            builder.AddButton(30, 116, 0x15E1, 0x15E5, 1);
        }

        builder.AddHtmlLocalized(50, 115, 150, 20, 1062356, 0x1); // Price Per Rental
        if (_price > 0)
        {
            builder.AddLabel(230, 115, 0x64, $"{_price}");
        }
        else
        {
            builder.AddLabel(230, 115, 0x64, "FREE");
        }

        builder.AddImageTiled(50, 160, 250, 2, 0x23BF);

        if (_type == GumpType.Offer)
        {
            builder.AddButton(67, 180, 0x482, 0x483, 2);
            builder.AddHtmlLocalized(100, 180, 270, 20, 1049011, 0x28); // I accept!

            builder.AddButton(67, 210, 0x47F, 0x480, 0);
            builder.AddHtmlLocalized(100, 210, 270, 20, 1049012, 0x28); // No thanks, I decline.
        }
        else
        {
            builder.AddImage(49, 170, 0x61);
            builder.AddHtmlLocalized(60, 170, 250, 20, 1062355, 0x1); // Renew On Expiration?

            if (_type is GumpType.LockedContract or GumpType.UnlockedContract or GumpType.VendorLandlord)
            {
                builder.AddButton(30, 192, 0x15E1, 0x15E5, 3);
            }

            builder.AddHtmlLocalized(85, 190, 250, 20, 1062359, 0x1);                             // Landlord:
            builder.AddHtmlLocalized(230, 190, 270, 20, _landlordRenew ? 1049717 : 1049718, 0x1); // YES / NO

            if (_type == GumpType.VendorRenter)
            {
                builder.AddButton(30, 212, 0x15E1, 0x15E5, 4);
            }

            builder.AddHtmlLocalized(85, 210, 250, 20, 1062360, 0x1);                           // Renter:
            builder.AddHtmlLocalized(230, 210, 270, 20, _renterRenew ? 1049717 : 1049718, 0x1); // YES / NO

            if (_renew)
            {
                builder.AddImage(49, 233, 0x939);
                builder.AddHtmlLocalized(70, 230, 250, 20, 1062482, 0x1); // Contract WILL renew
            }
            else
            {
                builder.AddImage(49, 233, 0x938);
                builder.AddHtmlLocalized(70, 230, 250, 20, 1062483, 0x1); // Contract WILL NOT renew
            }
        }

        builder.AddImageTiled(30, 283, 257, 30, 0x5D);
        builder.AddImage(285, 283, 0x5E);
        builder.AddImage(20, 288, 0x232C);

        if (_type == GumpType.LockedContract)
        {
            builder.AddButton(67, 295, 0x15E1, 0x15E5, 5);
            builder.AddHtmlLocalized(85, 294, 270, 20, 1062358, 0x28); // Offer Contract To Someone
        }
        else if (_type is GumpType.VendorLandlord or GumpType.VendorRenter)
        {
            if (_type == GumpType.VendorLandlord)
            {
                builder.AddButton(30, 250, 0x15E1, 0x15E1, 6);
            }

            builder.AddHtmlLocalized(85, 250, 250, 20, 1062499, 0x1); // Renewal Price
            builder.AddLabel(230, 250, 0x64, $"{_renewalPrice}");

            builder.AddHtmlLocalized(60, 294, 270, 20, 1062369, 0x1); // Renter:
            builder.AddLabel(120, 293, 0x64, _renter != null ? _renter.Name : "");
        }

        if (_type is GumpType.UnlockedContract or GumpType.LockedContract)
        {
            builder.AddPage(2);

            for (var i = 0; i < VendorRentalDuration.Instances.Length; i++)
            {
                var durationItem = VendorRentalDuration.Instances[i];

                builder.AddButton(30, 76 + i * 20, 0x15E1, 0x15E5, 0x10 | i, GumpButtonType.Reply, 1);
                builder.AddHtmlLocalized(50, 75 + i * 20, 150, 20, durationItem.Name, 0x1);
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (!IsValidResponse(from))
        {
            return;
        }

        if ((info.ButtonID & 0x10) != 0) // Contract duration
        {
            var index = info.ButtonID & 0xF;

            if (index < VendorRentalDuration.Instances.Length)
            {
                SetContractDuration(from, VendorRentalDuration.Instances[index]);
            }

            return;
        }

        switch (info.ButtonID)
        {
            case 1: // Price Per Rental
                {
                    SetPricePerRental(from);
                    break;
                }

            case 2: // Accept offer
                {
                    AcceptOffer(from);
                    break;
                }

            case 3: // Renew on expiration - landlord
                {
                    LandlordRenewOnExpiration(from);
                    break;
                }

            case 4: // Renew on expiration - renter
                {
                    RenterRenewOnExpiration(from);
                    break;
                }

            case 5: // Offer Contract To Someone
                {
                    OfferContract(from);
                    break;
                }

            case 6: // Renewal price
                {
                    SetRenewalPrice(from);
                    break;
                }

            default:
                {
                    Cancel(from);
                    break;
                }
        }
    }

    protected abstract bool IsValidResponse(Mobile from);

    protected virtual void SetContractDuration(Mobile from, VendorRentalDuration duration)
    {
    }

    protected virtual void SetPricePerRental(Mobile from)
    {
    }

    protected virtual void AcceptOffer(Mobile from)
    {
    }

    protected virtual void LandlordRenewOnExpiration(Mobile from)
    {
    }

    protected virtual void RenterRenewOnExpiration(Mobile from)
    {
    }

    protected virtual void OfferContract(Mobile from)
    {
    }

    protected virtual void SetRenewalPrice(Mobile from)
    {
    }

    protected virtual void Cancel(Mobile from)
    {
    }

    protected enum GumpType
    {
        UnlockedContract,
        LockedContract,
        Offer,
        VendorLandlord,
        VendorRenter
    }
}

public class VendorRentalContractGump : BaseVendorRentalGump
{
    private readonly VendorRentalContract _contract;

    private VendorRentalContractGump(VendorRentalContract contract, Mobile from) : base(
        contract.IsLockedDown ? GumpType.LockedContract : GumpType.UnlockedContract,
        contract.Duration,
        contract.Price,
        contract.Price,
        from,
        null,
        contract.LandlordRenew,
        false,
        false
    ) => _contract = contract;

    public static void DisplayTo(Mobile from, VendorRentalContract contract)
    {
        if (from?.NetState != null && contract?.Deleted == false)
        {
            from.SendGump(new VendorRentalContractGump(contract, from), true);
        }
    }

    protected override bool IsValidResponse(Mobile from) => _contract.IsUsableBy(from, true, true, true, true);

    protected override void SetContractDuration(Mobile from, VendorRentalDuration duration)
    {
        _contract.Duration = duration;

        DisplayTo(from, _contract);
    }

    protected override void SetPricePerRental(Mobile from)
    {
        // Please enter the amount of gold that should be charged for this contract (ESC to cancel):
        from.SendLocalizedMessage(1062365);
        from.Prompt = new PricePerRentalPrompt(_contract);
    }

    protected override void LandlordRenewOnExpiration(Mobile from)
    {
        _contract.LandlordRenew = !_contract.LandlordRenew;

        DisplayTo(from, _contract);
    }

    protected override void OfferContract(Mobile from)
    {
        if (_contract.IsLandlord(from))
        {
            from.SendLocalizedMessage(1062371); // Please target the person you wish to offer this contract to.
            from.Target = new OfferContractTarget(_contract);
        }
    }

    private class PricePerRentalPrompt : Prompt
    {
        private readonly VendorRentalContract _contract;

        public PricePerRentalPrompt(VendorRentalContract contract) => _contract = contract;

        public override void OnResponse(Mobile from, string text)
        {
            if (!_contract.IsUsableBy(from, true, true, true, true))
            {
                return;
            }

            if (!int.TryParse(text.AsSpan().Trim(), out var price))
            {
                price = -1;
            }

            if (price < 0)
            {
                from.SendLocalizedMessage(1062485); // Invalid entry.  Rental fee set to 0.
                _contract.Price = 0;
            }
            else if (price > 5000000)
            {
                _contract.Price = 5000000;
            }
            else
            {
                _contract.Price = price;
            }

            DisplayTo(from, _contract);
        }

        public override void OnCancel(Mobile from)
        {
            if (_contract.IsUsableBy(from, true, true, true, true))
            {
                DisplayTo(from, _contract);
            }
        }
    }

    private class OfferContractTarget : Target
    {
        private readonly VendorRentalContract _contract;

        public OfferContractTarget(VendorRentalContract contract) : base(-1, false, TargetFlags.None) =>
            _contract = contract;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!_contract.IsUsableBy(from, true, false, true, true))
            {
                return;
            }

            if (targeted is not Mobile mob || !mob.Player || !mob.Alive || mob == from)
            {
                from.SendLocalizedMessage(1071984); // That is not a valid target for a rental contract!
            }
            else if (!mob.InRange(_contract, 5))
            {
                from.SendLocalizedMessage(501853); // Target is too far away.
            }
            else
            {
                from.SendLocalizedMessage(1062372); // Please wait while that person considers your offer.

                // ~1_NAME~ is offering you a vendor rental.   If you choose to accept this offer, you have 30 seconds to do so.
                mob.SendLocalizedMessage(1062373, from.Name);
                VendorRentalOfferGump.DisplayTo(mob, _contract, from);

                _contract.Offeree = mob;
            }
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            from.SendLocalizedMessage(1062380); // You decide against offering the contract to anyone.
        }
    }
}

public class VendorRentalOfferGump : BaseVendorRentalGump
{
    private readonly VendorRentalContract _contract;
    private readonly Mobile _landlord;

    private VendorRentalOfferGump(VendorRentalContract contract, Mobile landlord) : base(
        GumpType.Offer,
        contract.Duration,
        contract.Price,
        contract.Price,
        landlord,
        null,
        contract.LandlordRenew,
        false,
        false
    )
    {
        _contract = contract;
        _landlord = landlord;
    }

    public static void DisplayTo(Mobile to, VendorRentalContract contract, Mobile landlord)
    {
        if (to?.NetState != null && contract?.Deleted == false && landlord != null)
        {
            to.SendGump(new VendorRentalOfferGump(contract, landlord));
        }
    }

    protected override bool IsValidResponse(Mobile from) =>
        _contract.IsUsableBy(_landlord, true, false, false, false) && from.CheckAlive() && _contract.Offeree == from;

    protected override void AcceptOffer(Mobile from)
    {
        _contract.Offeree = null;

        if (!_contract.Map.CanFit(_contract.Location, 16, false, false))
        {
            _landlord.SendLocalizedMessage(1062486); // A vendor cannot exist at that location.  Please try again.
            return;
        }

        var house = BaseHouse.FindHouseAt(_contract);
        if (house == null)
        {
            return;
        }

        var price = _contract.Price;
        int goldToGive;

        if (price > 0)
        {
            if (Banker.Withdraw(from, price))
            {
                // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                from.SendLocalizedMessage(1060398, price.ToString());

                var depositedGold = Banker.DepositUpTo(_landlord, price);
                goldToGive = price - depositedGold;

                if (depositedGold > 0)
                {
                    // ~1_AMOUNT~ gold has been deposited into your bank box.
                    _landlord.SendLocalizedMessage(1060397, price.ToString());
                }

                if (goldToGive > 0)
                {
                    _landlord.SendLocalizedMessage(500390); // Your bank box is full.
                }
            }
            else
            {
                // You do not have enough gold in your bank account to cover the cost of the contract.
                from.SendLocalizedMessage(1062378);
                _landlord.SendLocalizedMessage(1062374, from.Name); // ~1_NAME~ has declined your vendor rental offer.

                return;
            }
        }
        else
        {
            goldToGive = 0;
        }

        var vendor = new RentedVendor(from, house, _contract.Duration, price, _contract.LandlordRenew, goldToGive);
        vendor.MoveToWorld(_contract.Location, _contract.Map);

        _contract.Delete();

        // You have accepted the offer and now own a vendor in this house.  Rental contract options and details may be viewed on this vendor via the 'Contract Options' context menu.
        from.SendLocalizedMessage(1062377);

        // ~1_NAME~ has accepted your vendor rental offer.  Rental contract details and options may be viewed on this vendor via the 'Contract Options' context menu.
        _landlord.SendLocalizedMessage(1062376, from.Name);
    }

    protected override void Cancel(Mobile from)
    {
        _contract.Offeree = null;

        from.SendLocalizedMessage(1062375);                  // You decline the offer for a vendor space rental.
        _landlord.SendLocalizedMessage(1062374, from.Name); // ~1_NAME~ has declined your vendor rental offer.
    }
}

public class RenterVendorRentalGump : BaseVendorRentalGump
{
    private readonly RentedVendor _vendor;

    private RenterVendorRentalGump(RentedVendor vendor) : base(
        GumpType.VendorRenter,
        vendor.RentalDuration,
        vendor.RentalPrice,
        vendor.RenewalPrice,
        vendor.Landlord,
        vendor.Owner,
        vendor.LandlordRenew,
        vendor.RenterRenew,
        vendor.Renew
    ) => _vendor = vendor;

    public static void DisplayTo(Mobile from, RentedVendor vendor)
    {
        if (from?.NetState != null && vendor?.Deleted == false)
        {
            from.SendGump(new RenterVendorRentalGump(vendor), true);
        }
    }

    protected override bool IsValidResponse(Mobile from) => _vendor.CanInteractWith(from, true);

    protected override void RenterRenewOnExpiration(Mobile from)
    {
        _vendor.RenterRenew = !_vendor.RenterRenew;

        DisplayTo(from, _vendor);
    }
}

public class LandlordVendorRentalGump : BaseVendorRentalGump
{
    private readonly RentedVendor _vendor;

    private LandlordVendorRentalGump(RentedVendor vendor) : base(
        GumpType.VendorLandlord,
        vendor.RentalDuration,
        vendor.RentalPrice,
        vendor.RenewalPrice,
        vendor.Landlord,
        vendor.Owner,
        vendor.LandlordRenew,
        vendor.RenterRenew,
        vendor.Renew
    ) => _vendor = vendor;

    public static void DisplayTo(Mobile from, RentedVendor vendor)
    {
        if (from?.NetState != null && vendor?.Deleted == false)
        {
            from.SendGump(new LandlordVendorRentalGump(vendor), true);
        }
    }

    protected override bool IsValidResponse(Mobile from) =>
        _vendor.CanInteractWith(from, false) && _vendor.IsLandlord(from);

    protected override void LandlordRenewOnExpiration(Mobile from)
    {
        _vendor.LandlordRenew = !_vendor.LandlordRenew;

        DisplayTo(from, _vendor);
    }

    protected override void SetRenewalPrice(Mobile from)
    {
        from.SendLocalizedMessage(1062500); // Enter contract renewal price:

        from.Prompt = new ContractRenewalPricePrompt(_vendor);
    }

    private class ContractRenewalPricePrompt : Prompt
    {
        private readonly RentedVendor _vendor;

        public ContractRenewalPricePrompt(RentedVendor vendor) => _vendor = vendor;

        public override void OnResponse(Mobile from, string text)
        {
            if (!_vendor.CanInteractWith(from, false) || !_vendor.IsLandlord(from))
            {
                return;
            }

            if (!int.TryParse(text.AsSpan().Trim(), out var price))
            {
                price = -1;
            }

            if (price < 0)
            {
                from.SendLocalizedMessage(1062485); // Invalid entry.  Rental fee set to 0.
                _vendor.RenewalPrice = 0;
            }
            else if (price > 5000000)
            {
                _vendor.RenewalPrice = 5000000;
            }
            else
            {
                _vendor.RenewalPrice = price;
            }

            _vendor.RenterRenew = false;

            DisplayTo(from, _vendor);
        }

        public override void OnCancel(Mobile from)
        {
            if (_vendor.CanInteractWith(from, false) && _vendor.IsLandlord(from))
            {
                DisplayTo(from, _vendor);
            }
        }
    }
}

public class VendorRentalRefundGump : StaticGump<VendorRentalRefundGump>
{
    private readonly Mobile _landlord;
    private readonly int _refundAmount;
    private readonly RentedVendor _vendor;

    public override bool Singleton => true;

    private VendorRentalRefundGump(RentedVendor vendor, Mobile landlord, int refundAmount) : base(50, 50)
    {
        _vendor = vendor;
        _landlord = landlord;
        _refundAmount = refundAmount;
    }

    public static void DisplayTo(Mobile to, RentedVendor vendor, Mobile landlord, int refundAmount)
    {
        if (to?.NetState == null || vendor?.Deleted != false || landlord == null)
        {
            return;
        }

        to.SendGump(new VendorRentalRefundGump(vendor, landlord, refundAmount));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 420, 320, 0x13BE);

        builder.AddImageTiled(10, 10, 400, 300, 0xA40);
        builder.AddAlphaRegion(10, 10, 400, 300);

        /* The landlord for this vendor is offering you a partial refund of your rental fee
         * in exchange for immediate termination of your rental contract.<BR><BR>
         *
         * If you accept this offer, the vendor will be immediately dismissed.  You will then
         * be able to claim the inventory and any funds the vendor may be holding for you via
         * a context menu on the house sign for this house.
         */
        builder.AddHtmlLocalized(10, 10, 400, 150, 1062501, 0x7FFF, false, true);

        builder.AddHtmlLocalized(10, 180, 150, 20, 1062508, 0x7FFF); // Vendor Name:
        builder.AddLabelPlaceholder(160, 180, 0x480, "vendorName");

        builder.AddHtmlLocalized(10, 200, 150, 20, 1062509, 0x7FFF); // Shop Name:
        builder.AddLabelPlaceholder(160, 200, 0x480, "shopName");

        builder.AddHtmlLocalized(10, 220, 150, 20, 1062510, 0x7FFF); // Refund Amount:
        builder.AddLabelPlaceholder(160, 220, 0x480, "refundAmount");

        builder.AddButton(10, 268, 0xFA5, 0xFA7, 1);
        builder.AddHtmlLocalized(45, 268, 350, 20, 1062511, 0x7FFF); // Agree, and <strong>dismiss vendor</strong>

        builder.AddButton(10, 288, 0xFA5, 0xFA7, 0);
        builder.AddHtmlLocalized(45, 288, 350, 20, 1062512, 0x7FFF); // No, I want to <strong>keep my vendor</strong>
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetStringSlot("vendorName", _vendor.Name);
        builder.SetStringSlot("shopName", _vendor.ShopName);
        builder.SetStringSlot("refundAmount", $"{_refundAmount}");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (!_vendor.CanInteractWith(from, true) || !_vendor.CanInteractWith(_landlord, false) ||
            !_vendor.IsLandlord(_landlord))
        {
            return;
        }

        if (info.ButtonID != 1)
        {
            _landlord.SendLocalizedMessage(1062513); // The renter declined your offer.
            return;
        }

        if (!Banker.Withdraw(_landlord, _refundAmount))
        {
            _landlord.SendLocalizedMessage(1062507); // You do not have that much money in your bank account.
            return;
        }

        // ~1_AMOUNT~ gold has been withdrawn from your bank box.
        _landlord.SendLocalizedMessage(1060398, _refundAmount.ToString());

        var depositedGold = Banker.DepositUpTo(from, _refundAmount);

        if (depositedGold > 0)
        {
            // ~1_AMOUNT~ gold has been deposited into your bank box.
            from.SendLocalizedMessage(1060397, depositedGold.ToString());
        }

        _vendor.HoldGold += _refundAmount - depositedGold;

        _vendor.Destroy(false);

        from.SendLocalizedMessage(1071990); // Remember to claim your vendor's belongings from the house sign!
    }
}
