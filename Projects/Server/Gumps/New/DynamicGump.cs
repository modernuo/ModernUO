/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicGump.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Components;
using Server.Gumps.Enums;
using Server.Network;

namespace Server.Gumps
{
    public abstract class DynamicGump : BaseGump
    {
        private readonly int x;
        private readonly int y;
        private readonly GumpFlags flags;

        protected DynamicGump(int x, int y, GumpFlags flags = GumpFlags.None)
        {
            this.x = x;
            this.y = y;
            this.flags = flags;
        }

        public override void SendTo(NetState ns)
        {
            GumpBuilder<StaticStringsHandler> builder = GumpBuilder.ForStaticStrings(flags);

            try
            {
                Build(ref builder);
                ns.AddGump(this);
                builder.Send(ns, Serial, TypeID, x, y, out _switches, out _textEntries);
            }
            finally
            {
                builder.Dispose();
            }
        }

        protected abstract void Build(ref GumpBuilder<StaticStringsHandler> builder);
    }
}
