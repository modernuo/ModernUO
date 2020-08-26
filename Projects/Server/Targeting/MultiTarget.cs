/***************************************************************************
 *                               MultiTarget.cs
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

namespace Server.Targeting
{
    public abstract class MultiTarget : Target
    {
        protected MultiTarget(
            int multiID, Point3D offset, int range = 10, bool allowGround = true,
            TargetFlags flags = TargetFlags.None
        )
            : base(range, allowGround, flags)
        {
            MultiID = multiID;
            Offset = offset;
        }

        public int MultiID { get; set; }

        public Point3D Offset { get; set; }

        public override Packet GetPacketFor(NetState ns)
        {
            if (ns.HighSeas)
                return new MultiTargetReqHS(this);
            return new MultiTargetReq(this);
        }
    }
}
