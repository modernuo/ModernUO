/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MobilePackets.cs - Created: 2020/05/07 - Updated: 2020/05/26    *
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
  public sealed class DeathAnimation : Packet
  {
    public DeathAnimation(Mobile killed, Item corpse) : base(0xAF, 13)
    {
      Stream.Write(killed.Serial);
      Stream.Write(corpse?.Serial ?? Serial.Zero);
      Stream.Write(0);
    }
  }

  public sealed class BondedStatus : Packet
  {
    public BondedStatus(Serial serial, bool bonded) : base(0xBF)
    {
      EnsureCapacity(11);

      Stream.Write((short)0x19);
      Stream.Write((byte)0);
      Stream.Write(serial);
      Stream.Write((byte)(bonded ? 1 : 0));
    }
  }

  public sealed class MobileMoving : Packet
  {
    public MobileMoving(Mobile m, int noto) : base(0x77, 17)
    {
      var loc = m.Location;

      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((short)loc.m_X);
      Stream.Write((short)loc.m_Y);
      Stream.Write((sbyte)loc.m_Z);
      Stream.Write((byte)m.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetPacketFlags());
      Stream.Write((byte)noto);
    }
  }

  // Pre-7.0.0.0 Mobile Moving
  public sealed class MobileMovingOld : Packet
  {
    public MobileMovingOld(Mobile m, int noto) : base(0x77, 17)
    {
      var loc = m.Location;

      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((short)loc.m_X);
      Stream.Write((short)loc.m_Y);
      Stream.Write((sbyte)loc.m_Z);
      Stream.Write((byte)m.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetOldPacketFlags());
      Stream.Write((byte)noto);
    }
  }
}
