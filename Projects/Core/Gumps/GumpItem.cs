/***************************************************************************
 *                                GumpItem.cs
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
    public class GumpItem : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tilepic");
        private static readonly byte[] m_LayoutNameHue = Gump.StringToBuffer("tilepichue");

        public GumpItem(int x, int y, int itemID, int hue = 0)
        {
            X = x;
            Y = y;
            ItemID = itemID;
            Hue = hue;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public override string Compile(NetState ns) =>
            Hue == 0 ? $"{{ tilepic {X} {Y} {ItemID} }}" : $"{{ tilepichue {X} {Y} {ItemID} {Hue} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(Hue == 0 ? m_LayoutName : m_LayoutNameHue);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(ItemID);

            if (Hue != 0)
                disp.AppendLayout(Hue);
        }
    }
}
