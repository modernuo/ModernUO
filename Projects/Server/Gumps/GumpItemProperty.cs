/***************************************************************************
 *                            GumpItemProperty.cs
 *                            -------------------
 *   begin                : May 26, 2013
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
    public class GumpItemProperty : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("itemproperty");

        public GumpItemProperty(uint serial) => Serial = serial;

        public uint Serial { get; set; }

        public override string Compile(NetState ns) => $"{{ itemproperty {Serial} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Serial);
        }
    }
}
