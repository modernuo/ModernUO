using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

[Flippable(0xA57, 0xA58, 0xA59)]
[SerializationGenerator(0)]
public partial class Bedroll : Item
{
    [Constructible]
    public Bedroll() : base(0xA57) => Weight = 5.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (Parent != null || !VerifyMove(from))
        {
            return;
        }

        if (!from.InRange(this, 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (ItemID == 0xA57) // rolled
        {
            var dir = PlayerMobile.GetDirection4(from.Location, Location);

            if (dir is Direction.North or Direction.South)
            {
                ItemID = 0xA55;
            }
            else
            {
                ItemID = 0xA56;
            }
        }
        else // unrolled
        {
            ItemID = 0xA57;

            if (!from.HasGump<LogoutGump>())
            {
                var entry = Campfire.GetEntry(from);

                if (entry?.Safe == true)
                {
                    from.SendGump(new LogoutGump(entry, this));
                }
            }
        }
    }

    private class LogoutGump : Gump
    {
        private Bedroll _dedroll;
        private TimerExecutionToken _closeTimerToken;

        private CampfireEntry _entry;

        public LogoutGump(CampfireEntry entry, Bedroll bedroll) : base(100, 0)
        {
            _entry = entry;
            _dedroll = bedroll;

            Timer.StartTimer(TimeSpan.FromSeconds(10.0), CloseGump, out _closeTimerToken);

            AddBackground(0, 0, 400, 350, 0xA28);

            AddHtmlLocalized(100, 20, 200, 35, 1011015); // <center>Logging out via camping</center>

            /* Using a bedroll in the safety of a camp will log you out of the game safely.
             * If this is what you wish to do choose CONTINUE and you will be logged out.
             * Otherwise, select the CANCEL button to avoid logging out at this time.
             * The camp will remain secure for 10 seconds at which time this window will close
             * and you not be logged out.
             */
            AddHtmlLocalized(50, 55, 300, 140, 1011016, true, true);

            AddButton(45, 298, 0xFA5, 0xFA7, 1);
            AddHtmlLocalized(80, 300, 110, 35, 1011011); // CONTINUE

            AddButton(200, 298, 0xFA5, 0xFA7, 0);
            AddHtmlLocalized(235, 300, 110, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var pm = _entry.Player;

            _closeTimerToken.Cancel();

            if (Campfire.GetEntry(pm) != _entry)
            {
                return;
            }

            if (info.ButtonID == 1 && _entry.Safe && _dedroll.Parent == null && _dedroll.IsAccessibleTo(pm)
                && _dedroll.VerifyMove(pm) && _dedroll.Map == pm.Map && pm.InRange(_dedroll, 2))
            {
                pm.PlaceInBackpack(_dedroll);

                pm.BedrollLogout = true;
                sender.Disconnect("Used a bedroll to log out.");
            }

            Campfire.RemoveEntry(_entry);
        }

        private void CloseGump()
        {
            Campfire.RemoveEntry(_entry);
            _entry.Player.CloseGump<LogoutGump>();
        }
    }
}
