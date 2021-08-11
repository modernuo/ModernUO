using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    [Flippable(0xA57, 0xA58, 0xA59)]
    public class Bedroll : Item
    {
        [Constructible]
        public Bedroll() : base(0xA57) => Weight = 5.0;

        public Bedroll(Serial serial) : base(serial)
        {
        }

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

                if (dir == Direction.North || dir == Direction.South)
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }

        private class LogoutGump : Gump
        {
            private readonly Bedroll m_Bedroll;
            private TimerExecutionToken _closeTimerToken;

            private readonly CampfireEntry m_Entry;

            public LogoutGump(CampfireEntry entry, Bedroll bedroll) : base(100, 0)
            {
                m_Entry = entry;
                m_Bedroll = bedroll;

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
                var pm = m_Entry.Player;

                _closeTimerToken.Cancel();

                if (Campfire.GetEntry(pm) != m_Entry)
                {
                    return;
                }

                if (info.ButtonID == 1 && m_Entry.Safe && m_Bedroll.Parent == null && m_Bedroll.IsAccessibleTo(pm)
                    && m_Bedroll.VerifyMove(pm) && m_Bedroll.Map == pm.Map && pm.InRange(m_Bedroll, 2))
                {
                    pm.PlaceInBackpack(m_Bedroll);

                    pm.BedrollLogout = true;
                    sender.Disconnect("Used a bedroll to log out.");
                }

                Campfire.RemoveEntry(m_Entry);
            }

            private void CloseGump()
            {
                Campfire.RemoveEntry(m_Entry);
                m_Entry.Player.CloseGump<LogoutGump>();
            }
        }
    }
}
