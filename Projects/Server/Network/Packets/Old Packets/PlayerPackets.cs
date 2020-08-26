/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: PlayerPackets.cs - Created: 2020/05/07 - Updated: 2020/06/25    *
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

using System;

namespace Server.Network
{
    public enum LRReason : byte
    {
        CannotLift = 0,
        OutOfRange = 1,
        OutOfSight = 2,
        TryToSteal = 3,
        AreHolding = 4,
        Inspecific = 5
    }

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

    public sealed class DisplayProfile : Packet
    {
        public DisplayProfile(Serial m, string header, string body, string footer) : base(0xB8)
        {
            header ??= "";
            body ??= "";
            footer ??= "";

            EnsureCapacity(12 + header.Length + footer.Length * 2 + body.Length * 2);

            Stream.Write(m);
            Stream.WriteAsciiNull(header);
            Stream.WriteBigUniNull(footer);
            Stream.WriteBigUniNull(body);
        }
    }

    public sealed class LiftRej : Packet
    {
        public LiftRej(LRReason reason) : base(0x27, 2)
        {
            Stream.Write((byte)reason);
        }
    }

    public sealed class LogoutAck : Packet
    {
        public LogoutAck() : base(0xD1, 2)
        {
            Stream.Write((byte)0x01);
        }
    }

    public sealed class Weather : Packet
    {
        public Weather(int type, int density, int temp) : base(0x65, 4)
        {
            Stream.Write((byte)type);
            Stream.Write((byte)density);
            Stream.Write((byte)temp);
        }
    }

    public sealed class RemoveEntity : Packet
    {
        public RemoveEntity(Serial entity) : base(0x1D, 5)
        {
            Stream.Write(entity);
        }
    }

    public sealed class ServerChange : Packet
    {
        public ServerChange(Point3D p, Map map) : base(0x76, 16)
        {
            Stream.Write((short)p.X);
            Stream.Write((short)p.Y);
            Stream.Write((short)p.Z);
            Stream.Write((byte)0);
            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write((short)map.Width);
            Stream.Write((short)map.Height);
        }
    }

    public sealed class SkillUpdate : Packet
    {
        public SkillUpdate(Skills skills) : base(0x3A)
        {
            EnsureCapacity(6 + skills.Length * 9);

            Stream.Write((byte)0x02); // type: absolute, capped

            for (var i = 0; i < skills.Length; ++i)
            {
                var s = skills[i];

                var v = s.NonRacialValue;
                var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

                Stream.Write((ushort)(s.Info.SkillID + 1));
                Stream.Write((ushort)uv);
                Stream.Write((ushort)s.BaseFixedPoint);
                Stream.Write((byte)s.Lock);
                Stream.Write((ushort)s.CapFixedPoint);
            }

            Stream.Write((short)0); // terminate
        }
    }

    public sealed class Sequence : Packet
    {
        public Sequence(int num) : base(0x7B, 2)
        {
            Stream.Write((byte)num);
        }
    }

    public sealed class SkillChange : Packet
    {
        public SkillChange(Skill skill) : base(0x3A)
        {
            EnsureCapacity(13);

            var v = skill.NonRacialValue;
            var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

            Stream.Write((byte)0xDF); // type: delta, capped
            Stream.Write((ushort)skill.Info.SkillID);
            Stream.Write((ushort)uv);
            Stream.Write((ushort)skill.BaseFixedPoint);
            Stream.Write((byte)skill.Lock);
            Stream.Write((ushort)skill.CapFixedPoint);
        }
    }

    public sealed class LaunchBrowser : Packet
    {
        public LaunchBrowser(string url) : base(0xA5)
        {
            url ??= "";

            EnsureCapacity(4 + url.Length);

            Stream.WriteAsciiNull(url);
        }
    }

    public sealed class DragEffect : Packet
    {
        public DragEffect(IEntity src, IEntity trg, int itemID, int hue, int amount) : base(0x23, 26)
        {
            Stream.Write((short)itemID);
            Stream.Write((byte)0);
            Stream.Write((short)hue);
            Stream.Write((short)amount);
            Stream.Write(src.Serial);
            Stream.Write((short)src.X);
            Stream.Write((short)src.Y);
            Stream.Write((sbyte)src.Z);
            Stream.Write(trg.Serial);
            Stream.Write((short)trg.X);
            Stream.Write((short)trg.Y);
            Stream.Write((sbyte)trg.Z);
        }
    }

    public sealed class SeasonChange : Packet
    {
        private static readonly SeasonChange[][] m_Cache =
        {
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2]
        };

        public SeasonChange(int season, bool playSound = true) : base(0xBC, 3)
        {
            Stream.Write((byte)season);
            Stream.Write(playSound);
        }

        public static SeasonChange Instantiate(int season) => Instantiate(season, true);

        public static SeasonChange Instantiate(int season, bool playSound)
        {
            if (season >= 0 && season < m_Cache.Length)
            {
                var idx = playSound ? 1 : 0;

                var p = m_Cache[season][idx];

                if (p == null)
                {
                    m_Cache[season][idx] = p = new SeasonChange(season, playSound);
                    p.SetStatic();
                }

                return p;
            }

            return new SeasonChange(season, playSound);
        }
    }

    public sealed class DisplayPaperdoll : Packet
    {
        public DisplayPaperdoll(Serial m, string title, bool warmode, bool canLift) : base(0x88, 66)
        {
            byte flags = 0x00;

            if (warmode)
                flags |= 0x01;

            if (canLift)
                flags |= 0x02;

            Stream.Write(m);
            Stream.WriteAsciiFixed(title, 60);
            Stream.Write(flags);
        }
    }

    public sealed class PlaySound : Packet
    {
        public PlaySound(int soundID, IPoint3D target) : base(0x54, 12)
        {
            Stream.Write((byte)1); // flags
            Stream.Write((short)soundID);
            Stream.Write((short)0); // volume
            Stream.Write((short)target.X);
            Stream.Write((short)target.Y);
            Stream.Write((short)target.Z);
        }
    }

    public sealed class PlayMusic : Packet
    {
        public static readonly Packet InvalidInstance = SetStatic(new PlayMusic(MusicName.Invalid));

        private static readonly Packet[] m_Instances = new Packet[60];

        public PlayMusic(MusicName name) : base(0x6D, 3)
        {
            Stream.Write((short)name);
        }

        public static Packet GetInstance(MusicName name)
        {
            if (name == MusicName.Invalid)
                return InvalidInstance;

            var v = (int)name;
            Packet p;

            if (v >= 0 && v < m_Instances.Length)
            {
                p = m_Instances[v];

                if (p == null)
                    m_Instances[v] = p = SetStatic(new PlayMusic(name));
            }
            else
            {
                p = new PlayMusic(name);
            }

            return p;
        }
    }

    public sealed class ScrollMessage : Packet
    {
        public ScrollMessage(int type, int tip, string text) : base(0xA6)
        {
            text ??= "";

            EnsureCapacity(10 + text.Length);

            Stream.Write((byte)type);
            Stream.Write(tip);
            Stream.Write((ushort)text.Length);
            Stream.WriteAsciiFixed(text, text.Length);
        }
    }

    public sealed class CurrentTime : Packet
    {
        public CurrentTime() : this(DateTime.Now)
        {
        }

        public CurrentTime(DateTime date) : base(0x5B, 4)
        {
            Stream.Write((byte)date.Hour);
            Stream.Write((byte)date.Minute);
            Stream.Write((byte)date.Second);
        }
    }

    public sealed class PathfindMessage : Packet
    {
        public PathfindMessage(Point3D p) : base(0x38, 7)
        {
            Stream.Write((short)p.X);
            Stream.Write((short)p.Y);
            Stream.Write((short)p.Z);
        }
    }

    public sealed class PingAck : Packet
    {
        private static readonly PingAck[] m_Cache = new PingAck[0x100];

        public PingAck(byte ping) : base(0x73, 2)
        {
            Stream.Write(ping);
        }

        public static PingAck Instantiate(byte ping)
        {
            var p = m_Cache[ping];

            if (p == null)
            {
                m_Cache[ping] = p = new PingAck(ping);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class ClearWeaponAbility : Packet
    {
        public static readonly Packet Instance = SetStatic(new ClearWeaponAbility());

        public ClearWeaponAbility() : base(0xBF)
        {
            EnsureCapacity(5);

            Stream.Write((short)0x21);
        }
    }
}
