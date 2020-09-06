/***************************************************************************
 *                               GumpImage.cs
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
    public class GumpImage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("gumppic");
        private static readonly byte[] m_HueEquals = Gump.StringToBuffer(" hue=");

        public GumpImage(int x, int y, int gumpID, int hue = 0)
        {
            X = x;
            Y = y;
            GumpID = gumpID;
            Hue = hue;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int GumpID { get; set; }

        public int Hue { get; set; }

        public override string Compile(NetState ns) =>
            Hue == 0 ? $"{{ gumppic {X} {Y} {GumpID} }}" : $"{{ gumppic {X} {Y} {GumpID} hue={Hue} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(GumpID);

            if (Hue != 0)
            {
                disp.AppendLayout(m_HueEquals);
                disp.AppendLayoutNS(Hue);
            }
        }
    }
}
