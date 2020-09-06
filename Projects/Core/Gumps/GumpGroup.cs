/***************************************************************************
 *                                GumpGroup.cs
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
    public class GumpGroup : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("group");

        public GumpGroup(int group) => Group = group;

        public int Group { get; set; }

        public override string Compile(NetState ns) => $"{{ group {Group} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Group);
        }
    }
}
