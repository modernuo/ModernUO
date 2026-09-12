/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AntiBotTurnstileGump.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps;
using Server.Network;
using System.Diagnostics;

namespace Server.Engines.AntiBot
{
    public class AntiBotTurnstileGump : Gump
    {
        private readonly Mobile _from;
        private readonly string _challengeId;

        public AntiBotTurnstileGump(Mobile from, string challengeId) : base(150, 150)
        {
            _from = from;
            _challengeId = challengeId;

            Closable = false;
            Disposable = false;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 350, 220, 9270);

            AddHtml(20, 20, 310, 25, "<CENTER><B><BASEFONT COLOR=#FFFFFF>Anti-Bot Verification</BASEFONT></B></CENTER>", false, false);
            AddHtml(20, 50, 310, 40, "<CENTER><BASEFONT COLOR=#FFFFFF>Please verify by web browser:</B></BASEFONT></CENTER>", false, false);
            AddHtml(20, 75, 310, 40, $"<CENTER><BASEFONT COLOR=#FFFFFF>{AntiBotSystem.VerificationUrl}?id={challengeId}</B></BASEFONT></CENTER>", false, false);
            AddHtml(20, 100, 310, 50, "<BASEFONT COLOR=#FFFFFF>You have 5 minutes to complete verification.<BR>Cancelling or timing out will disconnect you from the server.</BASEFONT>", false, false);

            AddButton(30, 170, 4005, 4007, 1, GumpButtonType.Reply, 0); // Open Browser
            AddHtml(60, 173, 100, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Open Browser</BASEFONT></CENTER>", false, false);

            AddButton(230, 170, 4017, 4019, 0, GumpButtonType.Reply, 0); // Cancel
            AddHtml(260, 173, 60, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Cancel</BASEFONT></CENTER>", false, false);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender?.Mobile;
            if (from == null)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Open Browser
                    try
                    {
                        var url = $"{AntiBotSystem.VerificationUrl}?id={_challengeId}";
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        _from.SendMessage("Browser opened. Complete verification and return to game.");
                        _from.SendGump(this); // Keep gump open
                    }
                    catch
                    {
                        _from.SendMessage("Could not open browser. Please visit the verification URL manually.");
                        _from.SendGump(this); // Keep gump open
                    }
                    break;
                    
                case 0: // Cancel
                    AntiBotSystem.CancelChallenge(_from);
                    _from.SendMessage("Anti-Bot: Verification cancelled. Disconnecting...");
                    _from.NetState?.Disconnect("Anti-Bot: Verification cancelled.");
                    break;
            }
        }
    }
}