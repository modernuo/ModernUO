/***************************************************************************
 *                               GumpLabel.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using Server.Network;

namespace Server.Gumps
{
    public class GumpLabel : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("text");

        public GumpLabel(int x, int y, int hue, string text)
        {
            X = x;
            Y = y;
            Hue = hue;
            Text = text;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Hue { get; set; }

        public string Text { get; set; }

        public override string Compile(NetState ns) => $"{{ text {X} {Y} {Hue} {Parent.Intern(Text)} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Hue);
            disp.AppendLayout(Parent.Intern(Text));
        }
    }
}
