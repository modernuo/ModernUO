/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: PlayerPackets.cs - Created: 2020/05/07 - Updated: 2020/05/07    *
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
  public sealed class StatLockInfo : Packet
  {
    public StatLockInfo(Mobile m) : base(0xBF)
    {
      EnsureCapacity(12);

      Stream.Write((short)0x19);
      Stream.Write((byte)2);
      Stream.Write(m.Serial);
      Stream.Write((byte)0);

      var lockBits = ((int)m.StrLock << 4) | ((int)m.DexLock << 2) | (int)m.IntLock;

      Stream.Write((byte)lockBits);
    }
  }

  public sealed class ChangeCombatant : Packet
  {
    public ChangeCombatant(Mobile combatant) : base(0xAA, 5)
    {
      Stream.Write(combatant?.Serial ?? Serial.Zero);
    }
  }

  public sealed class ChangeUpdateRange : Packet
  {
    private static readonly ChangeUpdateRange[] m_Cache = new ChangeUpdateRange[0x100];

    public ChangeUpdateRange(int range) : base(0xC8, 2)
    {
      Stream.Write((byte)range);
    }

    public static ChangeUpdateRange Instantiate(int range)
    {
      var idx = (byte)range;
      var p = m_Cache[idx];

      if (p == null)
      {
        m_Cache[idx] = p = new ChangeUpdateRange(range);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class DeathStatus : Packet
  {
    public static readonly Packet Dead = SetStatic(new DeathStatus(true));
    public static readonly Packet Alive = SetStatic(new DeathStatus(false));

    public DeathStatus(bool dead) : base(0x2C, 2)
    {
      Stream.Write((byte)(dead ? 0 : 2));
    }

    public static Packet Instantiate(bool dead) => dead ? Dead : Alive;
  }

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

  public sealed class ToggleSpecialAbility : Packet
  {
    public ToggleSpecialAbility(int abilityID, bool active) : base(0xBF)
    {
      EnsureCapacity(7);

      Stream.Write((short)0x25);

      Stream.Write((short)abilityID);
      Stream.Write(active);
    }
  }

  public sealed class GlobalLightLevel : Packet
  {
    private static readonly GlobalLightLevel[] m_Cache = new GlobalLightLevel[0x100];

    public GlobalLightLevel(int level) : base(0x4F, 2)
    {
      Stream.Write((sbyte)level);
    }

    public static GlobalLightLevel Instantiate(int level)
    {
      var lvl = (byte)level;
      var p = m_Cache[lvl];

      if (p == null)
      {
        m_Cache[lvl] = p = new GlobalLightLevel(level);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class PersonalLightLevel : Packet
  {
    public PersonalLightLevel(Mobile m, int level) : base(0x4E, 6)
    {
      Stream.Write(m.Serial);
      Stream.Write((sbyte)level);
    }
  }

  public sealed class PersonalLightLevelZero : Packet
  {
    public PersonalLightLevelZero(Mobile m) : base(0x4E, 6)
    {
      Stream.Write(m.Serial);
      Stream.Write((sbyte)0);
    }
  }
}
