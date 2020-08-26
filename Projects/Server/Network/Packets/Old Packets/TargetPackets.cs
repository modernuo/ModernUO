/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: TargetPackets.cs - Created: 2020/05/26 - Updated: 2020/05/26    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.IO;
using Server.Targeting;

namespace Server.Network
{
    public sealed class MultiTargetReqHS : Packet
    {
        public MultiTargetReqHS(MultiTarget t) : base(0x99, 30)
        {
            Stream.Write(t.AllowGround);
            Stream.Write(t.TargetID);
            Stream.Write((byte)t.Flags);

            Stream.Fill();

            Stream.Seek(18, SeekOrigin.Begin);
            Stream.Write((short)t.MultiID);
            Stream.Write((short)t.Offset.X);
            Stream.Write((short)t.Offset.Y);
            Stream.Write((short)t.Offset.Z);

            // DWORD Hue
        }
    }

    public sealed class MultiTargetReq : Packet
    {
        public MultiTargetReq(MultiTarget t) : base(0x99, 26)
        {
            Stream.Write(t.AllowGround);
            Stream.Write(t.TargetID);
            Stream.Write((byte)t.Flags);

            Stream.Fill();

            Stream.Seek(18, SeekOrigin.Begin);
            Stream.Write((short)t.MultiID);
            Stream.Write((short)t.Offset.X);
            Stream.Write((short)t.Offset.Y);
            Stream.Write((short)t.Offset.Z);
        }
    }

    public sealed class CancelTarget : Packet
    {
        public static readonly Packet Instance = SetStatic(new CancelTarget());

        public CancelTarget() : base(0x6C, 19)
        {
            Stream.Write((byte)0);
            Stream.Write(0);
            Stream.Write((byte)3);
            Stream.Fill();
        }
    }

    public sealed class TargetReq : Packet
    {
        public TargetReq(Target t) : base(0x6C, 19)
        {
            Stream.Write(t.AllowGround);
            Stream.Write(t.TargetID);
            Stream.Write((byte)t.Flags);
            Stream.Fill();
        }
    }
}
