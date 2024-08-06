using Server.Guilds;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public class ConfirmHouseResize : StaticGump<ConfirmHouseResize>
{
    private readonly BaseHouse _house;
    private readonly Mobile _mobile;

    public ConfirmHouseResize(Mobile mobile, BaseHouse house) : base(110, 100)
    {
        _mobile = mobile;
        _house = house;

        mobile.CloseGump<ConfirmHouseResize>();
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder) {
        builder.SetNoClose();

        builder.AddPage(0);

        builder.AddBackground(0, 0, 420, 280, 0x13BE);
        builder.AddImageTiled(10, 10, 400, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 400, 20);
        builder.AddHtmlLocalized(10, 10, 400, 20, 1060635, 0x7800); // <CENTER>WARNING</CENTER>
        builder.AddImageTiled(10, 40, 400, 200, 0xA40);
        builder.AddAlphaRegion(10, 40, 400, 200);

        /* You are attempting to resize your house. You will be refunded the house's
        value directly to your bank box. All items in the house will *remain behind*
        and can be *freely picked up by anyone*. Once the house is demolished, however,
        only this account will be able to place on the land for one hour. This *will*
        circumvent the normal 7-day waiting period (if it applies to you). This action
        will not un-condemn any other houses on your account. If you have other,
        grandfathered houses, this action *WILL* condemn them. Are you sure you wish
        to continue?*/
        builder.AddHtmlLocalized(10, 40, 400, 200, 1080196, 0x7F00, false, true);

        builder.AddImageTiled(10, 250, 400, 20, 0xA40);
        builder.AddAlphaRegion(10, 250, 400, 20);
        builder.AddButton(10, 250, 0xFA5, 0xFA7, 1);
        builder.AddButton(210, 250, 0xFA5, 0xFA7, 0);
        builder.AddHtmlLocalized(40, 250, 170, 20, 1011036, 0x7FFF);  // OKAY
        builder.AddHtmlLocalized(240, 250, 170, 20, 1011012, 0x7FFF); // CANCEL
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && !_house.Deleted)
        {
            if (_house.IsOwner(_mobile))
            {
                if (_house.MovingCrate != null || _house.InternalizedVendors.Count > 0)
                {
                    _mobile.SendLocalizedMessage(
                        1080455
                    ); // You can not resize your house at this time. Please remove all items fom the moving crate and try again.
                    return;
                }

                if (!Guild.NewGuildSystem && _house.FindGuildstone() != null)
                {
                    _mobile.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                    return;
                }

                /*else if (_house.PlayerVendors.Count > 0)
                {
                  _mobile.SendLocalizedMessage( 503236 ); // You need to collect your vendor's belongings before moving.
                  return;
                }*/
                if (_house.HasRentedVendors && _house.VendorInventories.Count > 0)
                {
                    _mobile.SendLocalizedMessage(
                        1062679
                    ); // You cannot do that that while you still have contract vendors or unclaimed contract vendor inventory in your house.
                    return;
                }

                if (_house.HasRentedVendors)
                {
                    _mobile.SendLocalizedMessage(
                        1062680
                    ); // You cannot do that that while you still have contract vendors in your house.
                    return;
                }

                if (_house.VendorInventories.Count > 0)
                {
                    _mobile.SendLocalizedMessage(
                        1062681
                    ); // You cannot do that that while you still have unclaimed contract vendor inventory in your house.
                    return;
                }

                if (_mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    _mobile.SendMessage("You do not get a refund for your house as you are not a player");
                    _house.RemoveKeys(_mobile);
                    new TempNoHousingRegion(_house, _mobile);
                    _house.Delete();
                }
                else
                {
                    Item toGive;

                    if (_house.IsAosRules)
                    {
                        if (_house.Price > 0)
                        {
                            toGive = new BankCheck(_house.Price);
                        }
                        else
                        {
                            toGive = _house.GetDeed();
                        }
                    }
                    else
                    {
                        toGive = _house.GetDeed();

                        if (toGive == null && _house.Price > 0)
                        {
                            toGive = new BankCheck(_house.Price);
                        }
                    }

                    if (toGive != null)
                    {
                        var box = _mobile.BankBox;

                        if (box.TryDropItem(_mobile, toGive, false))
                        {
                            if (toGive is BankCheck check)
                            {
                                _mobile.SendLocalizedMessage(
                                    1060397,
                                    check.Worth.ToString()
                                ); // ~1_AMOUNT~ gold has been deposited into your bank box.
                            }

                            _house.RemoveKeys(_mobile);
                            new TempNoHousingRegion(_house, _mobile);
                            _house.Delete();
                        }
                        else
                        {
                            toGive.Delete();
                            _mobile.SendLocalizedMessage(500390); // Your bank box is full.
                        }
                    }
                    else
                    {
                        _mobile.SendMessage("Unable to refund house.");
                    }
                }
            }
            else
            {
                _mobile.SendLocalizedMessage(501320); // Only the house owner may do this.
            }
        }
        else if (info.ButtonID == 0)
        {
            _mobile.CloseGump<ConfirmHouseResize>();
            _mobile.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Customize, _mobile, _house));
        }
    }
}
