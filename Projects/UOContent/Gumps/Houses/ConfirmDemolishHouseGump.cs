using Server.Guilds;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public class ConfirmDemolishHouseGump : StaticGump<ConfirmDemolishHouseGump>
{
    private readonly BaseHouse _house;

    public override bool Singleton => true;

    public ConfirmDemolishHouseGump(BaseHouse house) : base(110, 100) => _house = house;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

        builder.AddBackground(0, 0, 420, 280, 5054);

        builder.AddImageTiled(10, 10, 400, 20, 2624);
        builder.AddAlphaRegion(10, 10, 400, 20);

        builder.AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720); // <CENTER>WARNING</CENTER>

        builder.AddImageTiled(10, 40, 400, 200, 2624);
        builder.AddAlphaRegion(10, 40, 400, 200);

        /*
         * You are about to demolish your house.
         * You will be refunded the house's value directly to your bank box.
         * All items in the house will remain behind and can be freely picked up by anyone.
         * Once the house is demolished, anyone can attempt to place a new house on the vacant land.
         * This action will not un-condemn any other houses on your account, nor will it end your 7-day waiting period (if it applies to you).
         * Are you sure you wish to continue?
        */
        builder.AddHtmlLocalized(10, 40, 400, 200, 1061795, 32512, false, true);

        builder.AddImageTiled(10, 250, 400, 20, 2624);
        builder.AddAlphaRegion(10, 250, 400, 20);

        builder.AddButton(10, 250, 4005, 4007, 1);
        builder.AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767); // OKAY

        builder.AddButton(210, 250, 4005, 4007, 0);
        builder.AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767); // CANCEL
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID != 1 || _house.Deleted)
        {
            return;
        }

        var from = state.Mobile;

        if (!_house.IsOwner(from))
        {
            from.SendLocalizedMessage(501320); // Only the house owner may do this.
            return;
        }

        if (_house.MovingCrate != null || _house.InternalizedVendors.Count > 0)
        {
            return;
        }

        if (!Guild.NewGuildSystem && _house.FindGuildstone() != null)
        {
            from.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
            return;
        }

        if (_house.HasRentedVendors && _house.VendorInventories.Count > 0)
        {
            // You cannot do that that while you still have contract vendors or unclaimed contract vendor inventory in your house.
            from.SendLocalizedMessage(1062679);
            return;
        }

        if (_house.HasRentedVendors)
        {
            // You cannot do that that while you still have contract vendors in your house.
            from.SendLocalizedMessage(1062680);
            return;
        }

        if (_house.VendorInventories.Count > 0)
        {
            // You cannot do that that while you still have unclaimed contract vendor inventory in your house.
            from.SendLocalizedMessage(1062681);
            return;
        }

        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            from.SendMessage("You do not get a refund for your house as you are not a player");
        }
        else if (_house.IsAosRules && _house.Price > 0)
        {
            if (Banker.Deposit(state.Mobile, _house.Price))
            {
                // ~1_AMOUNT~ gold has been deposited into your bank box.
                from.SendLocalizedMessage(1060397, $"{_house.Price:#,0}");
            }
            else
            {
                from.SendLocalizedMessage(500390); // Your bank box is full.
                return;
            }
        }
        else
        {
            var deed = _house.GetDeed();
            if (deed == null)
            {
                from.SendMessage("Unable to refund house.");
                return;
            }

            if (!from.BankBox.TryDropItem(from, deed, false))
            {
                deed.Delete();
                from.SendLocalizedMessage(500390); // Your bank box is full.
                return;
            }
        }

        _house.RemoveKeys(from);
        _house.Delete();
    }
}
