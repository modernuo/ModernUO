using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public class RewardDemolitionGump : DynamicGump
{
    private readonly IAddon _addon;
    private readonly int _question;

    public override bool Singleton => true;

    private RewardDemolitionGump(IAddon addon, int question) : base(150, 50)
    {
        _addon = addon;
        _question = question;
    }

    public static void DisplayTo(Mobile from, IAddon addon, int question)
    {
        if (from?.NetState == null || addon is not Item item || item.Deleted)
        {
            return;
        }

        from.SendGump(new RewardDemolitionGump(addon, question));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 220, 170, 0x13BE);
        builder.AddBackground(10, 10, 200, 150, 0xBB8);

        builder.AddHtmlLocalized(20, 30, 180, 60, _question); // Do you wish to re-deed this decoration?

        builder.AddHtmlLocalized(55, 100, 150, 25, 1011011); // CONTINUE
        builder.AddButton(20, 100, 0xFA5, 0xFA7, (int)Buttons.Confirm);

        builder.AddHtmlLocalized(55, 125, 150, 25, 1011012); // CANCEL
        builder.AddButton(20, 125, 0xFA5, 0xFA7, (int)Buttons.Cancel);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_addon is not Item item || item.Deleted)
        {
            return;
        }

        if (info.ButtonID != (int)Buttons.Confirm)
        {
            return;
        }

        var m = sender.Mobile;
        var house = BaseHouse.FindHouseAt(m);

        if (house?.IsOwner(m) != true)
        {
            // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
            m.SendLocalizedMessage(1049784);
            return;
        }

        if (m.InRange(item.Location, 2))
        {
            var deed = _addon.Deed;

            if (deed != null)
            {
                m.AddToBackpack(deed);
                house.Addons.Remove(item);
                item.Delete();
            }
        }
        else
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    private enum Buttons
    {
        Cancel,
        Confirm
    }
}
