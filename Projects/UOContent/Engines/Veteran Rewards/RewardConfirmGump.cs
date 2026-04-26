using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Engines.VeteranRewards;

public class RewardConfirmGump : DynamicGump
{
    private readonly RewardEntry _entry;
    private readonly Mobile _from;

    public override bool Singleton => true;

    private RewardConfirmGump(Mobile from, RewardEntry entry) : base(0, 0)
    {
        _from = from;
        _entry = entry;
    }

    public static void DisplayTo(Mobile from, RewardEntry entry)
    {
        if (from?.NetState == null || entry == null)
        {
            return;
        }

        from.SendGump(new RewardConfirmGump(from, entry));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 500, 300, 2600);

        builder.AddHtmlLocalized(30, 55, 300, 35, 1006000); // You have selected:

        if (_entry.NameString != null)
        {
            builder.AddHtml(335, 55, 150, 35, _entry.NameString);
        }
        else
        {
            builder.AddHtmlLocalized(335, 55, 150, 35, _entry.Name);
        }

        builder.AddHtmlLocalized(30, 95, 300, 35, 1006001); // This will be assigned to this character:
        builder.AddLabel(335, 95, 0, _from.RawName);

        // Are you sure you wish to select this reward for this character?
        // You will not be able to transfer this reward to another character on another shard.
        // Click 'ok' below to confirm your selection or 'cancel' to go back to the selection screen.
        builder.AddHtmlLocalized(35, 160, 450, 90, 1006002, true, true);

        builder.AddButton(60, 265, 4005, 4007, 1);
        builder.AddHtmlLocalized(95, 266, 150, 35, 1006044); // Ok

        builder.AddButton(295, 265, 4017, 4019, 0);
        builder.AddHtmlLocalized(330, 266, 150, 35, 1006045); // Cancel
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            if (!RewardSystem.HasAccess(_from, _entry))
            {
                return;
            }

            var item = _entry.Construct();

            if (item != null)
            {
                if (item is RedSoulstone soulstone)
                {
                    soulstone.Account = _from.Account.Username;
                }

                if (RewardSystem.ConsumeRewardPoint(_from))
                {
                    _from.AddToBackpack(item);
                }
                else
                {
                    item.Delete();
                }
            }
        }

        RewardSystem.ComputeRewardInfo(_from, out var cur, out var max);

        if (cur < max)
        {
            RewardNoticeGump.DisplayTo(_from);
        }
    }
}
