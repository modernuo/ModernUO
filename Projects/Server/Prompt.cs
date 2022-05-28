/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Prompt.cs                                                       *
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

namespace Server.Prompts;

public abstract class Prompt
{
    private static int m_Serials;

    public Prompt(string args = "")
    {
        do
        {
            Serial = ++m_Serials;
        } while (Serial == 0);

        MessageArgs = args;
        TypeId = GetType().FullName?.GetHashCode() ?? -1;
    }

    public int Serial { get; }
    public string MessageArgs { get; }
    public virtual int MessageCliloc => 1042971;
    public virtual int MessageHue => 0;
    public virtual int TypeId { get; }

    public virtual void OnCancel(Mobile from)
    {
    }

    public virtual void OnResponse(Mobile from, string text)
    {
    }

    public void SendTo(Mobile m)
    {
        if (m.NetState?.IsEnhancedClient == true)
        {
            m.SendGump(new PromptGump(this, m));
            return;
        }

        if (MessageCliloc != 1042971 || MessageArgs != string.Empty)
        {
            m.SendLocalizedMessage(MessageCliloc, MessageArgs, MessageHue);
        }

        m.NetState.SendPrompt(this);
    }

    public class PromptGump : Gump
    {
        public override int TypeID => 0x2AE;

        public PromptGump(Prompt prompt, Mobile to) : base(0, 0)
        {
            var senderSerial = prompt.Serial;

            Intern("TEXTENTRY");
            Intern(senderSerial.ToString());
            Intern(to.Serial.ToString());
            Intern(prompt.TypeId.ToString());
            Intern(prompt.MessageCliloc.ToString());
            Intern("1"); // 0 = Ascii response, 1 = Unicode Response

            AddBackground(50, 50, 540, 350, 0xA28);

            AddPage(0);

            AddHtmlLocalized(264, 80, 200, 24, 1062524);
            AddHtmlLocalized(120, 108, 420, 48, 1062638);
            AddBackground(100, 148, 440, 200, 0xDAC);
            AddTextEntry(120, 168, 400, 200, 0x0, 44, "TEXTENTRY");
            AddButton(175, 355, 0x81A, 0x81B, 1);
            AddButton(405, 355, 0x819, 0x818, 0);
        }
    }
}
