/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MovementPackets.cs - Created: 2020/06/25 - Updated: 2020/06/25  *
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

namespace Server.Network
{
    public sealed class SpeedControl : Packet
    {
        public static readonly Packet WalkSpeed = SetStatic(new SpeedControl(2));
        public static readonly Packet MountSpeed = SetStatic(new SpeedControl(1));
        public static readonly Packet Disable = SetStatic(new SpeedControl(0));

        public SpeedControl(int speedControl) : base(0xBF)
        {
            EnsureCapacity(3);

            Stream.Write((short)0x26);
            Stream.Write((byte)speedControl);
        }
    }

    /// <summary>
    ///     Causes the client to walk in a given direction. It does not send a movement request.
    /// </summary>
    public sealed class MovePlayer : Packet
    {
        public MovePlayer(Direction d) : base(0x97, 2)
        {
            Stream.Write((byte)d);

            // @4C63B0
        }
    }

    public sealed class MovementRej : Packet
    {
        public MovementRej(int seq, Mobile m) : base(0x21, 8)
        {
            Stream.Write((byte)seq);
            Stream.Write((short)m.X);
            Stream.Write((short)m.Y);
            Stream.Write((byte)m.Direction);
            Stream.Write((sbyte)m.Z);
        }
    }

    public sealed class MovementAck : Packet
    {
        private static readonly MovementAck[] m_Cache = new MovementAck[8 * 256];

        private MovementAck(int seq, int noto) : base(0x22, 3)
        {
            Stream.Write((byte)seq);
            Stream.Write((byte)noto);
        }

        public static MovementAck Instantiate(int seq, Mobile m)
        {
            var noto = Notoriety.Compute(m, m);

            var p = m_Cache[noto * seq];

            if (p == null)
            {
                m_Cache[noto * seq] = p = new MovementAck(seq, noto);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class NullFastwalkStack : Packet
    {
        public NullFastwalkStack() : base(0xBF)
        {
            EnsureCapacity(256);
            Stream.Write((short)0x1);
            Stream.Write(0x0);
            Stream.Write(0x0);
            Stream.Write(0x0);
            Stream.Write(0x0);
            Stream.Write(0x0);
            Stream.Write(0x0);
        }
    }
}
