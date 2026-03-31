using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.PlayerMurderSystem;

public class BountyReportMurdererGump : DynamicGump
{
    private readonly List<Mobile> _killers;
    private readonly PlayerMobile _victim;
    private int _idx;

    public BountyReportMurdererGump(PlayerMobile victim, List<Mobile> killers, int idx = 0) : base(0, 0)
    {
        _victim = victim;
        _killers = killers;
        _idx = idx;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();
        builder.SetNoResize();

        builder.AddBackground(265, 205, 393, 270, 70000);
        builder.AddImage(265, 205, 1140);

        builder.AddPage();

        builder.AddHtml(325, 255, 300, 60,
            $"<BIG>Would you like to report {_killers[_idx].Name} as a murderer?</BIG>");

        var bountyMax = Banker.GetBalance(_victim);

        if (_killers[_idx].Kills >= 4 && bountyMax > 0)
        {
            builder.AddHtml(325, 325, 300, 60, $"<BIG>Optional Bounty: [{bountyMax} max] </BIG>");
            builder.AddImage(323, 343, 0x475);
            builder.AddTextEntry(329, 346, 311, 16, 0, 1);
        }

        builder.AddButton(385, 395, 0x47B, 0x47D, 1);
        builder.AddButton(465, 395, 0x478, 0x47A, 2);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = (PlayerMobile)state.Mobile;

        if (info.ButtonID == 1)
        {
            var killer = _killers[_idx];

            if (PlayerMurderSystem.ReportMurder(from, killer) && killer is PlayerMobile pk)
            {
                var text = info.GetTextEntry(1);
                if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text, out var requested) && requested > 0)
                {
                    var bounty = Math.Min(requested, Banker.GetBalance(from));
                    if (bounty > 0 && Banker.Withdraw(from, bounty))
                    {
                        PlayerMurderSystem.AddBounty(pk, bounty);

                        pk.SendMessage(
                            $"{from.Name} has placed a bounty of {bounty} {(bounty == 1 ? "gold piece" : "gold pieces")} on your head!"
                        );

                        from.SendMessage($"You place a bounty of {bounty}gp on {pk.Name}'s head.");
                    }
                }
            }
        }

        _idx++;
        if (_idx < _killers.Count)
        {
            from.SendGump(new BountyReportMurdererGump(from, _killers, _idx));
        }
    }
}
